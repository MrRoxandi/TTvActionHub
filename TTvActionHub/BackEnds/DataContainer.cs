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
                Logger.Log(LogType.Error, ServiceName, $"Error deserializing item '{name}' to type {typeof(T).Name}.", ex);
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
                        Logger.Log(LogType.Error, ServiceName, $"Error deserializing item '{name}' to type {typeof(T).Name}.", ex);
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
            Logger.Log(LogType.Info, ServiceName, $"Item '{name}' was removed successfully.");
            return true;
        }

        public async Task<bool> RemoveItemAsync(string name)
        {
            var dataTable = _db.DataTable.FirstOrDefault(t => t.Name == name);
            if (dataTable == null) return true;
            _db.DataTable.Remove(dataTable);
            await _db.SaveChangesAsync();
            Logger.Log(LogType.Info, ServiceName, $"Item '{name}' was removed successfully.");
            return true;
        }

        public bool Contains(string name) => _db.DataTable.Any(t => t.Name == name);

        public async Task<bool> ContainsAsync(string name) => await _db.DataTable.AnyAsync(t => t.Name == name);
        
    }
}