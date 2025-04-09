using TTvActionHub.Services;

namespace TTvActionHub.LuaTools.Stuff
{
    public static class Storage
    {
        public static ContainerService? Container = null;

        public static bool Contains(string name)
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            ArgumentException.ThrowIfNullOrEmpty($"Invalid name of an item [{name}]", nameof(name));
            return Container.Contains(name);
        }

        public static void InsertValue<T>(string name, T value) where T : notnull
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            Container.AddOrUpdateItem(name, value);
        }

        public static async Task InsertValueAsync<T>(string name, T value) where T : notnull
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            await Container.AddOrUpdateItemAsync(name, value);
        }


        public static T? GetValue<T>(string name) where T : class
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            return Container.GetItem<T>(name);
        }

        public static async Task<T?> GetValueAsync<T>(string name) where T : class
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            return await Container.GetItemAsync<T>(name);
        }

        public static bool RemoveValue(string name)
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            return Container.RemoveItem(name);
        }

        public static async Task<bool> RemoveValueAsync(string name)
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            return await Container.RemoveItemAsync(name);
        }


        // Basic reps

        public static void InsertInt(string name, int value)
        {
            InsertValue(name, value);
        }

        public static int? GetInt(string name)
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            object? item = Container.GetItem<object?>(name);
            if (item is int val) return val;
            else return null;
        }

        public static void InsertChar(string name, char value)
        {
            InsertValue(name, value);
        }

        public static char? GetChar(string name)
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            object? item = Container.GetItem<object?>(name);
            if (item is char val) return val;
            else return null;
        }

        // bool
        public static void InsertBool(string name, bool value)
        {
            InsertValue(name, value);
        }

        public static bool? GetBool(string name)
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            object? item = Container.GetItem<object?>(name);
            if (item is bool val) return val;
            else return null;
        }

        // string
        public static void InsertString(string name, string value)
        {
            InsertValue(name, value);
        }

        public static string? GetString(string name)
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            object? item = Container.GetItem<object?>(name);
            if (item is string val) return val;
            else return null;
        }

        // double
        
        public static void InsertDouble(string name, double value)
        {
            InsertValue(name, value);
        }

        public static double? GetDouble(string name)
        {
            ArgumentNullException.ThrowIfNull(Container, nameof(Container));
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
            }
            object? item = Container.GetItem<object?>(name);
            if (item is double val) return val;
            else return null;
        }
    }
}
