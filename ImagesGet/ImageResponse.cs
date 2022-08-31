using System.IO;

namespace ImagesGet;

public sealed class ImageResponse
{
    public byte[] File { get; set; }
    public string FileName { get; set; }
    public int LikedCount { get; set; }
}