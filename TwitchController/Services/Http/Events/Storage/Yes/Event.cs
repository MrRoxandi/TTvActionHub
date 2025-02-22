namespace TwitchController.Services.Http.Events.Storage.Yes
{
    internal class Event : IEvent
    {
        private readonly string content = @"
            <div>
                <h2> NO </h2>
            </div>
        ";

        public string Dispatch()
        {
            return content;
        }
    }
}
