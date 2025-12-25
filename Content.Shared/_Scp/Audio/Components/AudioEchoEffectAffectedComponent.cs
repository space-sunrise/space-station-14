using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Audio.Components;

/// <summary>
/// Компонент-маркер, указывающий, что звук получил эффект эха.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AudioEchoEffectAffectedComponent : Component
{
    /// <summary>
    /// Пресет эффекта эха, который был использован к этому звуку.
    /// Нужен, чтобы после убрать именно его при необходимости
    /// </summary>
    [ViewVariables] public ProtoId<AudioPresetPrototype> Preset;
}
