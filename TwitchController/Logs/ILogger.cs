using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchController.Logs
{
    internal interface ILogger
    {
        public Task Error(string message, string? err = null);

        public Task Info(string message);

        public Task Warn(string message);

        public Task Log(string type, string name, string message, string? err = null);

    }
}
