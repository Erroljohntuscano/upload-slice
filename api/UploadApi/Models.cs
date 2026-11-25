using Microsoft.EntityFrameworkCore;

namespace UploadApi;

public class StoredFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public string OriginalName { get; set; } = "";   // display only, never used as a path
    public string StoredName { get; set; } = "";     // generated name on disk
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Status { get; set; } = "Available";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<StoredFile> Files => Set<StoredFile>();
}