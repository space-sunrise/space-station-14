using Content.Shared._Sunrise.Lobby;
using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// ID прототипа набора фонов лобби.
    /// All означает, что будут использованы все доступные фоны
    /// </summary>
    public static readonly CVarDef<string> LobbyBackgroundPreset =
        CVarDef.Create("lobby.background_preset", "AllAllAll", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Определяет, какой тип фона лобби будет использован.
    /// Random, Art, Animation или Parallax
    /// </summary>
    /// <seealso cref="LobbyArtPrototype"/>
    /// <seealso cref="LobbyAnimationPrototype"/>
    /// <seealso cref="LobbyParallaxPrototype"/>
    public static readonly CVarDef<string> LobbyBackgroundType =
        CVarDef.Create("lobby.background", "Random", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Определяет, какой лобби-арт будет использован. Позволяет игроку выбрать один арт, который будет использован постоянно.
    /// Random означает, что будет выбираться случайно из списка
    /// </summary>
    /// <seealso cref="LobbyArtPrototype"/>
    public static readonly CVarDef<string> LobbyArt =
        CVarDef.Create("lobby.art", "Random", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Определяет, какая лобби-анимация будет использована. Позволяет игроку выбрать одну анимацию, который будет использован постоянно.
    /// Random означает, что будет выбираться случайно из списка
    /// </summary>
    /// <seealso cref="LobbyAnimationPrototype"/>
    public static readonly CVarDef<string> LobbyAnimation =
        CVarDef.Create("lobby.animation", "Random", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Определяет, какой лобби-параллакс будет использован. Позволяет игроку выбрать один параллакс, который будет использован постоянно.
    /// Random означает, что будет выбираться случайно из списка
    /// </summary>
    /// <seealso cref="LobbyParallaxPrototype"/>
    public static readonly CVarDef<string> LobbyParallax =
        CVarDef.Create("lobby.parallax", "Random", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether to unload lobby resources from video memory when switching backgrounds or entering round.
    /// </summary>
    public static readonly CVarDef<bool> LobbyUnloadResources =
        CVarDef.Create("lobby.unload_resources", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Прозрачность интерфейса лобби (0.0-1.0)
    /// </summary>
    public static readonly CVarDef<float> LobbyOpacity =
        CVarDef.Create("lobby.lobby_opacity", 0.90f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
