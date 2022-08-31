using Microsoft.EntityFrameworkCore;

namespace ImagesGet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Image> Images => Set<Image>();
}