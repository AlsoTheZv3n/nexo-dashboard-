namespace Dashboard.PowerShell;

public sealed class PowerShellOptions
{
    public const string SectionName = "PowerShell";

    public string ScriptsDirectory { get; set; } = "./powershell/scripts";
    public int TimeoutSeconds { get; set; } = 60;
}
