using Robust.Shared.Console;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

/// <summary>
/// Эта часть содержит вспомогательные команды
/// </summary>
public sealed partial class CultMirrorShieldSystem
{
    [Dependency] private readonly IConsoleHost _console = default!;

    private void InitializeCommands()
    {
    }
}
