namespace TTvActionHub.ShellItems.InnerCommands;
using Interfaces;

public class LogViewInnerCommand : IInnerCommand
{
    public static string CommandName => "Logs";
    public string CommandDescription => "Switches shell mode to show logs";

    public bool Execute(Shell shell, string[] arguments)
    {
        shell.ToggleLogView(true);
        return true;
    }
}