using System.Text;
using Microsoft.AspNetCore.Http;
using UploadApi;

public class ValidatorTests
{
    private static IFormFile Make(string name, byte[] bytes, string contentType, long? length = null) =>
        new FormFile(new MemoryStream(bytes), 0, length ?? bytes.Length, "file", name)
        { Headers = new HeaderDictionary(), ContentType = contentType };

    [Fact]
    public async Task Rejects_empty_file()
    {
        var f = Make("a.pdf", [], "application/pdf");
        Assert.Equal("File is empty.", await UploadValidator.ValidateAsync(f));
    }

    [Fact]
    public async Task Rejects_file_over_max_size()
    {
        var f = Make("a.pdf", Encoding.ASCII.GetBytes("%PDF-1.4"), "application/pdf",
                     UploadValidator.MaxBytes + 1);
        Assert.Equal("File is larger than 5 MB.", await UploadValidator.ValidateAsync(f));
    }

    [Fact]
    public async Task Rejects_disallowed_extension()
    {
        var f = Make("virus.exe", Encoding.ASCII.GetBytes("MZ..."), "application/octet-stream");
        Assert.NotNull(await UploadValidator.ValidateAsync(f));
    }

    [Fact]
    public async Task Rejects_fake_pdf_with_wrong_content()
    {
        var f = Make("fake.pdf", Encoding.ASCII.GetBytes("this is not a pdf"), "application/pdf");
        Assert.Equal("File content does not match its extension.",
                     await UploadValidator.ValidateAsync(f));
    }

    [Fact]
    public async Task Accepts_valid_pdf()
    {
        var f = Make("ok.pdf", Encoding.ASCII.GetBytes("%PDF-1.4 hello"), "application/pdf");
        Assert.Null(await UploadValidator.ValidateAsync(f));
    }
}