using System.Net;
using System.Text;

namespace TTvActionHub.Services.Http
{
    internal class Service : IService
    {
        HttpListener Listener = new();
        Dictionary<string, Func<Task<string>>> Endpoints = new();
        Thread thread;
        bool running = true;

        public event EventHandler<ServiceStatusEventArgs> StatusChanged;

        public Service(string adress = "http://localhost", string port = "8888")
        {
            thread = new Thread(this.ThreadStart);
            Listener.Prefixes.Add(adress + ":" + port + "/");

            Mapper.Map(this);
        }

        public void Map(string endpoint, Func<Task<string>> func)
        {
            this.Endpoints.Add(endpoint, func); 
        }

        public void Run()
        {
            thread.Start();
        }

        public void Stop()
        {
            this.running = false;
            thread.Join();
        }

        private async void ThreadStart()
        {
            Listener.Start();

            while (running)
            {
                var context = await Listener.GetContextAsync();
                await HandleAsync(context);
            }

            Listener.Stop();
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var output = response.OutputStream;

            var responseText = "";

            try
            {
                responseText = await (this.Endpoints[request.RawUrl])();
            }
            catch (KeyNotFoundException)
            {
                response.StatusCode = 404;
                response.Close();
            }

            var buffer = Encoding.UTF8.GetBytes(responseText);

            await output.WriteAsync(buffer);
            await output.FlushAsync();
            response.Close();
        }

        public string ServiceName { get => throw new NotImplementedException(); }

        public bool IsRunning => throw new NotImplementedException();
    }
}
