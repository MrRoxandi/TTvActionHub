using System.Collections.Concurrent;
using TTvActionHub.Services.Http.Events;

namespace TTvActionHub.Services.Http
{
    internal static class Repository
    {
        public static IEvent? LastEvent = null;
        public static Queue<IEvent> Events = [];
        public static bool XD = false;
    }
}
