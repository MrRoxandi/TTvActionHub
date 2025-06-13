namespace TTvActionHub.ShellItems.Interfaces;

public interface IInnerCommand
{
    public static string CommandName { get; } = string.Empty;

    public string CommandDescription { get; }
    public bool Execute(Shell shell, string[] arguments);
}
