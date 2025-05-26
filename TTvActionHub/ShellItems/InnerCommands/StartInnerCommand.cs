namespace TTvActionHub.ShellItems.InnerCommands;
using Interfaces;

public class StartInnerCommand : IInnerCommand
{
    public static string CommandName => "Start";
    public string CommandDescription => "Allows to start specified service";

    public bool Execute(Shell shell, string[] arguments)
    {
        var target = arguments.Length > 0 ? arguments[0].Trim() : string.Empty;
        var properServiceName = shell.GetProperServiceName(target);
        if (string.IsNullOrEmpty(properServiceName))
        {
            shell.CmdOut($"Unable to find: {target}");
            shell.CmdOut($"Usage: stop [<field>|{string.Join('|', shell.ServiceStates.Keys)}]");
            return false;
        }

        if (shell.StartServiceCallBack is null)
        {
            shell.CmdOut("Start service functionality is not configured. Ignoring...");
            return false;
        } 
        try
        {
            shell.StartServiceCallBack(properServiceName);
            return true;
        }
        catch (Exception ex)
        {
            shell.HandleCallbackError($"start {properServiceName}", ex);
            return false;
        }
    }
}