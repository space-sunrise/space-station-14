using Content.Shared._Sunrise.Research.TechnologyDisk;
using Robust.Shared.Audio;

namespace Content.Server._Sunrise.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class SunriseDiskConsoleComponent : Component
{
    /// <summary>
    /// Диски, доступные для печати, и их стоимость в очках исследований.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<SunriseDiskConsolePrintOption> DiskOptions =
    [
        new("ResearchDisk", 1000),
        new("ResearchDisk5000", 5000),
        new("ResearchDisk10000", 10000),
    ];

    /// <summary>
    /// Время печати одного диска.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Звук печати диска.
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
