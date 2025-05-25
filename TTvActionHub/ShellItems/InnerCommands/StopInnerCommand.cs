namespace TTvActionHub.ShellItems.InnerCommands;
using Interfaces;

public class StopInnerCommand : IInnerCommand
{
    public static string CommandName => "Stop";
    public string CommandDescription => "Allows to stop specified service";

    public bool Execute(Shell shell, string[] arguments)
    {
        var target = arguments.Length > 1 ? arguments[0].Trim() : string.Empty;
        if (string.IsNullOrEmpty(target))
        {
            shell.CmdOut($"Usage: stop [<service_name>|{string.Join('|', shell.ServiceStates.Keys)}]");
            return false;
        }
        var properServiceName = shell.ServiceStates.Keys.FirstOrDefault(key => key.Equals(target, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(properServiceName))
        {
            shell.CmdOut($"Unable to find: {target}");
            shell.CmdOut($"Usage: stop [<field>|{string.Join('|', shell.ServiceStates.Keys)}]");
            return false;
        }

        if (shell.StopServiceCallBack is null)
        {
            shell.CmdOut("Stop service functionality is not configured. Ignoring...");
            return false;
        } 
        try
        {
            shell.StopServiceCallBack(properServiceName);
            return true;
        }
        catch (Exception ex)
        {
            shell.HandleCallbackError($"stop {properServiceName}", ex);
            return false;
        }
    }
}