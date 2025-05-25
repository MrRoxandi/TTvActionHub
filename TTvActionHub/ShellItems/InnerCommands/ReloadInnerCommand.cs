namespace TTvActionHub.ShellItems.InnerCommands;
using Interfaces;

public class ReloadInnerCommand : IInnerCommand
{
    public static string CommandName => "Reload";
    public string CommandDescription => "Allows to reload configuration of specified service";

    public bool Execute(Shell shell, string[] arguments)
    {
        var target = arguments.Length > 0 ? arguments[0].Trim() : string.Empty;
        if (string.IsNullOrEmpty(target))
        {
            shell.CmdOut($"Usage: reload [<service_name>|{string.Join('|', shell.ServiceStates.Keys)}]");
            return false;
        }
        var properServiceName = shell.ServiceStates.Keys.FirstOrDefault(key => key.Equals(target, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(properServiceName))
        {
            shell.CmdOut($"Unable to find: {target}");
            shell.CmdOut($"Usage: reload [<field>|{string.Join('|', shell.ServiceStates.Keys)}]");
            return false;
        }

        if (shell.ReloadServiceCallBack is null)
        {
            shell.CmdOut("reload service functionality is not configured. Ignoring...");
            return false;
        } 
        try
        {
            shell.ReloadServiceCallBack(properServiceName);
            return true;
        }
        catch (Exception ex)
        {
            shell.HandleCallbackError($"stop {properServiceName}", ex);
            return false;
        }
    }
}