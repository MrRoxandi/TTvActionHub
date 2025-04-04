namespace TTvActionHub.Services
{
    public class ServiceStatusEventArgs(string serviceName, bool isRunninng, string? message) : EventArgs
    {
        public bool IsRunning { get; } = isRunninng;
        public string ServiceName { get; } = serviceName;
        public string? Message { get; } = message;
    }
    
    internal interface IService
    {
        public void Run();
        public void Stop();
        public string ServiceName { get; }

        // Events
        event EventHandler<ServiceStatusEventArgs> StatusChanged;

        // Status
        bool IsRunning { get; }
    }
}
