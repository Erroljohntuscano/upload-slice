using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using UploadApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=files.db"));
builder.Services.AddAuthentication("Dev")
    .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("Dev", null);
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { } 