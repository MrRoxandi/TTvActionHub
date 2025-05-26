
namespace TTvActionHub.ShellItems.InnerCommands;
using Interfaces;

public class ExitInnerCommand : IInnerCommand
{
    public static string CommandName => "Exit";
    public string CommandDescription => "Stops the program";
    public bool Execute(Shell shell, string[] arguments)
    {
        shell.Stop();
        return true;
    }
}