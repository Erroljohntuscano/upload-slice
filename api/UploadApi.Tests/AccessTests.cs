using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "upl_" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dir);
        builder.UseSetting("Storage:Path", Path.Combine(_dir, "files"));
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={Path.Combine(_dir, "test.db")}");
    }
}

public class AccessTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<Guid> UploadAs(string user)
    {
        var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4 test"));
        part.Headers.ContentType = new("application/pdf");
        content.Add(part, "file", "hello.pdf");

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/files") { Content = content };
        req.Headers.Add("X-User-Id", user);
        var res = await factory.CreateClient().SendAsync(req);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }

    private async Task<HttpStatusCode> Get(Guid id, string? user)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/files/{id}");
        if (user is not null) req.Headers.Add("X-User-Id", user);
        return (await factory.CreateClient().SendAsync(req)).StatusCode;
    }

    [Fact]
    public async Task Owner_can_download_own_file()
    {
        var id = await UploadAs("alice");
        Assert.Equal(HttpStatusCode.OK, await Get(id, "alice"));
    }

    [Fact]
    public async Task Other_user_cannot_download_file()
    {
        var id = await UploadAs("alice");
        Assert.Equal(HttpStatusCode.NotFound, await Get(id, "bob"));
    }

    [Fact]
    public async Task Anonymous_user_gets_401()
    {
        var id = await UploadAs("alice");
        Assert.Equal(HttpStatusCode.Unauthorized, await Get(id, null));
    }
}