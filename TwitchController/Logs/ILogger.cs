using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchController.Logs
{
    internal interface ILogger
    {
        public void Error(string message, string? err = null);

        public void Info(string message);

        public void Warn(string message);

        public void External(string type, string name, string message, string? err = null);

    }
}
