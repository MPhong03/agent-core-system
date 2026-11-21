// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Common;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Application.Tools.Queries;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Module.Organization.Application.Contracts;
using AgentCoreSystem.Module.Organization.Domain.Entities;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Module.Tasks.Application.Contracts;
using AgentCoreSystem.Module.Tasks.Application.Tasks.DTOs;
using AgentCoreSystem.Module.Tasks.Domain.Enums;
using AgentCoreSystem.Shared.Infrastructure.Http;
using AgentCoreSystem.Shared.Infrastructure.LLM;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentCoreSystem.Module.Tasks.Infrastructure.Jobs
{
    /// <summary>
    /// Trình thực thi tác vụ ngầm cho Nhân sự AI thông qua Hangfire Worker hỗ trợ Tool Calling.
    /// </summary>
    public class TaskAgentJobRunner(
        ITasksDbContext dbContext,
        IOrganizationDbContext orgDbContext,
        ILLMClient llmClient,
        IArtifactStorageService storageService,
        ITasksRealtimeNotifier realtimeNotifier,
        ISender mediator,
        IRestApiExecutor restApiExecutor,
        ILogger<TaskAgentJobRunner> logger) : ITaskAgentJobRunner
    {
        private const int MaxToolIterations = 5;

        /// <inheritdoc />
        public async Task ExecuteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var task = await dbContext.Tasks
                .Include(t => t.Assignments)
                .Include(t => t.Artifacts)
                .Include(t => t.ActivityLogs)
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
            {
                logger.LogWarning("Không tìm thấy tác vụ có Id {TaskId} để thực thi.", taskId);
                return;
            }

            if (task.Status == TaskItemStatus.Done || task.Status == TaskItemStatus.InReview)
            {
                logger.LogInformation("Tác vụ {TaskId} ({TaskCode}) đã ở trạng thái {Status}, không cần chạy lại.", taskId, task.TaskCode, task.Status);
                return;
            }

            var agentAssignment = task.Assignments.FirstOrDefault(a => a.IsAgent);
            if (agentAssignment == null)
            {
                logger.LogWarning("Tác vụ {TaskId} ({TaskCode}) không có Agent nào được phân công.", taskId, task.TaskCode);
                return;
            }

            try
            {
                // Chuyển sang InProgress nếu đang ở ToDo
                if (task.Status == TaskItemStatus.ToDo)
                {
                    task.MoveTo(
                        TaskItemStatus.InProgress,
                        agentAssignment.AssigneeMemberId,
                        isActorAgent: true,
                        "Agent bắt đầu thực thi tác vụ ngầm.");

                    await dbContext.SaveChangesAsync(cancellationToken);
                    await realtimeNotifier.NotifyTaskStatusChangedAsync(
                        task.ChannelId,
                        task.Id,
                        TaskItemStatus.ToDo,
                        TaskItemStatus.InProgress,
                        agentAssignment.AssigneeMemberId,
                        cancellationToken);
                }

                // Truy vấn thông tin Agent từ Module Organization
                var member = await orgDbContext.Members
                    .FirstOrDefaultAsync(m => m.Id == agentAssignment.AssigneeMemberId, cancellationToken);

                string systemPrompt = "Bạn là một trợ lý AI chuyên nghiệp và tận tụy.";
                string modelName = "gemini-2.5-flash";
                decimal temperature = 0.7m;

                AgentProfile? profile = null;
                if (member?.AgentProfileId.HasValue == true)
                {
                    profile = await orgDbContext.AgentProfiles
                        .FirstOrDefaultAsync(p => p.Id == member.AgentProfileId.Value, cancellationToken);

                    if (profile != null)
                    {
                        systemPrompt = profile.SystemPrompt;
                        modelName = string.IsNullOrWhiteSpace(profile.ModelName) ? modelName : profile.ModelName;
                        temperature = profile.Temperature;

                        if (!string.IsNullOrWhiteSpace(profile.PrivateMemoryNotes))
                        {
                            systemPrompt += $"\n\n[Bộ nhớ & Kinh nghiệm cá nhân của bạn]:\n{profile.PrivateMemoryNotes}";
                        }
                    }
                }

                var userPrompt = new StringBuilder();
                userPrompt.AppendLine($"Hãy thực hiện tác vụ sau và trình bày kết quả chi tiết dưới định dạng Markdown:");
                userPrompt.AppendLine($"- Mã tác vụ: {task.TaskCode}");
                userPrompt.AppendLine($"- Tiêu đề: {task.Title}");
                userPrompt.AppendLine($"- Mô tả chi tiết: {task.Description}");

                var messages = new List<ChatMessagePayload>
                {
                    new("system", systemPrompt),
                    new("user", userPrompt.ToString())
                };

                // Truy vấn danh sách công cụ được phân quyền cho Agent (nếu có profile)
                var activeTools = new List<ToolDefinitionDto>();
                if (profile != null)
                {
                    try
                    {
                        var toolsResult = await mediator.Send(new GetAgentToolsQuery(profile.Id, MaskSecrets: false), cancellationToken);
                        if (toolsResult is { IsSuccess: true, Value: not null })
                        {
                            activeTools = toolsResult.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Không thể truy vấn danh sách công cụ của Agent {ProfileId}", profile.Id);
                    }
                }

                var toolPayloads = activeTools.Count > 0
                    ? activeTools.Select(t => new LlmToolDefinitionPayload(
                        t.Name,
                        t.Description,
                        t.ParametersSchema,
                        "function")).ToList()
                    : null;

                string finalContent = string.Empty;

                if (toolPayloads is { Count: > 0 } && profile != null)
                {
                    int iterations = 0;
                    while (iterations < MaxToolIterations)
                    {
                        iterations++;

                        var response = await llmClient.ChatWithToolsAsync(
                            modelName,
                            messages,
                            toolPayloads,
                            temperature,
                            cancellationToken);

                        if (response.HasToolCalls && response.ToolCalls != null && response.ToolCalls.Count > 0)
                        {
                            messages.Add(new ChatMessagePayload(
                                Role: "assistant",
                                Content: response.Content,
                                ToolCalls: response.ToolCalls));

                            foreach (var call in response.ToolCalls)
                            {
                                var matchedTool = activeTools.FirstOrDefault(t =>
                                    t.Name.Equals(call.Name, StringComparison.OrdinalIgnoreCase));

                                bool isSafe = matchedTool != null &&
                                    (profile.ToolExecutionMode == ToolExecutionMode.Auto ||
                                     (profile.ToolExecutionMode == ToolExecutionMode.SemiAuto && !matchedTool.RequiresConfirmation));

                                if (isSafe && matchedTool != null)
                                {
                                    var execResult = await ExecuteToolCallAsync(matchedTool, call, cancellationToken);
                                    var toolOutput = execResult.IsSuccess
                                        ? execResult.ResponsePayload
                                        : (execResult.ErrorMessage ?? "Tool execution failed.");

                                    try
                                    {
                                        await mediator.Send(new RecordToolExecutionCommand(
                                            ToolDefinitionId: matchedTool.Id,
                                            AgentProfileId: profile.Id,
                                            CallerType: ToolCallerType.Task,
                                            ExecutionSourceId: task.Id,
                                            InputArguments: call.Arguments,
                                            ResponsePayload: execResult.ResponsePayload,
                                            StatusCode: execResult.StatusCode,
                                            DurationMs: execResult.DurationMs,
                                            IsSuccess: execResult.IsSuccess,
                                            ErrorMessage: execResult.ErrorMessage), cancellationToken);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogWarning(ex, "Không thể ghi nhật ký thực thi công cụ {ToolName} cho tác vụ {TaskId}", call.Name, task.Id);
                                    }

                                    messages.Add(new ChatMessagePayload(
                                        Role: "tool",
                                        Content: toolOutput,
                                        ToolCallId: call.Id));
                                }
                                else
                                {
                                    var warningComment = matchedTool != null && matchedTool.RequiresConfirmation
                                        ? $"Công cụ '{call.Name}' yêu cầu phê duyệt từ con người (RequiresConfirmation = true) nên không thể thực thi tự động trong tác vụ ngầm."
                                        : $"Công cụ '{call.Name}' không được phép thực thi tự động trong tác vụ ngầm theo chế độ {profile.ToolExecutionMode}.";

                                    logger.LogWarning("Tác vụ {TaskId}: {Warning}", task.Id, warningComment);

                                    task.AddActivityLog(
                                        agentAssignment.AssigneeMemberId,
                                        "ToolConfirmationRequired",
                                        warningComment);

                                    const string skippedMessage = "Tool execution skipped: This tool requires confirmation and cannot be executed autonomously in a non-interactive background task.";

                                    messages.Add(new ChatMessagePayload(
                                        Role: "tool",
                                        Content: skippedMessage,
                                        ToolCallId: call.Id));
                                }
                            }
                        }
                        else
                        {
                            finalContent = response.Content ?? string.Empty;
                            break;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(finalContent))
                    {
                        var lastAssistantMsg = messages.LastOrDefault(m => m.Role == "assistant" && !string.IsNullOrWhiteSpace(m.Content));
                        if (lastAssistantMsg != null)
                        {
                            finalContent = lastAssistantMsg.Content!;
                        }
                    }
                }
                else
                {
                    // Thu thập kết quả từ LLM via StreamChatAsync nếu không có công cụ
                    var outputBuilder = new StringBuilder();
                    await foreach (var chunk in llmClient.StreamChatAsync(modelName, messages, temperature, cancellationToken))
                    {
                        outputBuilder.Append(chunk);
                    }

                    finalContent = outputBuilder.ToString();
                }

                if (string.IsNullOrWhiteSpace(finalContent))
                {
                    finalContent = $"# {task.Title}\n\nAgent đã hoàn thành tác vụ nhưng không có dữ liệu phản hồi trả về.";
                }

                // Lưu kết quả thành file artifact
                var artifactFileName = $"{task.TaskCode.ToLowerInvariant()}_artifact.md";
                var relativePath = await storageService.SaveArtifactAsync(
                    task.ChannelId,
                    task.Id,
                    artifactFileName,
                    finalContent,
                    cancellationToken);

                var fileSize = Encoding.UTF8.GetByteCount(finalContent);
                var summary = $"Báo cáo tự động sinh bởi Agent '{member?.DisplayName ?? "AI"}' ({fileSize} bytes).";

                var addArtifactResult = task.AddArtifact(
                    artifactFileName,
                    relativePath,
                    "text/markdown",
                    fileSize,
                    summary,
                    agentAssignment.AssigneeMemberId);

                if (addArtifactResult.IsFailure)
                {
                    logger.LogError("Không thể thêm artifact vào task {TaskId}: {Error}", taskId, addArtifactResult.Error.Description);
                }

                // Chuyển sang InReview (Cổng nghiệm thu)
                var oldStatus = task.Status;
                task.MoveTo(
                    TaskItemStatus.InReview,
                    agentAssignment.AssigneeMemberId,
                    isActorAgent: true,
                    "Agent đã hoàn thành toàn bộ yêu cầu và bàn giao sản phẩm đầu ra vào Cổng Nghiệm Thu.");

                await dbContext.SaveChangesAsync(cancellationToken);

                // Broadcast thông báo thời gian thực
                var latestArtifact = task.Artifacts.LastOrDefault();
                if (latestArtifact != null)
                {
                    await realtimeNotifier.NotifyTaskArtifactAddedAsync(
                        task.ChannelId,
                        task.Id,
                        new TaskArtifactDto(
                            latestArtifact.Id,
                            latestArtifact.TaskId,
                            latestArtifact.FileName,
                            latestArtifact.FilePath,
                            latestArtifact.FileType,
                            latestArtifact.FileSize,
                            latestArtifact.Summary,
                            latestArtifact.CreatedByMemberId,
                            latestArtifact.CreatedDate),
                        cancellationToken);
                }

                await realtimeNotifier.NotifyTaskStatusChangedAsync(
                    task.ChannelId,
                    task.Id,
                    oldStatus,
                    TaskItemStatus.InReview,
                    agentAssignment.AssigneeMemberId,
                    cancellationToken);

                logger.LogInformation("Tác vụ {TaskId} ({TaskCode}) đã được Agent xử lý thành công và chuyển sang InReview.", taskId, task.TaskCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi ngầm tác vụ {TaskId} ({TaskCode}) bởi Agent.", taskId, task.TaskCode);

                var prevStatus = task.Status;
                task.MoveTo(
                    TaskItemStatus.Failed,
                    agentAssignment.AssigneeMemberId,
                    isActorAgent: true,
                    $"Sự cố thực thi ngầm: {ex.Message}");

                await dbContext.SaveChangesAsync(cancellationToken);

                await realtimeNotifier.NotifyTaskStatusChangedAsync(
                    task.ChannelId,
                    task.Id,
                    prevStatus,
                    TaskItemStatus.Failed,
                    agentAssignment.AssigneeMemberId,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Thực thi lệnh gọi API bên ngoài cho công cụ được kích hoạt.
        /// </summary>
        /// <param name="toolDef">Định nghĩa cấu hình công cụ.</param>
        /// <param name="toolCall">Payload chi tiết lượt gọi công cụ từ LLM.</param>
        /// <param name="ct">Token hủy tác vụ.</param>
        /// <returns>Kết quả thực thi REST API <see cref="RestApiExecutionResult"/>.</returns>
        private async Task<RestApiExecutionResult> ExecuteToolCallAsync(
            ToolDefinitionDto toolDef,
            LlmToolCallPayload toolCall,
            CancellationToken ct)
        {
            var headers = SecretMaskingHelper.ParseHeaders(toolDef.HeadersConfig);
            IDictionary<string, object?>? arguments = null;
            if (!string.IsNullOrWhiteSpace(toolCall.Arguments))
            {
                try
                {
                    arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.Arguments);
                }
                catch (JsonException)
                {
                    arguments = new Dictionary<string, object?>();
                }
            }

            var req = new RestApiExecutionRequest
            {
                Url = toolDef.EndpointUrl,
                Method = toolDef.HttpMethod,
                Headers = headers,
                Arguments = arguments,
                TimeoutSeconds = toolDef.TimeoutSeconds
            };

            return await restApiExecutor.ExecuteAsync(req, ct);
        }
    }
}
