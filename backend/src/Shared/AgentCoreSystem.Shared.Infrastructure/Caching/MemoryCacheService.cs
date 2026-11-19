// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AgentCoreSystem.Shared.Infrastructure.Caching
{
    /// <summary>
    /// Giao diện trừu tượng cho dịch vụ lưu trữ bộ nhớ đệm (Cache).
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Định nghĩa các thao tác đọc, ghi, xóa và factory cache.</para>
    /// <para><strong>Why:</strong> Cho phép hệ thống hoán đổi linh hoạt giữa In-Memory Cache (giai đoạn đầu) và Distributed Cache (Redis) mà không làm thay đổi các tầng nghiệp vụ phía trên.</para>
    /// </remarks>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Triển khai dịch vụ Cache cục bộ dựa trên <see cref="IMemoryCache"/>.
    /// </summary>
    /// <remarks>
    /// <para><strong>When to use:</strong> Được sử dụng ở giai đoạn khởi đầu dự án khi hệ thống chỉ cần chạy trên single instance.</para>
    /// <para><strong>Why:</strong> Cung cấp hiệu năng đọc ghi siêu nhanh (in-process memory) mà không tốn chi phí hạ tầng mạng. Tuân thủ abstraction <see cref="ICacheService"/> để sẵn sàng hoán đổi sang Redis.</para>
    /// </remarks>
    public class MemoryCacheService(
        IMemoryCache memoryCache, 
        ILogger<MemoryCacheService> logger) : ICacheService
    {
        private static readonly ConcurrentDictionary<string, byte> CacheKeys = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (memoryCache.TryGetValue(key, out T? value))
            {
                logger.LogDebug("Cache Hit: {Key}", key);
                return Task.FromResult(value);
            }

            logger.LogDebug("Cache Miss: {Key}", key);
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default)
        {
            var options = new MemoryCacheEntryOptions();
            if (expirationTime.HasValue)
            {
                options.SetAbsoluteExpiration(expirationTime.Value);
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromMinutes(30));
            }

            options.RegisterPostEvictionCallback((k, _, _, _) =>
            {
                CacheKeys.TryRemove(k.ToString()!, out _);
            });

            memoryCache.Set(key, value, options);
            CacheKeys.TryAdd(key, 0);

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            memoryCache.Remove(key);
            CacheKeys.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
        {
            foreach (var key in CacheKeys.Keys)
            {
                if (key.StartsWith(prefixKey, StringComparison.OrdinalIgnoreCase))
                {
                    memoryCache.Remove(key);
                    CacheKeys.TryRemove(key, out _);
                }
            }

            return Task.CompletedTask;
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key, 
            Func<Task<T>> factory, 
            TimeSpan? expirationTime = null, 
            CancellationToken cancellationToken = default)
        {
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            var newValue = await factory();
            await SetAsync(key, newValue, expirationTime, cancellationToken);
            return newValue;
        }
    }
}

