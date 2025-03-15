using TTvActionHub.Logs;
using TTvActionHub.Logs.BackEnds;

namespace TTvActionHub.Logs
{
    enum LOGTYPE : int
    {
        ERROR = -1, INFO = 0, WARNING = 1
    }

    internal static class Logger
    {
        private static readonly ILogger _logger = new ConsoleFileLogger();

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

        public static void Log(LOGTYPE type, string name, string message, string? err = null)
        {
            string res = type switch
            {
                LOGTYPE.ERROR => "ERR",
                LOGTYPE.INFO => "INFO",
                LOGTYPE.WARNING => "WARN",
                _ => "NULL"
            };

            _logger.Log(res, name, message, err);
        }

    }
}
