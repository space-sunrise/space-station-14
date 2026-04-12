using Robust.Shared.Console;

namespace Content.Client._Sunrise.Sandbox;

/// <summary>
/// Console command that toggles the mapping transparency overlay for mapper admins.
/// </summary>
public sealed class ShowMappingTransparencyCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MappingTransparencySystem _mappingTransparency = default!;

    /// <summary>
    /// Gets the console verb used to toggle the overlay.
    /// </summary>
    public override string Command => "showmappingtransparency";

    /// <summary>
    /// Toggles the mapping transparency overlay and reports the resulting state to the caller.
    /// </summary>
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
