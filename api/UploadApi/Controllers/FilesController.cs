using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UploadApi.Controllers;

[ApiController, Authorize, Route("api/files")]
public class FilesController(AppDb db, IConfiguration cfg, IWebHostEnvironment env) : ControllerBase
{
    private string StorageDir =>
        Path.GetFullPath(Path.Combine(env.ContentRootPath, cfg["Storage:Path"] ?? "storage"));

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    [RequestSizeLimit(UploadValidator.MaxBytes + 1_048_576)] 
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        var error = await UploadValidator.ValidateAsync(file);
        if (error is not null)
            return BadRequest(new { error });

        var ext = Path.GetExtension(file!.FileName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{ext}";  

        Directory.CreateDirectory(StorageDir);
        await using (var fs = System.IO.File.Create(Path.Combine(StorageDir, storedName)))
            await file.CopyToAsync(fs);

        var displayName = Path.GetFileName(file.FileName);
        if (displayName.Length > 255) displayName = displayName[..255];

        var record = new StoredFile
        {
            OwnerId = UserId,
            OriginalName = displayName,
            StoredName = storedName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            Status = "Available"
        };
        db.Files.Add(record);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Download), new { id = record.Id },
            new { id = record.Id, name = record.OriginalName, size = record.SizeBytes, status = record.Status });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id)
    {
        var record = await db.Files.FindAsync(id);

        if (record is null || record.OwnerId != UserId || record.Status != "Available")
            return NotFound();

        var path = Path.Combine(StorageDir, record.StoredName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, record.ContentType, record.OriginalName);
    }
}