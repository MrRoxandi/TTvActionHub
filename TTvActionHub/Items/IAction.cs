using Lua;

namespace TTvActionHub.Items
{
    public interface IAction
    {
        public LuaFunction Function { get; set; }
    }
}
