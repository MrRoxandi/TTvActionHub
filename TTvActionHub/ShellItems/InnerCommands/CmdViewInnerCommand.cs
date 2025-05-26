namespace TTvActionHub.ShellItems.InnerCommands;
using Interfaces;

public class CmdViewInnerCommand : IInnerCommand
{
    public static string CommandName => "Cmd";
    public string CommandDescription => "Switches shell mode to show commands logs";

    public bool Execute(Shell shell, string[] arguments)
    {
        shell.ToggleLogView(false);
        return true;
    }
}