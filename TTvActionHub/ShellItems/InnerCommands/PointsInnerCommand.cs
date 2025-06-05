using System.Text;

namespace TTvActionHub.ShellItems.InnerCommands;
using TTvActionHub.Services.Interfaces;
using Interfaces;

public class PointsInnerCommand : IInnerCommand
{
    public static string CommandName => "Points";
    public string CommandDescription => "Manages user points for services";

    private static string BaseUsageMessage =>
        $"Usage: {CommandName.ToLowerInvariant()} <service_name> <action> [args...]. Use '{CommandName.ToLowerInvariant()} help' for more details.";

    public bool Execute(Shell shell, string[] arguments)
    {
        switch (arguments.Length)
        {
            case 0:
                shell.CmdOut(BaseUsageMessage);
                shell.CmdOut($"Example: {CommandName.ToLowerInvariant()} twitch list");
                return false;
            case 1 when arguments[0].Equals("help", StringComparison.InvariantCultureIgnoreCase):
                PrintUsage(shell);
                return true;
            case < 2:
                shell.CmdOut("Too few arguments.");
                shell.CmdOut(BaseUsageMessage);
                return false;
        }

        var serviceNameArg = arguments[0].Trim();
        var action = arguments[1].ToLowerInvariant();
        var actionArgs = arguments[2..];

        var properServiceName = shell.GetProperServiceName(serviceNameArg);
        if (string.IsNullOrEmpty(properServiceName))
        {
            shell.CmdOut($"Unable to find: {serviceNameArg}");
            shell.CmdOut(BaseUsageMessage);
            return false;
        }

        if (shell.GetServiceByNameCallBack == null)
        {
            shell.CmdOut($"Unable to get service by name: {serviceNameArg}. Feature is not configured.");
            return false;
        }

        var service = shell.GetServiceByNameCallBack(properServiceName);

        if (service is not IPointsService pointsService)
        {
            shell.CmdOut($"Service with name: {properServiceName} not supported.");
            return false;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                switch (action)
                {
                    case "add":
                        await HandleAddPoints(shell, pointsService, actionArgs);
                        break;
                    case "set":
                        await HandleSetPoints(shell, pointsService, actionArgs);
                        break;
                    case "get":
                        await HandleGetPoints(shell, pointsService, actionArgs);
                        break;
                    case "list":
                        await HandleListPoints(shell, pointsService);
                        break;
                    case "export":
                        await HandleExportPoints(shell, pointsService, actionArgs);
                        break;
                    default:
                        shell.CmdOut(
                            $"Unknown action '{action}' for points management on service '{properServiceName}'.");
                        PrintUsage(shell, properServiceName); // Печатаем usage для конкретного сервиса
                        break;

                }
            }
            catch (Exception ex)
            {
                shell.HandleCallbackError($"{CommandName} {properServiceName} {action}", ex);
            }
        });
        return true;
    }

    private static async Task<bool> HandleAddPoints(Shell shell, IPointsService pointsService, string[] args)
    {
        if (args.Length < 2)
        {
            shell.CmdOut(
                $"Usage: {CommandName.ToLowerInvariant()} {pointsService.ServiceName.ToLowerInvariant()} add <username_or_id> <amount> [--id]");
            return false;
        }

        var target = args[0];
        if (!long.TryParse(args[1], out var amount) || amount == 0)
        {
            shell.CmdOut("Invalid amount. Must be a non-zero integer.");
            return false;
        }

        var byId = args.Length > 2 && args[2].Equals("--id", StringComparison.InvariantCultureIgnoreCase);
        bool success;

        shell.CmdOut($"Attempting to add {amount} points for target '{target}' on {pointsService.ServiceName}...");

        if (byId)
        {
            success = await pointsService.AddPointsByIdAsync(target, amount);
            var username = await pointsService.GetUserNameByIdAsync(target) ?? target; // Попытка получить имя для лога
            shell.CmdOut(success
                ? $"Successfully added {amount} points to user ID {target} (User: {username}). Current points might be visible with 'get' command."
                : $"Failed to add points to user ID {target} (User: {username}). Check logs or user existence.");
        }
        else
        {
            success = await pointsService.AddPointsAsync(target, amount);
            shell.CmdOut(success
                ? $"Successfully added {amount} points to {target}. Current points might be visible with 'get' command."
                : $"Failed to add points to {target}. User might not exist, or an error occurred.");
        }

        return success;
    }

    private static async Task<bool> HandleSetPoints(Shell shell, IPointsService pointsService, string[] args)
    {
        if (args.Length < 2)
        {
            shell.CmdOut(
                $"Usage: {CommandName.ToLowerInvariant()} {pointsService.ServiceName.ToLowerInvariant()} set <username_or_id> <amount> [--id]");
            return false;
        }

        var target = args[0];
        if (!long.TryParse(args[1], out var amount) || amount < 0)
        {
            shell.CmdOut("Invalid amount. Must be a non-negative integer.");
            return false;
        }

        var byId = args.Length > 2 && args[2].Equals("--id", StringComparison.InvariantCultureIgnoreCase);
        bool success;

        shell.CmdOut($"Attempting to set points to {amount} for target '{target}' on {pointsService.ServiceName}...");

        if (byId)
        {
            success = await pointsService.SetPointsByIdAsync(target, amount);
            var username = await pointsService.GetUserNameByIdAsync(target) ?? target;
            shell.CmdOut(success
                ? $"Successfully set points for user ID {target} (User: {username}) to {amount}."
                : $"Failed to set points for user ID {target} (User: {username}).");
        }
        else
        {
            success = await pointsService.SetPointsAsync(target, amount);
            shell.CmdOut(success
                ? $"Successfully set points for {target} to {amount}."
                : $"Failed to set points for {target}. User might not exist, or an error occurred.");
        }

        return success;
    }

    private static async Task<bool> HandleGetPoints(Shell shell, IPointsService pointsService, string[] args)
    {
        if (args.Length < 1)
        {
            shell.CmdOut(
                $"Usage: {CommandName.ToLowerInvariant()} {pointsService.ServiceName.ToLowerInvariant()} get <username_or_id> [--id]");
            return false;
        }

        var target = args[0];
        var byId = args.Length > 1 && args[1].Equals("--id", StringComparison.InvariantCultureIgnoreCase);
        long points;

        shell.CmdOut($"Fetching points for target '{target}' on {pointsService.ServiceName}...");

        if (byId)
        {
            points = await pointsService.GetPointsByIdAsync(target);
            var username = await pointsService.GetUserNameByIdAsync(target);
            shell.CmdOut(username != null
                ? $"User ID {target} (Name: {username}) has {points} points on {pointsService.ServiceName}."
                : $"User ID {target} (username not resolved by service) has {points} points on {pointsService.ServiceName}.");
        }
        else
        {
            points = await pointsService.GetPointsAsync(target);
            shell.CmdOut($"User {target} has {points} points on {pointsService.ServiceName}.");
        }

        return true;
    }

    private static async Task<bool> HandleListPoints(Shell shell, IPointsService pointsService)
    {
        shell.CmdOut($"Fetching all user points for {pointsService.ServiceName}...");
        var allPoints = await pointsService.GetAllUsersPointsAsync();
        if (allPoints.Count == 0)
        {
            shell.CmdOut($"No users with points found on {pointsService.ServiceName}.");
            return true;
        }

        shell.CmdOut($"User Points on {pointsService.ServiceName}:");
        foreach (var kvp in allPoints.OrderByDescending(kvp => kvp.Value))
        {
            shell.CmdOut($"- {kvp.Key}: {kvp.Value}");
        }

        return true;
    }

    private static async Task<bool> HandleExportPoints(Shell shell, IPointsService pointsService, string[] args)
    {
        var serviceNameSanitized =
            new string(pointsService.ServiceName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var defaultFileName = $"{serviceNameSanitized}_points_{DateTime.Now:yyyyMMddHHmmss}.csv";
        var filePath = args.Length > 0 ? args[0] : defaultFileName;

        if (!filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            filePath += ".csv";
        }

        if (!Path.IsPathRooted(filePath) && string.IsNullOrEmpty(Path.GetDirectoryName(filePath)))
        {
            filePath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        }
        else if (!Path.IsPathRooted(filePath)) // Если есть имя папки, но не полный путь
        {
            filePath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                shell.HandleCallbackError($"Points export: Error creating directory {directory}", ex);
                return false;
            }
        }


        shell.CmdOut($"Exporting points for {pointsService.ServiceName} to {filePath}...");

        var allUserPoints = await pointsService.GetAllUsersPointsAsync();

        if (allUserPoints.Count == 0)
        {
            shell.CmdOut($"No points data to export for {pointsService.ServiceName}.");
            return false;
        }

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Username,Points"); // CSV Header
            foreach (var kvp in allUserPoints.OrderByDescending(kvp => kvp.Value))
            {
                var usernameCsv = EscapeCsvField(kvp.Key);
                sb.AppendLine($"{usernameCsv},{kvp.Value}");
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
            shell.CmdOut($"Points for {pointsService.ServiceName} exported successfully to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            shell.HandleCallbackError($"Points export to {filePath}", ex);
            return false;
        }
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private static void PrintUsage(Shell shell, string? forService = null)
    {
        var cmd = CommandName.ToLowerInvariant();
        shell.CmdOut($"{CommandName} Command Usage:");
        if (string.IsNullOrEmpty(forService))
        {
            shell.CmdOut($"  {cmd} <service_name> <action> [arguments...]");
            shell.CmdOut($"  {cmd} help - Shows this help message.");
            shell.CmdOut(
                $"  For specific service actions, type: {cmd} <service_name> help (not yet implemented, use '{cmd} help')");

        }
        else
        {
            var serviceNameLower = forService.ToLowerInvariant();
            shell.CmdOut($"Actions for service '{forService}':");
            shell.CmdOut(
                $"  {cmd} {serviceNameLower} add <username_or_id> <amount> [--id] - Adds points. Use --id if first arg is User ID.");
            shell.CmdOut(
                $"  {cmd} {serviceNameLower} set <username_or_id> <amount> [--id] - Sets points. Use --id if first arg is User ID.");
            shell.CmdOut(
                $"  {cmd} {serviceNameLower} get <username_or_id> [--id]          - Gets points. Use --id if first arg is User ID.");
            shell.CmdOut(
                $"  {cmd} {serviceNameLower} list                               - Lists all users and their points for this service.");
            shell.CmdOut(
                $"  {cmd} {serviceNameLower} export [filepath.csv]              - Exports all points for this service to a CSV file.");
        }

        shell.CmdOut($"Example: {cmd} twitch add someuser 100");
        shell.CmdOut($"Example: {cmd} twitch get 12345678 --id");
        shell.CmdOut($"Example: {cmd} twitch export my_twitch_points.csv");
    }
}
