using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.UI;

[Serializable, NetSerializable]
public enum InteractionWindowUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class InteractionWindowBoundUserInterfaceState(NetEntity userEntity, NetEntity targetEntity, List<string> availableInteractionIds) : BoundUserInterfaceState
{
    public NetEntity UserEntity { get; } = userEntity;
    public NetEntity TargetEntity { get; } = targetEntity;
    public List<string> AvailableInteractionIds { get; } = availableInteractionIds;
}

[Serializable, NetSerializable]
public sealed class InteractionMessage : BoundUserInterfaceMessage
{
    public string InteractionId { get; }
    public bool IsCustom { get; }
    public CustomInteractionData? CustomData { get; }

    public InteractionMessage(string interactionId)
    {
        InteractionId = interactionId;
        IsCustom = false;
        CustomData = null;
    }

    public InteractionMessage(string interactionId, CustomInteractionData customData)
    {
        InteractionId = interactionId;
        IsCustom = true;
        CustomData = customData;
    }
}

[Serializable, NetSerializable]
public sealed class RequestSavePosAndCloseMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CustomInteractionData
{
    public string InteractionMessage { get; }
    public string? SoundId { get; }
    public bool SpawnsEffect { get; }
    public float EffectChance { get; }
    public string? EntityEffectId { get; }
    public float Cooldown { get; }

    public CustomInteractionData(
        string message,
        string? soundId,
        bool spawnsEffect,
        float effectChance,
        string? entityEffectId,
        float cooldown)
    {
        InteractionMessage = message;
        SoundId = soundId;
        SpawnsEffect = spawnsEffect;
        EffectChance = effectChance;
        EntityEffectId = entityEffectId;
        Cooldown = cooldown;
    }
}

[CVarDefs]
public sealed class InteractionsCVars
{
    public static readonly CVarDef<bool> EmoteVisibility =
        CVarDef.Create("interactions.emote", true, CVar.CLIENTONLY);

    public static readonly CVarDef<bool> Expand =
        CVarDef.Create("interactions.expand", false, CVar.CLIENTONLY);

    public static readonly CVarDef<string> OpenInteractionCategories =
        CVarDef.Create("interactions.open_categories", "", CVar.CLIENTONLY);

    public static readonly CVarDef<string> FavoriteInteractions =
        CVarDef.Create("interactions.favorites", "", CVar.CLIENTONLY);

    public static readonly CVarDef<int> WindowPosX =
        CVarDef.Create("interactions.window_pos_x", 0, CVar.CLIENTONLY);

    public static readonly CVarDef<int> WindowPosY =
        CVarDef.Create("interactions.window_pos_y", 0, CVar.CLIENTONLY);
}
