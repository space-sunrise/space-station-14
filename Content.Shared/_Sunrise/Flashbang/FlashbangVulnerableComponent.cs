using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Flashbang;

/// <summary>
/// Сущности с этим компонентом получают усиленный эффект вспышки.
/// <see cref="BypassProtection"/> управляет игнорированием экипировочной защиты,
/// <see cref="EffectMultiplier"/> — множителем длительности стана и падения.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlashbangVulnerableComponent : Component
{
    /// <summary>Множитель длительности стана и падения. 2 = вдвое дольше.</summary>
    [DataField, AutoNetworkedField]
    public float EffectMultiplier = 1f;

    /// <summary>Если true — защита от экипировки игнорируется и knockdown принудительный.</summary>
    [DataField, AutoNetworkedField]
    public bool BypassProtection = false;
}
