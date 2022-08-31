namespace ImagesGet;

public sealed class Image
{
    public int Id { get; set; }
    public string? FileName { get; set; }
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
}