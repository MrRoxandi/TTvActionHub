using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;
using System.Text.Json;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Services.ContainerItems;
using TTvActionHub.Services;

namespace TTvActionHub.BackEnds
{
    public sealed class DataContainer
    {
        private static readonly string ServiceName = "Container";
        private IDataBaseContext db;
        
        public DataContainer(IDataBaseContext context)
        {
            db = context;
            db.EnsureCreated();
        }

        // --- User releated methods ---
        public void AddPointsToUser(string username, long points)
        {
            var user = db.Users.FirstOrDefault(te => te.Name == username);
            user ??= new() { Name = username };
            user.Points += points;
            if (user.ID == 0)
            {
                db.Users.Add(user);
            }
            db.SaveChanges();
        }

        public long GetPointsFromUser(string username)
        {
            var user = db.Users.FirstOrDefault(te => te.Name == username);
            return user?.Points ?? 0;
        }

        public async Task<long> GetPointsFromUserAsync(string username)
        {
            var user = await db.Users.FirstOrDefaultAsync(te => te.Name == username);
            return user?.Points ?? 0;
        }

        public async Task AddPointsToUserAsync(string username, long points)
        {
            var user = db.Users.FirstOrDefault(te => te.Name == username);
            user ??= new() { Name = username };
            user.Points += points;
            if (user.ID == 0)
            {
                await db.Users.AddAsync(user);
            }
            await db.SaveChangesAsync();
        }

        public void AddAdditionalInfoToUser(string username, string data)
        {
            var user = db.Users.FirstOrDefault(te => te.Name == username);
            user ??= new() { Name = username };
            user.AdditionalInfo = data;
            if (user.ID == 0)
            {
                db.Users.Add(user);
            }
            db.SaveChanges();
        }

        public async Task AddAdditionalInfoToUserAsync(string username, string data)
        {
            var user = db.Users.FirstOrDefault(te => te.Name == username);
            user ??= new() { Name = username };
            user.AdditionalInfo = data;
            if (user.ID == 0)
            {
                await db.Users.AddAsync(user);
            }
            await db.SaveChangesAsync();
        }

        // --- Random data releated methods ---

        public void AddOrUpdateItem<T>(string name, T value)
        {
            var dataTable = db.DataTable.FirstOrDefault(t => t.Name == name);
            dataTable ??= new() { Name = name };
            dataTable.JSONData = JsonSerializer.Serialize(value);
            if (dataTable.ID == 0)
                db.DataTable.Add(dataTable);
            db.SaveChanges();
        }

        public async Task AddOrUpdateItemAsync<T>(string name, T value)
        {
            var dataTable = db.DataTable.FirstOrDefault(t => t.Name == name);
            dataTable ??= new() { Name = name };
            dataTable.JSONData = JsonSerializer.Serialize(value);
            if (dataTable.ID == 0)
                await db.DataTable.AddAsync(dataTable);
            await db.SaveChangesAsync();
        }

        public T? GetItem<T>(string name)
        {
            var dataTable = db.DataTable.FirstOrDefault(t => t.Name == name);
            if (dataTable == null) return default;
            try
            {
                return JsonSerializer.Deserialize<T>(dataTable.JSONData);
            } 
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error deserializing item '{name}' to type {typeof(T).Name}.", ex);
                return default;
            }
        }

        public async Task<T?> GetItemAsync<T>(string name)
        {
            return await Task.Run(
                () =>
                {
                    var dataTable = db.DataTable.FirstOrDefault(t => t.Name == name);
                    if (dataTable == null) return default;
                    try
                    {
                        return JsonSerializer.Deserialize<T>(dataTable.JSONData);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error deserializing item '{name}' to type {typeof(T).Name}.", ex);
                        return default;
                    }
                }
            ).ConfigureAwait(false);
        }

        public bool RemoveItem(string name)
        {
            var dataTable = db.DataTable.FirstOrDefault(t => t.Name == name);
            if (dataTable == null) return false;
            db.DataTable.Remove(dataTable);
            db.SaveChanges();
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Item '{name}' was removed successfully.");
            return true;
        }

        public async Task<bool> RemoveItemAsync(string name)
        {
            var dataTable = db.DataTable.FirstOrDefault(t => t.Name == name);
            if (dataTable == null) return true;
            db.DataTable.Remove(dataTable);
            await db.SaveChangesAsync();
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Item '{name}' was removed successfully.");
            return true;
        }

        public bool Contains(string name) => db.DataTable.Any(t => t.Name == name);

        public async Task<bool> ContainsAsync(string name) => await db.DataTable.AnyAsync(t => t.Name == name);
        
    }
}