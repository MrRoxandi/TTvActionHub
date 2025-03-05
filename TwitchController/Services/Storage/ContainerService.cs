using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwitchController.Logs;
using TwitchController.Services.Storage;

namespace TwitchController.Services
{
    public class ContainerService : IService
    {
        private ConcurrentDictionary<string, string> _storage = new();
        private string _fullpath = string.Empty;

        public void Run()
        {
            var _dir = Path.Combine(Directory.GetCurrentDirectory(), "container");
            _fullpath = Path.Combine(_dir, "data.dat");

            Directory.CreateDirectory(_dir);

            if (!File.Exists(_fullpath))
            {
                File.Create(_fullpath).Close(); // Explicitly close the file stream
            }
            else
            {
                ReadDataFromDisk();
            }
            Logger.Log(LOGTYPE.INFO, ServiceName(), "Service is running");
        }

        public void Stop()
        {
            SaveDataToDisk().Wait(); // Ensure data is saved before stopping
        }

        public string ServiceName() => "ContainerService";

        public void AddOrUpdateItem<T>(string name, T value)
        {
            string serializedValue = JsonSerializer.Serialize(value);
            _storage.AddOrUpdate(name, serializedValue, (_, __) => serializedValue);
            Logger.Log(LOGTYPE.INFO, ServiceName(), $"Field [{name}] was updated with value [{value}]");
        }

        public async Task AddOrUpdateItemAsync<T>(string name, T value)
        {
            string serializedValue = JsonSerializer.Serialize(value);
            await Task.Run(() => _storage.AddOrUpdate(name, serializedValue, (_, __) => serializedValue));
            Logger.Log(LOGTYPE.INFO, ServiceName(), $"Field [{name}] was updated with value [{value}]");
        }

        public T? GetItem<T>(string name) => _storage.ContainsKey(name) switch
        {
            true => JsonSerializer.Deserialize<T>(_storage[name]),
            _ => default
        };

        public async Task<T?> GetItemAsync<T>(string name) => 
            await Task.Run(() =>
            {
                if (_storage.TryGetValue(name, out var serializedItem))
                {
                    return JsonSerializer.Deserialize<T>(serializedItem);
                }
                return default;
            });

        public bool RemoveItem(string name) {
            bool result = _storage.TryRemove(name, out _);
            if(result)
                Logger.Log(LOGTYPE.INFO, ServiceName(), $"Field [{name}] was removed");
            return result;
        }

        public async Task<bool> RemoveItemAsync(string name)
        {
            bool result = await Task.Run(() => _storage.TryRemove(name, out _));
            if (result)
                Logger.Log(LOGTYPE.INFO, ServiceName(), $"Field [{name}] was removed");
            return result;
        }

        public bool Contains(string name) => _storage.ContainsKey(name);

        public async Task<bool> ContainsAsync(string name) => await Task.Run(() => _storage.ContainsKey(name));

        private async Task SaveDataToDisk()
        {
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                string jsonData = JsonSerializer.Serialize(_storage, options);
                await File.WriteAllTextAsync(_fullpath, jsonData);
                Logger.Log(LOGTYPE.INFO, ServiceName(), "Data was saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName(), "Error saving data", ex.Message);
            }
        }

        private void ReadDataFromDisk()
        {
            try
            {
                var fileInfo = new FileInfo(_fullpath);
                if (fileInfo.Length > 0)
                {
                    var jsonData = File.ReadAllText(_fullpath);
                    JsonSerializerOptions options = new()
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var deserializedStorage = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(jsonData, options)
                        ?? new ConcurrentDictionary<string, string>();

                    _storage = new ConcurrentDictionary<string, string>(deserializedStorage);
                    Logger.Log(LOGTYPE.INFO, ServiceName(), "Data was readed successfully");
                }
                else Logger.Log(LOGTYPE.INFO, ServiceName(), $"Nothing to read from {_fullpath}");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName(), "Error while loading data from disk", ex.Message);
                _storage = new ConcurrentDictionary<string, string>();
            }
        }

        public int GetStorageCount() => _storage.Count;

        public async Task<int> GetStorageCountAsync() => await Task.Run(() => _storage.Count);

        public IEnumerable<string> GetAllKeys() => _storage.Keys;

        public async Task<IEnumerable<string>> GetAllKeysAsync() => await Task.Run(() => _storage.Keys);

    }
}