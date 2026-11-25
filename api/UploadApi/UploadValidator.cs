namespace UploadApi;

public static class UploadValidator
{
    public const long MaxBytes = 5 * 1024 * 1024; 

    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        [".pdf"]  = ["application/pdf"],
        [".png"]  = ["image/png"],
        [".jpg"]  = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
    };

    public static async Task<string?> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "File is empty.";

        if (file.Length > MaxBytes)
            return "File is larger than 5 MB.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.TryGetValue(ext, out var types))
            return "Only PDF, PNG and JPEG files are allowed.";

        if (!types.Contains(file.ContentType))
            return "Content type does not match the file extension.";

        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, 8, throwOnEndOfStream: false);
        if (!MatchesSignature(ext, header.AsSpan(0, read)))
            return "File content does not match its extension.";

        return null;
    }

    private static bool MatchesSignature(string ext, ReadOnlySpan<byte> h) => ext switch
    {
        ".pdf" => h.StartsWith("%PDF-"u8),
        ".png" => h.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ".jpg" or ".jpeg" => h.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
        _ => false
    };
}