using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Включены ли артефакты-предметы? Переключение этого в моменты игры динамически включает и выключает фичу.
    /// </summary>
    public static readonly CVarDef<bool> EnableRandomArtifacts =
        CVarDef.Create("random_artifacts.enable", false, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Соотношение артефактов-предметов к обычным предметам.
    /// </summary>
    public static readonly CVarDef<float> ItemToArtifactRatio =
        CVarDef.Create("random_artifacts.ratio", 0.55f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Включён ли узел артефакта, который превращает ближайшие предметы в случайные.
    /// При отключении уже сгенерированные узлы активируются без эффекта.
    /// </summary>
    public static readonly CVarDef<bool> ArtifactRandomTransformationEnabled =
        CVarDef.Create("artifact.random_transformation.enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);
}
