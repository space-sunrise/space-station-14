using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Shared.Roles;

public sealed partial class JobPrototype
{
    /// <summary>
    /// Включает специальное оповещение экипажа о latejoin этой роли.
    /// </summary>
    [DataField]
    public bool JoinNotifyCrew { get; private set; }

    [DataField("radioBold")]
    public bool RadioIsBold { get; private set; }

    /// <summary>
    /// Запрещает выбирать роль указанным видам.
    /// </summary>
    [DataField("speciesBlacklist", customTypeSerializer: typeof(PrototypeIdListSerializer<SpeciesPrototype>))]
    public List<string> SpeciesBlacklist = new();

    /// <summary>
    /// Заставляет latejoin роли использовать job spawnpoint.
    /// </summary>
    [DataField("alwaysUseSpawner")]
    public bool AlwaysUseSpawner { get; private set; }

    [DataField]
    public SpriteSpecifier PreviewIcon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_Sunrise/Interface/Misc/job_preview.rsi"), "test");

    /// <summary>
    /// Список альтернативных названий должности.
    /// </summary>
    [DataField]
    public List<LocId> AlternativeTitles { get; private set; } = new();
}
