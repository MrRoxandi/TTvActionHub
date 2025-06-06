using TTvActionHub.BackEnds;
using MainLogger = TTvActionHub.BackEnds.MainLogger;

namespace TTvActionHub.Logs
{
    internal enum LogType
    {
        Error = -1, Info = 0, Warning = 1
    }

    internal static class Logger
    {
        public static ILogger InnerLogger = new MainLogger();

        public static void Error(string message, Exception? err = null)
        {
            InnerLogger.Error(message, err);
        }

        public static void Info(string message)
        {
            InnerLogger.Info(message);
        }

        public static void Warn(string message)
        {
            InnerLogger.Warn(message); 
        }

        public static void Log(LogType type, string name, string message, Exception? err = null)
        {
            var res = type switch
            {
                LogType.Error => "Err",
                LogType.Info => "Info",
                LogType.Warning => "Warn",
                _ => "NULL"
            };

            InnerLogger.Log(res, name, message, err);
        }

        public static IEnumerable<string> LastLogs() => InnerLogger.GetLastLogs();

    }
}
