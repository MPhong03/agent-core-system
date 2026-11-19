// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;

namespace AgentCoreSystem.Shared.Application.Messaging
{
    /// <summary>
    /// Giao diện đại diện cho một tác vụ ghi (Command) không trả về dữ liệu nghiệp vụ.
    /// </summary>
    public interface ICommand : IRequest<Result>
    {
    }

    /// <summary>
    /// Giao diện đại diện cho một tác vụ ghi (Command) có trả về kết quả kiểu <typeparamref name="TResponse"/>.
    /// </summary>
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {
    }

    /// <summary>
    /// Giao diện xử lý một tác vụ ghi không có dữ liệu trả về.
    /// </summary>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
        where TCommand : ICommand
    {
    }

    /// <summary>
    /// Giao diện xử lý một tác vụ ghi có trả về kết quả.
    /// </summary>
    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
        where TCommand : ICommand<TResponse>
    {
    }

    /// <summary>
    /// Giao diện đại diện cho một tác vụ đọc/truy vấn dữ liệu (Query).
    /// </summary>
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    }

    /// <summary>
    /// Giao diện xử lý một tác vụ đọc/truy vấn dữ liệu.
    /// </summary>
    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
        where TQuery : IQuery<TResponse>
    {
    }
}

