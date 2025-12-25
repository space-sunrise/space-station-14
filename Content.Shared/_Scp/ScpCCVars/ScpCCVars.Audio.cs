using Robust.Shared.Configuration;

namespace Content.Shared._Scp.ScpCCVars;

[CVarDefs]
public sealed partial class ScpCCVars
{
    /**
     * Echo
     */

    /// <summary>
    /// Будет ли использован эффект эхо?
    /// </summary>
    public static readonly CVarDef<bool> EchoEnabled =
        CVarDef.Create("scp.echo_enabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Будет ли использован пресет сильного эха?
    /// </summary>
    public static readonly CVarDef<bool> EchoStrongPresetPreferred =
        CVarDef.Create("scp.echo_strong_preset_preferred", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /**
     * Audio muffling
     */

    /// <summary>
    /// Будет ли подавление звуков в зависимости от видимости работать?
    /// </summary>
    public static readonly CVarDef<bool> AudioMufflingEnabled =
        CVarDef.Create("scp.audio_muffling_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Будет ли использована частая проверка параметров для подавления звуков?
    /// </summary>
    public static readonly CVarDef<bool> AudioMufflingHighFrequencyUpdate =
        CVarDef.Create("scp.audio_muffling_use_high_frequency_update", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
