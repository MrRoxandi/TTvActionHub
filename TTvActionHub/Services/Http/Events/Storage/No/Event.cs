namespace TTvActionHub.Services.Http.Events.Storage.No
{
    internal class Event : IEvent
    {
        private readonly string content = @"
            <div>
                <h2> YES </h2>
            </div>
        ";

        public string Dispatch()
        {
            return content;
        }
    }
}