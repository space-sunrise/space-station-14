using Content.Shared.Examine;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Research.Artifact.Effects.AddComponentsInRadius;

/// <summary>
/// Добавляет всем подходящим под вайтлист сущностням в переданном радиусе переданные компоненты
/// Если вайтлист пуст, то добавляет компоненты ВСЕМ СУЩНОСТЯМ ВОКРУГ
/// </summary>
[RegisterComponent]
public sealed partial class AddComponentsInRadiusComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;

    [DataField, ViewVariables]
    public float Radius = ExamineSystemShared.ExamineRange;

    [DataField]
    public EntityWhitelist? Whitelist;
}
