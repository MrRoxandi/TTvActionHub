using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TTvActionHub.Managers.AuthManagerItems;

public partial class AuthDbContext : DbContext
{
    public DbSet<TwitchAuthData> AuthenticationData  { get; set; }
    
    private static string DbPath => Path.Combine(Directory.GetCurrentDirectory(), ".storage", "AuthData.db");
    
    public AuthDbContext()
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
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
    
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