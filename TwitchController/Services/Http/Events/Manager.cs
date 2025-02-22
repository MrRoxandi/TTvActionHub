namespace TwitchController.Services.Http.Events
{
    internal static class Manager
    {
        public static void Enqueue(string s)
        {
            var ev = Create(s);
            Repository.Events.Enqueue(ev);
        }

        private static IEvent Create(string s)
        {
            IEvent res = s switch
            {
                "yes" => new Storage.Yes.Event(),
                "no" => new Storage.No.Event(),
                "fun" => new Storage.Fun.Event(),
                _ => throw new Exception("Undefined Http Event")
            };

            return res;
        }
    }
}
