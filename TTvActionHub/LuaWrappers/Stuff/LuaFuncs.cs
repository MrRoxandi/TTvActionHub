using System.Text;
using Lua;
using TTvActionHub.BackEnds.Abstractions;

namespace TTvActionHub.LuaWrappers.Stuff;

[LuaObject]
public partial class LuaFuncs
{
    [LuaMember]
    public static int RandomNumber(int min, int max) => Funcs.RandomNumber(min, max);
    
    [LuaMember]
    public static double RandomDouble(double min, double max) => Funcs.RandomDouble(min, max);

    [LuaMember]
    public static LuaValue RandomElement(LuaTable elements)
    {
        if (elements.ArrayLength == 0) return LuaValue.Nil;
        var enumerable = elements.GetArraySpan().ToArray();
        return enumerable.ElementAt(Random.Shared.Next(enumerable.Length));
    }
    
    [LuaMember]
    public static LuaValue Shuffle(LuaTable elements)
    {
        if (elements.ArrayLength == 0) return LuaValue.Nil;
        var span = elements.GetArraySpan();
        Random.Shared.Shuffle(span);
        var table = new LuaTable();
        foreach (var (value, index) in span.ToArray().Select((v, i) => (v, i + 1)))
        {
            table.Insert(index, value);
        }

        return table;
    }
    [LuaMember]
    public static string RandomString(int length) => Funcs.RandomString(length);
    
    [LuaMember]
    public static void Delay(int delay) => Funcs.Delay(delay);
    
    [LuaMember]
    public static string CollectionToString(LuaTable elements, string sep = " ")
    {
        if (elements.ArrayLength == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var value in elements.GetArraySpan())
        {
            sb.Append(value.ToString());
        }
        return sb.ToString();
    }
}