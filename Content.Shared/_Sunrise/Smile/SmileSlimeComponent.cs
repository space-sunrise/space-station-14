using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Smile;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class SmileSlimeComponent : Component
{
    /// <summary>
    /// Действие обнимашек
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public EntProtoId Action;

    [AutoNetworkedField]
    [DataField("actionEntity")]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Сколько восстанавливают обнимашки
    /// </summary>
    [DataField]
    public DamageSpecifier DamageSpecifier;

    /// <summary>
    /// Звук обнимашек
    /// </summary>
    [DataField]
    public SoundSpecifier SoundSpecifier;

    /// <summary>
    /// Тест, который будет появляться над целью обнимашек
    /// </summary>
    [DataField]
    public string AffectionPopupText = "smile-affection-popup";

    /// <summary>
    /// Сущность, которая спавнится при обнимашках
    /// </summary>
    [DataField]
    public string EffectPrototype = "EffectHearts";

    /// <summary>
    /// Сколько длятся обнимашки
    /// </summary>
    [DataField]
    public TimeSpan ActionTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Какие сообщения пишутся при приближении к смайлу
    /// </summary>
    [DataField("messages")]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> Messages = default!;
}
