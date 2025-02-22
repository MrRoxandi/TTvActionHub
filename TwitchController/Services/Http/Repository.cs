using System.Collections.Concurrent;
using TwitchController.Services.Http.Events;

namespace TwitchController.Services.Http
{
    internal static class Repository
    {
        public static IEvent? LastEvent = null;
        public static Queue<IEvent> Events = [];
        public static bool XD = false;
    }
}
