using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.TTS;

/// <summary>
/// Компонент, позволяющий выбирать случайный TTS голос из списка с шансами при спавне entity
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TTSRandomComponent : Component
{
    /// <summary>
    /// Словарь ID прототипов голосов с их шансами выпадения для случайного выбора.
    /// Чем выше число, тем больше шанс выпадения голоса.
    /// Если шанс не указан или равен 0, используется значение 1.
    /// </summary>
    [DataField("voices")]
    public Dictionary<ProtoId<TTSVoicePrototype>, int> Voices = new();
}
