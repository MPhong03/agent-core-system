// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AgentCoreSystem.Module.Agents.Application.Common
{
    /// <summary>
    /// Tiện ích hỗ trợ che giấu (masking) và hợp nhất (merge) các thông tin nhạy cảm trong HTTP Headers cấu hình công cụ.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Phát hiện các Header nhạy cảm (Authorization, ApiKey, X-Api-Key, Token, Secret) và thay thế giá trị bằng chuỗi '••••••••'.</para>
    /// <para><strong>Why:</strong> Tuyệt đối không để lộ Plaintext Secrets khi trả DTO về phía Client qua Web API, đồng thời bảo toàn Secret gốc khi người dùng gửi lại chuỗi mask trong tác vụ cập nhật.</para>
    /// </remarks>
    public static class SecretMaskingHelper
    {
        /// <summary>
        /// Chuỗi ký tự đại diện cho Secret đã được che giấu (8 ký tự bullet).
        /// </summary>
        public const string MaskedSecret = "••••••••";

        private static readonly HashSet<string> ExactSensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "ApiKey",
            "X-Api-Key",
            "Token",
            "Secret",
            "X-Auth-Token",
            "Bearer",
            "Proxy-Authorization"
        };

        /// <summary>
        /// Kiểm tra xem tên của một HTTP Header có thuộc danh sách nhạy cảm cần che giấu hay không.
        /// </summary>
        /// <param name="headerName">Tên Header cần kiểm tra.</param>
        /// <returns>True nếu Header chứa thông tin nhạy cảm (case-insensitive); ngược lại False.</returns>
        public static bool IsSensitiveHeader(string? headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                return false;
            }

            var trimmed = headerName.Trim();
            if (ExactSensitiveHeaderNames.Contains(trimmed))
            {
                return true;
            }

            // Kiểm tra các từ khóa nhạy cảm trong tên Header
            if (trimmed.IndexOf("secret", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (trimmed.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (trimmed.IndexOf("api-key", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (trimmed.IndexOf("apikey", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (trimmed.IndexOf("auth", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Che giấu các giá trị Header nhạy cảm trong Dictionary.
        /// </summary>
        /// <param name="headers">Dictionary chứa các Headers ban đầu.</param>
        /// <returns>Dictionary mới với các trường nhạy cảm được thay thế bằng <see cref="MaskedSecret"/>.</returns>
        public static Dictionary<string, string> MaskHeaders(IDictionary<string, string>? headers)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null)
            {
                return result;
            }

            foreach (var kvp in headers)
            {
                if (IsSensitiveHeader(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
                {
                    result[kvp.Key] = MaskedSecret;
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Che giấu các giá trị Header nhạy cảm từ chuỗi JSON cấu hình Headers.
        /// </summary>
        /// <param name="headersJson">Chuỗi JSON biểu diễn Dictionary headers.</param>
        /// <returns>Chuỗi JSON mới với các Header nhạy cảm đã được che giấu.</returns>
        public static string MaskHeadersJson(string? headersJson)
        {
            if (string.IsNullOrWhiteSpace(headersJson))
            {
                return "{}";
            }

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                if (dict == null)
                {
                    return "{}";
                }

                var masked = MaskHeaders(dict);
                return JsonSerializer.Serialize(masked);
            }
            catch (JsonException)
            {
                return headersJson;
            }
        }

        /// <summary>
        /// Hợp nhất chuỗi JSON Headers cập nhật với chuỗi Headers hiện có.
        /// Nếu giá trị của một Header trong bản cập nhật là <see cref="MaskedSecret"/>, giá trị Secret ban đầu sẽ được giữ nguyên.
        /// </summary>
        /// <param name="existingHeadersJson">Chuỗi JSON Headers hiện tại trong cơ sở dữ liệu.</param>
        /// <param name="incomingHeadersJson">Chuỗi JSON Headers mới nhận từ Client.</param>
        /// <returns>Chuỗi JSON Headers sau khi hợp nhất bảo toàn Secret.</returns>
        public static string MergeHeadersJson(string? existingHeadersJson, string? incomingHeadersJson)
        {
            if (string.IsNullOrWhiteSpace(incomingHeadersJson))
            {
                return string.IsNullOrWhiteSpace(existingHeadersJson) ? "{}" : existingHeadersJson;
            }

            Dictionary<string, string>? existingDict = null;
            if (!string.IsNullOrWhiteSpace(existingHeadersJson))
            {
                try
                {
                    existingDict = JsonSerializer.Deserialize<Dictionary<string, string>>(existingHeadersJson);
                }
                catch (JsonException)
                {
                    existingDict = null;
                }
            }

            Dictionary<string, string>? incomingDict;
            try
            {
                incomingDict = JsonSerializer.Deserialize<Dictionary<string, string>>(incomingHeadersJson);
            }
            catch (JsonException)
            {
                return incomingHeadersJson;
            }

            if (incomingDict == null)
            {
                return string.IsNullOrWhiteSpace(existingHeadersJson) ? "{}" : existingHeadersJson;
            }

            if (existingDict == null || existingDict.Count == 0)
            {
                return JsonSerializer.Serialize(incomingDict);
            }

            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in incomingDict)
            {
                if (kvp.Value == MaskedSecret && existingDict.TryGetValue(kvp.Key, out var originalValue))
                {
                    merged[kvp.Key] = originalValue;
                }
                else
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            return JsonSerializer.Serialize(merged);
        }

        /// <summary>
        /// Chuyển đổi chuỗi JSON Headers sang Dictionary với các khóa và giá trị ban đầu (Plaintext).
        /// </summary>
        /// <param name="headersJson">Chuỗi JSON Headers cấu hình trong cơ sở dữ liệu.</param>
        /// <returns>Dictionary Headers phục vụ thực thi gọi API bên ngoài.</returns>
        public static Dictionary<string, string> ParseHeaders(string? headersJson)
        {
            if (string.IsNullOrWhiteSpace(headersJson))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                return dict != null
                    ? new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
