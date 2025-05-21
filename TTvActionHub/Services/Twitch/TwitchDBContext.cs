using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TTvActionHub.Services.Twitch;

public partial class TwitchDbContext : DbContext
{
    private static string DbPath => Path.Combine(Directory.GetCurrentDirectory(), ".storage", "TwitchUsers.db");
    
    public DbSet<TwitchUser> Users { get; set; }
    public TwitchDbContext()
    {
        var folderPath = Path.GetDirectoryName(DbPath);
        if (folderPath != null && !Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }
    
    public void EnsureCreated() => Database.EnsureCreated();
    public async Task EnsureCreatedAsync() => await Database.EnsureCreatedAsync();
    public void EnsureDeleted() => Database.EnsureDeleted();
    public async Task EnsureDeletedAsync() => await Database.EnsureDeletedAsync();
    public async Task<int> SaveChangesAsync() => await base.SaveChangesAsync();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var folderPath = Path.GetDirectoryName(DbPath);
        if (folderPath != null && !Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        
        var sb = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath, 
            Mode = SqliteOpenMode.ReadWriteCreate
        };
        optionsBuilder.UseSqlite(sb.ToString());
    }
}