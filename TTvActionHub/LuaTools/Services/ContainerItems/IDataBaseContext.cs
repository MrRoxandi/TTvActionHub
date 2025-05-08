using Microsoft.EntityFrameworkCore;

namespace TTvActionHub.LuaTools.Services.ContainerItems
{
    public interface IDataBaseContext
    {
        public DbSet<TwitchUser> Users { get; }
        public DbSet<JsonTable> DataTable { get; }
        public void EnsureCreated();
        public Task EnsureCreatedAsync();
        public void EnsureDeleted();
        public Task EnsureDeletedAsync();
        public void SaveChanges();
        public Task SaveChangesAsync();
    }
}
