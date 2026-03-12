using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DropPost;

class Uploader
{
    private readonly AppSettings _settings;
    // No timeout — large file uploads can take a very long time
    private static readonly HttpClient Http = new() { Timeout = System.Threading.Timeout.InfiniteTimeSpan };

    public Uploader(AppSettings settings) => _settings = settings;

    public Task<string> UploadFileAsync(string filePath, string expiry)
    {
        var fileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Path.GetFileName(filePath)}";
        return PostStreamAsync(File.OpenRead(filePath), fileName, "application/octet-stream", expiry);
    }

    public Task<string> UploadTextAsync(string text, string expiry)
    {
        var fileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.txt";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return PostStreamAsync(stream, fileName, "text/plain", expiry);
    }

    private async Task<string> PostStreamAsync(Stream stream, string fileName, string contentType, string expiry)
    {
        using var _ = stream;
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);

        var url = expiry != "never"
            ? $"{_settings.ServerUrl}?expire={expiry}"
            : _settings.ServerUrl;

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadAsStringAsync()).Trim();
    }
}
