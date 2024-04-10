using Microsoft.Extensions.FileProviders;
using UploadChunked.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(o =>
{
    o.MaximumReceiveMessageSize = 20 * 1024 * 1024;
    o.StreamBufferCapacity = 1000 * 1024 * 1024;
    o.EnableDetailedErrors = true;
});

builder.Services.AddSingleton(new Dictionary<string,List<ChunkViewModelSignalR>>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "node_modules")),
    RequestPath = "/vendor"
});

app.UseRouting();


app.MapHub<UploadFileHub>("home/upload");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
