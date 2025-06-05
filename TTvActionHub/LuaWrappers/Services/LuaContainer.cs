using Lua;
using TTvActionHub.BackEnds.Abstractions;

namespace TTvActionHub.LuaWrappers.Services;

[LuaObject]
public partial class LuaContainer
{
    [LuaMember]
    public static bool Contains(string name) => Container.Contains(name);
    
    [LuaMember]
    public static void InsertValue(string name, LuaValue value) => Container.InsertValue(name, value);

    [LuaMember]
    public static LuaValue GetValue(string name) => Container.GetValue<LuaValue?>(name) ?? LuaValue.Nil;
    
    [LuaMember]
    public static bool RemoveValue(string name) => Container.RemoveValue(name);
    
    [LuaMember]
    public static void InsertInt(string name, int value) => Container.InsertInt(name, value);
    
    [LuaMember]
    public static LuaValue GetInt(string name) => Container.GetValue<int?>(name) ?? LuaValue.Nil;
    
    [LuaMember]
    public static void InsertString(string name, string value) => Container.InsertString(name, value);
    
    [LuaMember]
    public static LuaValue GetString(string name) => Container.GetValue<string?>(name) ?? LuaValue.Nil;
    
    public static void InsertBool(string name, bool value) => Container.InsertBool(name, value);
    
    public static LuaValue GetBool(string name) => Container.GetValue<bool?>(name) ?? LuaValue.Nil;
    
    public static void InsertDouble(string name, double value) => Container.InsertDouble(name, value);
    
    public static LuaValue GetDouble(string name) => Container.GetValue<double?>(name) ?? LuaValue.Nil;
    
}