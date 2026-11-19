// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

namespace AgentCoreSystem.Shared.Kernel.Primitives
{
    /// <summary>
    /// Đối tượng đóng gói kết quả thực thi của một tác vụ không có giá trị trả về.
    /// </summary>
    /// <remarks>
    /// <strong>Why:</strong> Thay thế việc throw Exception cho các lỗi nghiệp vụ thông thường, tuân thủ nguyên lý Functional Error Handling.
    /// </remarks>
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; }

        protected Result(bool isSuccess, Error error)
        {
            if (isSuccess && error != Error.None)
            {
                throw new InvalidOperationException("Kết quả thành công không thể mang thông tin lỗi.");
            }

            if (!isSuccess && error == Error.None)
            {
                throw new InvalidOperationException("Kết quả thất bại bắt buộc phải có thông tin lỗi.");
            }

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, Error.None);
        public static Result Failure(Error error) => new(false, error);
        public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
        public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
    }

    /// <summary>
    /// Đối tượng đóng gói kết quả thực thi của một tác vụ có giá trị trả về kiểu <typeparamref name="TValue"/>.
    /// </summary>
    public class Result<TValue> : Result
    {
        private readonly TValue? _value;

        protected internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
        {
            _value = value;
        }

        [NotNull]
        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Không thể truy cập giá trị của một kết quả thất bại.");

        public static implicit operator Result<TValue>(TValue? value) =>
            value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
    }
}

