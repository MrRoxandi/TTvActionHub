using System.Text;
using Terminal.Gui;

namespace TTvActionHub.ShellItems.InnerCommands;
using Interfaces;

public class HelpInnerCommand : IInnerCommand
{
    public static string CommandName => "Help";
    public string CommandDescription => "Shows help message";

    public bool Execute(Shell shell, string[] arguments)
    {
        var stringBuilder = new StringBuilder();
        foreach (var (name, command) in shell.InnerCommands.OrderByDescending(kvp => kvp.Key.Length + kvp.Value.CommandDescription.Length))
        {
            stringBuilder.AppendLine($"{name} : {command.CommandDescription}");
        }

        try
        {
            MessageBox.Query("Available commands", stringBuilder.ToString(), "Ok");
            return true;
        }
        catch (Exception ex)
        {
            shell.HandleCallbackError("help", ex);
            return false;
        }
    }
}