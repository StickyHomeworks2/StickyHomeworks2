using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StickyHomeworks2.Helpers;

public static class WebRequestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static ILogger? Logger { get; set; }

    public static async Task<T?> GetJsonAsync<T>(HttpClient client, string url, int retries = 2, CancellationToken cancellationToken = default)
    {
        Logger?.LogTrace("HTTP GET: {Url}", url);
        Exception? lastEx = null;
        var delay = TimeSpan.FromSeconds(1);

        for (int i = 0; i <= retries; i++)
        {
            try
            {
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                lastEx = ex;
                Logger?.LogWarning(ex, "HTTP GET 失败（第 {Retry} 次重试）{Url}", i, url);
                if (i < retries)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay *= 2;
                }
            }
        }

        throw new HttpRequestException($"在 {retries} 次重试后无法完成对 {url} 的 GET 请求", lastEx);
    }

    public static async Task<string> GetStringAsync(HttpClient client, string url, int retries = 2, CancellationToken cancellationToken = default)
    {
        Logger?.LogTrace("HTTP GET: {Url}", url);
        Exception? lastEx = null;
        var delay = TimeSpan.FromSeconds(1);

        for (int i = 0; i <= retries; i++)
        {
            try
            {
                return await client.GetStringAsync(url, cancellationToken);
            }
            catch (Exception ex)
            {
                lastEx = ex;
                Logger?.LogWarning(ex, "HTTP GET 失败（第 {Retry} 次重试）{Url}", i, url);
                if (i < retries)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay *= 2;
                }
            }
        }

        throw new HttpRequestException($"在 {retries} 次重试后无法完成对 {url} 的 GET 请求", lastEx);
    }

    public static async Task DownloadToFileAsync(HttpClient client, string url, string filePath,
        IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        Logger?.LogInformation("开始下载: {Url} -> {Path}", url, filePath);
        Exception? lastEx = null;

        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int read;

            while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;
                progress?.Report(totalRead);
            }

            Logger?.LogInformation("下载完成: {Path} 大小={Size}", filePath, totalRead);
        }
        catch (OperationCanceledException)
        {
            Logger?.LogInformation("下载已取消: {Url}", url);
            throw;
        }
        catch (Exception ex)
        {
            lastEx = ex;
            Logger?.LogError(ex, "下载失败: {Url}", url);
            throw new HttpRequestException($"下载失败: {url}", ex);
        }
    }
}
