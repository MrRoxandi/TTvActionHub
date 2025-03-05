namespace TTvActionHub.LuaTools.Http
{
    public static class Events
    {
        public static void Enqueue(string s)
        {
            Services.Http.Events.Manager.Enqueue(s);
        }
    }
}
