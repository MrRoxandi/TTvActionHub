using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TTvActionHub.Logs;
using TTvActionHub.LuaTools.Services.ContainerItems;

namespace TTvActionHub.BackEnds
{
    public sealed class DataContainer
    {
        private const string ServiceName = "Container";
        private readonly IDataBaseContext _db;
        
        public DataContainer(IDataBaseContext context)
        {
            _db = context;
            _db.EnsureCreated();
        }

        // --- User related methods ---
        public void AddPointsToUser(string username, long points)
        {
            var user = _db.Users.FirstOrDefault(te => te.Name == username);
            user ??= new TwitchUser { Name = username };
            user.Points += points;
            if (user.Id == 0)
            {
                _db.Users.Add(user);
            }
            _db.SaveChanges();
        }

        public long GetPointsFromUser(string username)
        {
            var user = _db.Users.FirstOrDefault(te => te.Name == username);
            return user?.Points ?? 0;
        }

        public async Task<long> GetPointsFromUserAsync(string username)
        {
            var user = await _db.Users.FirstOrDefaultAsync(te => te.Name == username);
            return user?.Points ?? 0;
        }

        public async Task AddPointsToUserAsync(string username, long points)
        {
            var user = _db.Users.FirstOrDefault(te => te.Name == username);
            user ??= new() { Name = username };
            user.Points += points;
            if (user.Id == 0)
            {
                await _db.Users.AddAsync(user);
            }
            await _db.SaveChangesAsync();
        }

        public void AddAdditionalInfoToUser(string username, string data)
        {
            var user = _db.Users.FirstOrDefault(te => te.Name == username);
            user ??= new() { Name = username };
            user.AdditionalInfo = data;
            if (user.Id == 0)
            {
                _db.Users.Add(user);
            }
            _db.SaveChanges();
        }

        public async Task AddAdditionalInfoToUserAsync(string username, string data)
        {
            var user = _db.Users.FirstOrDefault(te => te.Name == username);
            user ??= new() { Name = username };
            user.AdditionalInfo = data;
            if (user.Id == 0)
            {
                await _db.Users.AddAsync(user);
            }
            await _db.SaveChangesAsync();
        }

        // --- Random data related methods ---

        public void AddOrUpdateItem<T>(string name, T value)
        {
            var dataTable = _db.DataTable.FirstOrDefault(t => t.Name == name);
            dataTable ??= new() { Name = name };
            dataTable.JsonData = JsonSerializer.Serialize(value);
            if (dataTable.Id == 0)
                _db.DataTable.Add(dataTable);
            _db.SaveChanges();
        }

        public async Task AddOrUpdateItemAsync<T>(string name, T value)
        {
            var dataTable = _db.DataTable.FirstOrDefault(t => t.Name == name);
            dataTable ??= new() { Name = name };
            dataTable.JsonData = JsonSerializer.Serialize(value);
            if (dataTable.Id == 0)
                await _db.DataTable.AddAsync(dataTable);
            await _db.SaveChangesAsync();
        }

        public T? GetItem<T>(string name)
        {
            var dataTable = _db.DataTable.FirstOrDefault(t => t.Name == name);
            if (dataTable == null) return default;
            try
            {
                return JsonSerializer.Deserialize<T>(dataTable.JsonData);
            } 
            catch (Exception ex)
            {
                Logger.Log(LogType.ERROR, ServiceName, $"Error deserializing item '{name}' to type {typeof(T).Name}.", ex);
                return default;
            }
        }

        public async Task<T?> GetItemAsync<T>(string name)
        {
            return await Task.Run(
                () =>
                {
                    var dataTable = _db.DataTable.FirstOrDefault(t => t.Name == name);
                    if (dataTable == null) return default;
                    try
                    {
                        return JsonSerializer.Deserialize<T>(dataTable.JsonData);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.ERROR, ServiceName, $"Error deserializing item '{name}' to type {typeof(T).Name}.", ex);
                        return default;
                    }
                }
            ).ConfigureAwait(false);
        }

        public bool RemoveItem(string name)
        {
            var dataTable = _db.DataTable.FirstOrDefault(t => t.Name == name);
            if (dataTable == null) return false;
            _db.DataTable.Remove(dataTable);
            _db.SaveChanges();
            Logger.Log(LogType.INFO, ServiceName, $"Item '{name}' was removed successfully.");
            return true;
        }

        public async Task<bool> RemoveItemAsync(string name)
        {
            var dataTable = _db.DataTable.FirstOrDefault(t => t.Name == name);
            if (dataTable == null) return true;
            _db.DataTable.Remove(dataTable);
            await _db.SaveChangesAsync();
            Logger.Log(LogType.INFO, ServiceName, $"Item '{name}' was removed successfully.");
            return true;
        }

        public bool Contains(string name) => _db.DataTable.Any(t => t.Name == name);

        public async Task<bool> ContainsAsync(string name) => await _db.DataTable.AnyAsync(t => t.Name == name);
        
    }
}