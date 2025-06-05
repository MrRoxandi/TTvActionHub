namespace TTvActionHub.BackEnds.Abstractions;

public static class Container
{
    public static DataContainer? Storage = null;

    public static bool Contains(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        ArgumentException.ThrowIfNullOrEmpty($"Invalid name of an item [{name}]", nameof(name));
        return Storage.Contains(name);
    }

    public static void InsertValue<T>(string name, T value) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        Storage.AddOrUpdateItem(name, value);
    }

    public static async Task InsertValueAsync<T>(string name, T value) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        await Storage.AddOrUpdateItemAsync(name, value);
    }


    public static T? GetValue<T>(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        return Storage.GetItem<T>(name);
    }

    public static async Task<T?> GetValueAsync<T>(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        return await Storage.GetItemAsync<T>(name);
    }

    public static bool RemoveValue(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        return Storage.RemoveItem(name);
    }

    public static async Task<bool> RemoveValueAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        return await Storage.RemoveItemAsync(name);
    }


    // Basic reps

    public static void InsertInt(string name, int value)
    {
        InsertValue(name, value);
    }

    public static int? GetInt(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        if (Storage.Contains(name)) return Storage.GetItem<int>(name);
        return null;
    }

    public static void InsertChar(string name, char value)
    {
        InsertValue(name, value);
    }

    public static char? GetChar(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        if (Storage.Contains(name)) return Storage.GetItem<char>(name);
        return null;
    }

    // bool
    public static void InsertBool(string name, bool value)
    {
        InsertValue(name, value);
    }

    public static bool? GetBool(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        if (Storage.Contains(name)) return Storage.GetItem<bool>(name);
        return null;
    }

    // string
    public static void InsertString(string name, string value)
    {
        InsertValue(name, value);
    }

    public static string? GetString(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        return Storage.Contains(name) ? Storage.GetItem<string>(name) : null;
    }

    // double

    public static void InsertDouble(string name, double value)
    {
        InsertValue(name, value);
    }

    public static double? GetDouble(string name)
    {
        ArgumentNullException.ThrowIfNull(Storage, nameof(Storage));
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"Invalid name of an item [{name}]", nameof(name));
        var item = Storage.GetItem<object?>(name);
        if (item is double val) return val;
        return null;
    }
}