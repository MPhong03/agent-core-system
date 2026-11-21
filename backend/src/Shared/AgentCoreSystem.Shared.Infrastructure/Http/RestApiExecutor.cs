// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgentCoreSystem.Shared.Infrastructure.Http
{
    /// <summary>
    /// Triển khai dịch vụ thực thi REST API không trạng thái kèm bộ lọc SSRF Guard.
    /// </summary>
    public partial class RestApiExecutor(
        HttpClient httpClient,
        Func<string, CancellationToken, Task<IPAddress[]>>? dnsResolver = null)
        : IRestApiExecutor
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly Func<string, CancellationToken, Task<IPAddress[]>> _dnsResolver =
            dnsResolver ?? ((host, ct) => Dns.GetHostAddressesAsync(host, ct));

        [GeneratedRegex(@"\{([a-zA-Z0-9_-]+)\}", RegexOptions.Compiled)]
        private static partial Regex TemplateRegex();

        /// <inheritdoc />
        public async Task<RestApiExecutionResult> ExecuteAsync(RestApiExecutionRequest request, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var timeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30;

            try
            {
                // 1. Phân tách tham số và thế chỗ URL template
                var remainingArgs = request.Arguments != null
                    ? new Dictionary<string, object?>(request.Arguments, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                var interpolatedUrl = TemplateRegex().Replace(request.Url, match =>
                {
                    var key = match.Groups[1].Value;
                    if (remainingArgs.TryGetValue(key, out var val))
                    {
                        remainingArgs.Remove(key);
                        return Uri.EscapeDataString(val?.ToString() ?? string.Empty);
                    }
                    return match.Value;
                });

                var method = new HttpMethod(string.IsNullOrWhiteSpace(request.Method) ? "GET" : request.Method.ToUpperInvariant());

                // 2. Gắn Query String cho phương thức GET / HEAD nếu còn tham số chưa tiêu thụ
                if ((method == HttpMethod.Get || method == HttpMethod.Head) && remainingArgs.Count > 0)
                {
                    var queryParams = string.Join("&", remainingArgs
                        .Where(kvp => kvp.Value != null)
                        .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? string.Empty)}"));

                    if (!string.IsNullOrEmpty(queryParams))
                    {
                        interpolatedUrl += interpolatedUrl.Contains('?') ? $"&{queryParams}" : $"?{queryParams}";
                    }
                }

                // 3. Kiểm tra SSRF Protection trên URL đã được nội suy
                await EnsureSafeUrlAsync(interpolatedUrl, ct);

                // 4. Khởi tạo HttpRequestMessage
                using var httpRequest = new HttpRequestMessage(method, interpolatedUrl);

                // 5. Gắn Body cho POST / PUT / PATCH hoặc khi có Body chỉ định trực tiếp
                if (!string.IsNullOrWhiteSpace(request.Body))
                {
                    httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
                }
                else if (method != HttpMethod.Get && method != HttpMethod.Head && remainingArgs.Count > 0)
                {
                    var jsonBody = JsonSerializer.Serialize(remainingArgs, JsonOptions);
                    httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                // 6. Gắn Headers
                if (request.Headers != null)
                {
                    foreach (var (headerKey, headerValue) in request.Headers)
                    {
                        if (string.Equals(headerKey, "Content-Type", StringComparison.OrdinalIgnoreCase) && httpRequest.Content != null)
                        {
                            httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(headerValue);
                        }
                        else
                        {
                            httpRequest.Headers.TryAddWithoutValidation(headerKey, headerValue);
                        }
                    }
                }

                // 7. Thực thi HTTP call với timeout được kiểm soát
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var response = await httpClient.SendAsync(httpRequest, linkedCts.Token);
                sw.Stop();

                var content = await response.Content.ReadAsStringAsync(ct);
                var statusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    return RestApiExecutionResult.Success(statusCode, content, sw.ElapsedMilliseconds);
                }

                return RestApiExecutionResult.Failure(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
                    statusCode,
                    sw.ElapsedMilliseconds,
                    content);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                sw.Stop();
                return RestApiExecutionResult.Failure(
                    $"Request timed out after {timeoutSeconds} seconds.",
                    (int)HttpStatusCode.RequestTimeout,
                    sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                return RestApiExecutionResult.Failure(
                    "Request was cancelled.",
                    499,
                    sw.ElapsedMilliseconds);
            }
            catch (InvalidOperationException ex)
            {
                // Bắt các vi phạm an toàn SSRF và cấu hình URL không hợp lệ
                sw.Stop();
                return RestApiExecutionResult.Failure(
                    ex.Message,
                    (int)HttpStatusCode.BadRequest,
                    sw.ElapsedMilliseconds);
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                return RestApiExecutionResult.Failure(
                    $"HTTP request failed: {ex.Message}",
                    (int)(ex.StatusCode ?? HttpStatusCode.BadGateway),
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return RestApiExecutionResult.Failure(
                    $"Unexpected execution error: {ex.Message}",
                    (int)HttpStatusCode.InternalServerError,
                    sw.ElapsedMilliseconds);
            }
        }

        /// <inheritdoc />
        public async Task EnsureSafeUrlAsync(string url, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("SSRF Protection: URL cannot be null or empty.");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"SSRF Protection: The specified URL '{url}' is not a valid absolute URI.");
            }

            // Chỉ cho phép HTTP và HTTPS
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"SSRF Protection: Scheme '{uri.Scheme}' is prohibited. Only HTTP and HTTPS are permitted.");
            }

            var host = uri.DnsSafeHost;
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("SSRF Protection: Hostname cannot be empty.");
            }

            // Chặn localhost
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"SSRF Protection: Access to localhost host '{host}' is forbidden.");
            }

            // Nếu host là địa chỉ IP trực tiếp
            if (IPAddress.TryParse(host, out var directIp))
            {
                if (IsRestrictedIp(directIp))
                {
                    throw new InvalidOperationException($"SSRF Protection: Target IP address '{directIp}' belongs to a private/loopback/restricted network range.");
                }

                return;
            }

            // Nếu host là domain name, phân giải qua DNS resolver để kiểm tra địa chỉ IP thực tế (chống DNS Rebinding)
            try
            {
                var resolvedAddresses = await _dnsResolver(host, ct);
                if (resolvedAddresses == null || resolvedAddresses.Length == 0)
                {
                    throw new InvalidOperationException($"SSRF Protection: Unable to resolve hostname '{host}'.");
                }

                foreach (var ip in resolvedAddresses)
                {
                    if (IsRestrictedIp(ip))
                    {
                        throw new InvalidOperationException($"SSRF Protection: Hostname '{host}' resolves to restricted IP address '{ip}'.");
                    }
                }
            }
            catch (SocketException ex)
            {
                throw new InvalidOperationException($"SSRF Protection: Failed to resolve hostname '{host}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Kiểm tra địa chỉ IP có thuộc dải Private LAN, Loopback, Link-Local hoặc Reserved hay không.
        /// </summary>
        private static bool IsRestrictedIp(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            if (ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.Equals(IPAddress.None))
            {
                return true;
            }

            // Xử lý IPv4-mapped IPv6 address (vd ::ffff:127.0.0.1)
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();

                // 0.0.0.0/8 (This host on this network)
                if (bytes[0] == 0) return true;

                // 10.0.0.0/8 (Private LAN RFC 1918)
                if (bytes[0] == 10) return true;

                // 127.0.0.0/8 (Loopback)
                if (bytes[0] == 127) return true;

                // 169.254.0.0/16 (Link-Local / Cloud Metadata 169.254.169.254)
                if (bytes[0] == 169 && bytes[1] == 254) return true;

                // 172.16.0.0/12 (172.16.0.0 - 172.31.255.255, Private LAN RFC 1918)
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;

                // 192.168.0.0/16 (Private LAN RFC 1918)
                if (bytes[0] == 192 && bytes[1] == 168) return true;

                // 100.64.0.0/10 (Carrier-Grade NAT Shared Address Space)
                if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;

                // 192.0.2.0/24 (TEST-NET-1)
                if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2) return true;

                // 198.51.100.0/24 (TEST-NET-2)
                if (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) return true;

                // 203.0.113.0/24 (TEST-NET-3)
                if (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) return true;

                // 224.0.0.0/4 (Multicast 224.0.0.0 - 239.255.255.255)
                if (bytes[0] >= 224 && bytes[0] <= 239) return true;

                // 240.0.0.0/4 (Reserved / Broadcast 255.255.255.255)
                if (bytes[0] >= 240) return true;

                return false;
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
                {
                    return true;
                }

                var bytes = ip.GetAddressBytes();

                // Unique Local Address (fc00::/7 -> fc00 - fdff)
                if ((bytes[0] & 0xfe) == 0xfc) return true;

                // Link-Local (fe80::/10)
                if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80) return true;

                // Discard-Only prefix (100::/64)
                if (bytes[0] == 0x01 && bytes[1] == 0x00) return true;

                return false;
            }

            return true;
        }
    }
}
