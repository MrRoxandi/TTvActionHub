using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTvActionHub.LuaTools.Services.ContainerItems
{
    public interface IDataBaseContext
    {
        public DbSet<TwitchUser> Users { get; }
        public DbSet<JSONTable> DataTable { get; }
        public void EnsureCreated();
        public Task EnsureCreatedAsync();
        public void EnsureDeleted();
        public Task EnsureDeletedAsync();
        public void SaveChanges();
        public Task SaveChangesAsync();
    }
}
