using ImagesGet;
using ImagesGet.Data;
using Microsoft.EntityFrameworkCore;

const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000/");

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy  =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("Content-Disposition")
                .WithExposedHeaders("FileName")
                .WithExposedHeaders("LikedCount")
                .WithExposedHeaders("Location");
        });
});

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ImagesService>();
builder.Services.AddMemoryCache();

var app = builder.Build();
app.UseCors(myAllowSpecificOrigins);
app.MapGet("/images", async (HttpResponse res, ImagesService images) =>
{
    var image = await images.GetRandomImage();
    res.Headers.Add("FileName", image.FileName);
    res.Headers.Add("LikedCount", $"{image.LikedCount}");
    return Results.File(image.File, "image/jpeg");
});

app.MapGet("/images/{name}", async (HttpResponse res, ImagesService images, string name) =>
{
    var image = await images.GetByFileName(name);
    if (image is null)
        return Results.NotFound();
    
    res.Headers.Add("FileName", image.FileName);
    res.Headers.Add("LikedCount", $"{image.LikedCount}");
    return Results.File(image.File, "image/jpeg");
});

app.MapPost("/images", async (HttpRequest req, ImagesService images) =>
{
    var result = await images.UploadImage(req.Form.Files);
    return result.Item2 is not null 
        ? Results.BadRequest(new { message = result.Item2 }) 
        : Results.Created($"https://art.ruhex.ru/images/{result.Item1}", null);
});

app.MapGet("/images/top/{count:int}", async (ImagesService images, int count) 
    => Results.Ok(await images.GetTop(count)));

app.MapPut("/images/{name}", async (ImagesService images, string name) =>
{
    await images.UpdateLikeImage(name);
    return Results.Ok();
});

app.MapDelete("/images/{name}", async (ImagesService images, string name) => 
{
    await images.DeleteImage(name);
    return Results.NoContent();
});

app.MapDelete("/images/hard", async (ImagesService images) =>
{
    await images.HardDelete();
    return Results.NoContent();
});

app.Run();