using Lua;
using System.Text;
namespace TTvActionHub.LuaWrappers.Stuff;

[LuaObject]
public partial class LuaFunctions
{
    public static string Chars => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    
    [LuaMember]
    public static int RandomNumber(int min, int max) => Random.Shared.Next(min, max);

    [LuaMember]
    public static double RandomDouble(double min, double max) => Random.Shared.NextDouble() * (max - min) + min;

    [LuaMember]
    public static bool Contains(LuaTable table, LuaValue value)
    {
        if (table.ArrayLength != 0)
            return table.GetArraySpan().Contains(value);
        return table[value].Type != LuaValueType.Nil;
    }
    
    [LuaMember]
    public static LuaValue RandomElement(LuaTable elements) => elements.ArrayLength == 0 ? LuaValue.Nil : elements[Random.Shared.Next(elements.ArrayLength) + 1];
    
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