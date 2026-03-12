using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DropPost;

class Uploader
{
    private readonly AppSettings _settings;
    private static readonly HttpClient Http = new();

    public Uploader(AppSettings settings) => _settings = settings;

    public Task<string> UploadFileAsync(string filePath, string expiry) =>
        PostAsync(
            fileBytes: File.ReadAllBytes(filePath),
            fileName: Path.GetFileName(filePath),
            contentType: "application/octet-stream",
            expiry: expiry);

    public Task<string> UploadTextAsync(string text, string expiry) =>
        PostAsync(
            fileBytes: Encoding.UTF8.GetBytes(text),
            fileName: "paste.txt",
            contentType: "text/plain",
            expiry: expiry);

    private async Task<string> PostAsync(byte[] fileBytes, string fileName, string contentType, string expiry)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);

        var url = expiry != "never"
            ? $"{_settings.ServerUrl}?expire={expiry}"
            : _settings.ServerUrl;

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var resp = await Http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadAsStringAsync()).Trim();
    }
}
