using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchController.Services;

namespace TwitchController.LuaTools.Stuff
{
    public static class Storage
    {
        public static ContainerService? _service = null;

        public static bool Contains(string name)
        {
            if(_service is not null) return _service.Contains(name);
            return false;
        }

        public static async Task<bool> ContainsAsync(string name)
        {
            if(_service is not null) return await Task.Run(() => _service.Contains(name));
            return false;
        }

        public static async Task InsertValueAsync<T>(string name, T value) where T : notnull
        {
            await Task.Run(() => _service?.AddOrUpdateItemAsync(name, value));
        }

        public static void InsertValue<T>(string name, T value) where T : notnull
        {
            _service?.AddOrUpdateItem(name, value);
        }

        public static async Task<T?> GetValueAsync<T>(string name) where T : class
        {
            return await Task.Run(() => _service?.GetItemAsync<T>(name));
        }

        public static T? GetValue<T>(string name) where T: class
        {
            return _service?.GetItem<T>(name);
        }

        public static async Task<bool> RemoveValueAsync(string name)
        {
            if (_service is null) return false;
            return await Task.Run(() => _service.RemoveItemAsync(name));
        }

        public static bool RemoveValue(string name)
        {
            if (_service is null) return false;
            return _service.RemoveItem(name);
        }

        // Basic reps

        public static async Task InsertIntAsync(string name, int value)
        {
            await InsertValueAsync(name, value);
        }

        public static void InsertInt(string name, int value)
        {
            InsertValue(name, value);
        }

        public static async Task<int?> GetIntAsync(string name)
        {
            // Приводим результат к int, если таковой имеется
            object? item = await Task.Run(() => _service?.GetItemAsync<int>(name));
            return item is int result ? result : (int?)null;
        }

        public static int? GetInt(string name)
        {
            object? item = _service?.GetItem<int>(name);
            return item is int result ? result : (int?)null;
        }

        // char
        public static async Task InsertCharAsync(string name, char value)
        {
            await InsertValueAsync(name, value);
        }

        public static void InsertChar(string name, char value)
        {
            InsertValue(name, value);
        }

        public static async Task<char?> GetCharAsync(string name)
        {
            object? item = await Task.Run(() => _service?.GetItemAsync<char>(name));
            return item is char result ? result : (char?)null;
        }

        public static char? GetChar(string name)
        {
            object? item = _service?.GetItem<char>(name);
            return item is char result ? result : (char?)null;
        }

        // bool
        public static async Task InsertBoolAsync(string name, bool value)
        {
            await InsertValueAsync(name, value);
        }

        public static void InsertBool(string name, bool value)
        {
            InsertValue(name, value);
        }

        public static async Task<bool?> GetBoolAsync(string name)
        {
            object? item = await Task.Run(() => _service?.GetItemAsync<bool>(name));
            return item is bool result ? result : (bool?)null;
        }

        public static bool? GetBool(string name)
        {
            object? item = _service?.GetItem<bool>(name);
            return item is bool result ? result : (bool?)null;
        }

        // string
        public static async Task InsertStringAsync(string name, string value)
        {
            await InsertValueAsync(name, value);
        }

        public static void InsertString(string name, string value)
        {
            InsertValue(name, value);
        }

        public static async Task<string?> GetStringAsync(string name)
        {
            // Здесь мы ожидаем, что значение записано как string
            return await Task.Run(() => _service?.GetItemAsync<string>(name));
        }

        public static string? GetString(string name)
        {
            return _service?.GetItem<string>(name);
        }

        // double
        public static async Task InsertDoubleAsync(string name, double value)
        {
            await InsertValueAsync(name, value);
        }

        public static void InsertDouble(string name, double value)
        {
            InsertValue(name, value);
        }

        public static async Task<double?> GetDoubleAsync(string name)
        {
            object? item = await Task.Run(() => _service?.GetItemAsync<double>(name));
            return item is double result ? result : (double?)null;
        }

        public static double? GetDouble(string name)
        {
            object? item = _service?.GetItem<double>(name);
            return item is double result ? result : (double?)null;
        }
    }
}
