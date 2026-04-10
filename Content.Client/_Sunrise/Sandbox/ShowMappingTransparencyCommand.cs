using Robust.Shared.Console;

namespace Content.Client._Sunrise.Sandbox;

public sealed class ShowMappingTransparencyCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MappingTransparencySystem _mappingTransparency = default!;

    public override string Command => "showmappingtransparency";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_mappingTransparency.TrySetEnabled(!_mappingTransparency.Enabled))
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-denied"));
            return;
        }

        shell.WriteLine(LocalizationManager.GetString(_mappingTransparency.Enabled
            ? $"cmd-{Command}-status-on"
            : $"cmd-{Command}-status-off"));
    }
}
