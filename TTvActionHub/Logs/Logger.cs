using TTvActionHub.BackEnds;

namespace TTvActionHub.Logs
{
    internal enum LogType
    {
        Error = -1, Info = 0, Warning = 1
    }

    internal static class Logger
    {
        private static readonly ILogger _logger = new MainLogger();

        public static void Error(string message, Exception? err = null)
        {
            _logger.Error(message, err);
        }

        public static void Info(string message)
        {
            _logger.Info(message);
        }

        public static void Warn(string message)
        {
            _logger.Warn(message); 
        }

        public static void Log(LogType type, string name, string message, Exception? err = null)
        {
            string res = type switch
            {
                LogType.Error => "ERR",
                LogType.Info => "Info",
                LogType.Warning => "WARN",
                _ => "NULL"
            };

            _logger.Log(res, name, message, err);
        }

        public static IEnumerable<string> LastLogs() => _logger.GetLastLogs();

    }
}
