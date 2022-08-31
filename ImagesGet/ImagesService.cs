using ImagesGet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ImagesGet;

public class ImagesService
{
    private readonly List<string> _filesName;
    private readonly AppDbContext _context;
    private readonly Random _random;

    public ImagesService(AppDbContext context, IMemoryCache cache)
    {
        _random = new Random();
        _context = context;
        
        if (!cache.TryGetValue("filesName", out _filesName))
            cache.Set("filesName", Directory.GetFiles("img").ToList());
        _filesName = cache.Get<List<string>>("filesName");
    }

    public async Task<ImageResponse?> GetByFileName(string fileName)
    {
        var path = Path.Combine("img", fileName);
        if (!File.Exists(path)) return null;
        var file = new FileInfo(path);
        var image = await _context.Images.FirstOrDefaultAsync(x => x.FileName == file.Name);
        return new ImageResponse
        {
            File = await File.ReadAllBytesAsync(path),
            FileName = file.Name,
            LikedCount = image?.LikeCount ?? 0
        };
    }

    public async Task<(string?, string?)> UploadImage(IFormFileCollection fileCollection)
    {
        var ms = new MemoryStream();
        foreach (var file in fileCollection)
        {
            var splitFileName = file.FileName.Split(".");
            await file.CopyToAsync(ms);
            if (!IsImage(ms.ToArray().AsSpan(0, 4)))
                return (null, "The file format is invalid");
            
            var fileName = GenerateFileName(ms.GetHashCode().ToString(), splitFileName.Last());
            var path = Path.Combine("img", fileName);
            
            await File.WriteAllBytesAsync(path, ms.ToArray());
            _filesName.Add(path);
            return (fileName, null);
        }

        return (null, null);
    }
    
    private static bool IsImage(Span<byte> bytes)
    {
        var flag = 0;
        if (bytes[0] == 255 || bytes[0] == 137) flag++;
        if (bytes[1] == 216 || bytes[1] == 80) flag++;
        if (bytes[2] == 255 || bytes[2] == 78) flag++;
        if (bytes[3] == 224 || bytes[3] == 71) flag++;
    
        return flag == 4;
    }

    private static string GenerateFileName(string str, string format)
    {
        return $"{str}{DateTime.Now:mmhhddMMyyyy}.{format}";
    }
    
    public async Task<ImageResponse> GetRandomImage()
    {
        var fileName = _filesName[_random.Next(0, _filesName.Count - 1)];
        var file = new FileInfo(fileName);
        var image = await _context.Images.FirstOrDefaultAsync(x => x.FileName == file.Name);
        
        return new ImageResponse
        {
            File = await File.ReadAllBytesAsync(fileName),
            FileName = file.Name,
            LikedCount = image?.LikeCount ?? 0
        };
    }

    public async Task UpdateLikeImage(string fileName)
    {
        var image = await _context.Images.FirstOrDefaultAsync(x => x.FileName == fileName);
        if (image is null)
        {
            var path = Path.Combine("img", fileName);
            if (!_filesName.Contains(path))
                throw new Exception("Image not found");
    
            image = new Image
            {
                LikeCount = 0,
                FileName = fileName
            };
            await _context.AddAsync(image);
        }
    
        image.LikeCount++;
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteImage(string fileName)
    {
        var image = await _context.Images.FirstOrDefaultAsync(x => x.FileName == fileName);

        if (image is null)
        {
            image = new Image
            {
                FileName = fileName,
                DislikeCount = 0
            };
            await _context.AddAsync(image);
        }

        image.DislikeCount++;

        if (image.DislikeCount == 2)
        {
            _context.Remove(image);
            var path = Path.Combine("img", fileName);
            File.Delete(path);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<string?>> GetTop(int count)
    {
        return await _context.Images
            .OrderByDescending(x => x.LikeCount)
            .Select(x => x.FileName)
            .Skip(0)
            .Take(count)
            .ToListAsync();
    }

    public async Task HardDelete()
    {
        var images = await _context.Images.Where(x => x.DislikeCount >= 1).ToListAsync();
        foreach (var image in images)
        {
            var path = Path.Combine("img", image.FileName);
            File.Delete(path);
            _filesName.Remove(image.FileName);
        }
        _context.RemoveRange(images);
        await _context.SaveChangesAsync();
    }
}