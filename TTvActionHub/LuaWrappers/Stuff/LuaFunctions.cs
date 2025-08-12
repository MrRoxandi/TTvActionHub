using Lua;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text;
namespace TTvActionHub.LuaWrappers.Stuff;

[LuaObject]
public partial class LuaFunctions
{
    public static string Chars => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    [LuaMember]
    public static bool IsLuaArray(LuaTable table) => table.ArrayLength > 0 && table.HashMapCount == 0;

    [LuaMember]
    public static int RandomNumber(int min, int max) => Random.Shared.Next(min, max);

    [LuaMember]
    public static double RandomDouble(double min, double max) => Random.Shared.NextDouble() * (max - min) + min;

    [LuaMember]
    public static bool Contains(LuaTable table, LuaValue value)
    {
        if (IsLuaArray(table)) return table.GetArraySpan().Contains(value);
        var previosKey = LuaValue.Nil;
        while (table.TryGetNext(previosKey, out var kvp))
        {
            if (kvp.Value.Equals(value)) return true;
        }
        return false;
    }

    [LuaMember]
    public static LuaValue RandomElement(LuaTable elements)
    {
        if (IsLuaArray(elements)) return elements[RandomNumber(1, elements.ArrayLength + 1)];
        var collected = new List<LuaValue>();
        var previosKey = LuaValue.Nil;
        while (elements.TryGetNext(previosKey, out var kvp))
        {
            collected.Add(kvp.Value);
        }
        return collected.ElementAt(RandomNumber(0, collected.Count));
    }
    
    [LuaMember]
    public static LuaValue Shuffle(LuaTable elements)
    {
        if (elements.ArrayLength == 0) return LuaValue.Nil;
        var indexedAndShuffled = elements.GetArraySpan()
            .ToArray().Take(elements.ArrayLength)
            .OrderBy(_ => Random.Shared.Next())
            .ToArray();
        var table = new LuaTable();
        for(var i = 0; i < indexedAndShuffled.Length; i++)
        {
            table.Insert(i + 1, indexedAndShuffled[i]);
        }
        return table;
    }

    [LuaMember]
    public static string RandomString(int length) => new([..Enumerable.Repeat(Chars, length).Select(s => s[Random.Shared.Next(s.Length)])]);
    
    [LuaMember]
    public static void Delay(int delay) => Thread.Sleep(delay);
    
    [LuaMember]
    public static LuaTable RandomPosition(int minX, int maxX, int minY, int maxY) => new() { [0] = RandomNumber(minX, maxX), [1] = RandomNumber(minY, maxY) };
    
    [LuaMember]
    public static string CollectionToString(LuaTable elements, string sep = " ")
    {
        return elements.ArrayLength == 0
            ? string.Empty
            : string.Join(sep,
                elements.GetArraySpan().ToArray().Where(item => item.Type != LuaValueType.Nil)
                    .Select(item => item.ToString()));
    }
}