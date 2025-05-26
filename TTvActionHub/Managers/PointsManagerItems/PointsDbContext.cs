using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TTvActionHub.Managers.PointsManagerItems;

public partial class PointsDbContext : DbContext
{
    private readonly string _dbPath;

    public DbSet<PointsData> Users { get; set; }

    public PointsDbContext(string tag)
    {
        _dbPath = Path.Combine(Directory.GetCurrentDirectory(), ".storage", $"{tag}.points.db");
        var folderPath = Path.GetDirectoryName(_dbPath);
        if (folderPath != null && !Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    public void EnsureCreated() => Database.EnsureCreated();
    public async Task EnsureCreatedAsync() => await Database.EnsureCreatedAsync();
    public void EnsureDeleted() => Database.EnsureDeleted();
    public async Task EnsureDeletedAsync() => await Database.EnsureDeletedAsync();
    public async Task<int> SaveChangesAsync() => await base.SaveChangesAsync();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var folderPath = Path.GetDirectoryName(_dbPath);
        if (folderPath != null && !Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var sb = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };
        optionsBuilder.UseSqlite(sb.ToString());
    }
}