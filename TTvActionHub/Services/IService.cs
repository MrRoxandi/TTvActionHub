namespace TTvActionHub.Services
{
    internal interface IService
    {
        public void Run();
        public void Stop();
        public string ServiceName { get; }
    }
}
