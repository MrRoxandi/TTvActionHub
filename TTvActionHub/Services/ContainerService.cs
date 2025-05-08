using System.Collections.Concurrent;
using System.Text.Json;
using TTvActionHub.Logs;

namespace TTvActionHub.Services
{
    public sealed class ContainerService: IService
    {
        private const int SaveIntervalMinutes = 1;

        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
        public string ServiceName => "Container"; 
        public bool IsRunning => _runningState;

        private ConcurrentDictionary<string, string> _storage = new();
        private readonly string _fullPath;
        private volatile bool _runningState;
        private Timer? _saveTimer;

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true, 
            PropertyNameCaseInsensitive = true 
        };

        public ContainerService() 
        {
            var baseDirectory = Directory.GetCurrentDirectory();
            var containerDirectory = Path.Combine(baseDirectory, "container");
            _fullPath = Path.Combine(containerDirectory, "data.dat");
            try
            {
                Directory.CreateDirectory(containerDirectory);
                if (!File.Exists(_fullPath))
                {
                    File.WriteAllText(_fullPath, "{}");
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Data file created at {_fullPath}");
                }
                else
                {
                    ReadDataFromDiskAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error during initial setup or data loading: {ex.Message}", ex);
                _storage = [];
            }
        }

        private void OnStatusChanged(bool isRunning, string? message = null)
        {
            try
            {
                StatusChanged?.Invoke(this, new ServiceStatusEventArgs(ServiceName, isRunning, message));
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error invoking StatusChanged event handler.", ex);
            }

        }

        public void Run()
        {
            _runningState = true;
            OnStatusChanged(true);
            _saveTimer = new Timer(AutoSaveToDiskAsync, null, TimeSpan.Zero, TimeSpan.FromMinutes(SaveIntervalMinutes));
            Logger.Log(LOGTYPE.INFO,  ServiceName, "Service is running");
        }

        public void Stop()
        {
            SaveDataToDiskAsync().GetAwaiter().GetResult();
            _runningState = false;
            OnStatusChanged(false);
            _saveTimer?.Dispose();
            _saveTimer = null;
            Logger.Log(LOGTYPE.INFO,  ServiceName, "Service is stopped");
        }

        public void AddOrUpdateItem<T>(string name, T value)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, JsonSerializerOptions);
                _storage.AddOrUpdate(name, serializedValue, (_, _) => serializedValue);
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Item '{name}' was added/updated.");
            }
            catch (JsonException jsonEx)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error serializing item '{name}'. Value not added/updated.", jsonEx);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Generic error adding/updating item '{name}'.", ex);
            }
        }

        public async Task AddOrUpdateItemAsync<T>(string name, T value)
        {
            try
            {
                await Task.Run(() =>
                {
                    var serializedValue = JsonSerializer.Serialize(value, JsonSerializerOptions);
                    _storage.AddOrUpdate(name, serializedValue, (_, __) => serializedValue);
                }).ConfigureAwait(false);
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Item '{name}' was added/updated asynchronously.");
            }
            catch (JsonException jsonEx)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error serializing item '{name}' asynchronously. Value not added/updated.", jsonEx);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Generic error adding/updating item '{name}' asynchronously.", ex);
            }
        }

        public T? GetItem<T>(string name)
        {
            if (!_storage.TryGetValue(name, out var serializedItem)) return default;
            try
            {
                return JsonSerializer.Deserialize<T>(serializedItem, JsonSerializerOptions);
            }
            catch (JsonException ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error deserializing item '{name}' to type {typeof(T).Name}.", ex);
                return default;
            }
        }

        public async Task<T?> GetItemAsync<T>(string name)
        {
            return await Task.Run(() =>
            {
                if (!_storage.TryGetValue(name, out var serializedItem)) return default;
                try
                {
                    return JsonSerializer.Deserialize<T>(serializedItem, JsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error deserializing item '{name}' to type {typeof(T).Name} asynchronously.", ex);
                    return default;
                }
            }).ConfigureAwait(false);
        }

        public bool RemoveItem(string name)
        {
            var result = _storage.TryRemove(name, out _);
            if (result)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Item '{name}' was removed.");
            }
            return result;
        }

        public async Task<bool> RemoveItemAsync(string name)
        {
            var result = await Task.Run(() => _storage.TryRemove(name, out _)).ConfigureAwait(false);
            if (result)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Item '{name}' was removed asynchronously.");
            }
            return result;
        }

        public bool Contains(string name) => _storage.ContainsKey(name);

        public async Task<bool> ContainsAsync(string name) => 
            await Task.Run(() => _storage.ContainsKey(name)).ConfigureAwait(false);

        private async void AutoSaveToDiskAsync(object? state)
        {
            await SaveDataToDiskAsync();
        }

        public async Task SaveDataToDiskAsync()
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(_storage, JsonSerializerOptions);
                await File.WriteAllTextAsync(_fullPath, jsonData).ConfigureAwait(false);
                Logger.Log(LOGTYPE.INFO, ServiceName, "Data saved to disk successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error saving data to disk.", ex);
            }
        }

        private async Task ReadDataFromDiskAsync()
        {
            try
            {
                var fileInfo = new FileInfo(_fullPath);
                if (fileInfo is { Exists: true, Length: > 0 }) 
                {
                    var jsonData = await File.ReadAllTextAsync(_fullPath).ConfigureAwait(false);
                    var deserializedStorage = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(jsonData, JsonSerializerOptions);

                    _storage = deserializedStorage ?? new ConcurrentDictionary<string, string>();
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Data loaded from disk successfully. {_storage.Count} items.");
                }
                else
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Data file at '{_fullPath}' is empty or does not exist. Starting with an empty storage.");
                    _storage = new ConcurrentDictionary<string, string>();
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error deserializing data from disk (JSON format issue). File: '{_fullPath}'. Starting with an empty storage.", jsonEx);
                var backupPath = _fullPath + ".corrupted." + DateTime.Now.ToString("ddMMyyyyHHmmss");
                try { File.Move(_fullPath, backupPath); } catch (Exception moveEx) { Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to backup corrupted data file.", moveEx); }
                _storage = new ConcurrentDictionary<string, string>();
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error loading data from disk. File: '{_fullPath}'. Starting with an empty storage.", ex);
                _storage = new ConcurrentDictionary<string, string>();
            }
        }


        public int GetStorageCount() => _storage.Count;

        public async Task<int> GetStorageCountAsync() =>
            await Task.Run(() => _storage.Count).ConfigureAwait(false);

        public IEnumerable<string> GetAllKeys() => _storage.Keys;

        public async Task<IEnumerable<string>> GetAllKeysAsync() =>
            await Task.Run(() => (IEnumerable<string>)[.. _storage.Keys]).ConfigureAwait(false);

    }
}