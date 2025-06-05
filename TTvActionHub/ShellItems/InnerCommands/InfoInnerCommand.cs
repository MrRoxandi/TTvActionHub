namespace TTvActionHub.ShellItems.InnerCommands;
using Interfaces;

public class InfoInnerCommand : IInnerCommand
{
    public static string CommandName => "Info";
    public string CommandDescription => "Allows to get information about service";
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
        
        if (shell.ServiceInfoCallBack == null)
        {
            shell.CmdOut("Getting info about service is not configured. Ignoring...");
            return false;
        }

        try
        {
            var information = shell.ServiceInfoCallBack(properServiceName);
            if (information == null) return true;
            if (information.Length == 0)
            {
                shell.CmdOut($"Service: {properServiceName} -> Actions: empty");
                return true;
            }

            shell.CmdOut($"Service: {properServiceName} -> Actions: [{string.Join(',', information)}]");
            return true;
        }
        catch (Exception ex)
        {
            shell.HandleCallbackError($"info {properServiceName}", ex);
            return false;
        }
    }
}