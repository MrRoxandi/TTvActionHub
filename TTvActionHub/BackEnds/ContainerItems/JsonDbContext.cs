using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TTvActionHub.BackEnds.ContainerItems;

public partial class JsonDbContext : DbContext
{
    public DbSet<JsonTable> DataTable { get; set; }

    public void EnsureCreated() => Database.EnsureCreated();

    public async Task EnsureCreatedAsync() => await Database.EnsureCreatedAsync();

    public void EnsureDeleted() => Database.EnsureDeleted();

    public async Task EnsureDeletedAsync() => await Database.EnsureDeletedAsync();

    /*public async Task SaveChangesAsync() => await base.SaveChangesAsync();*/

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "container");
        Directory.CreateDirectory(folderPath);
        var a = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(folderPath, "d.db"),
            Mode = SqliteOpenMode.ReadWriteCreate
        };
        optionsBuilder.UseSqlite(a.ToString());
    }
}