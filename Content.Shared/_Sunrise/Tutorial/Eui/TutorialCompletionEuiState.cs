using System;
using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Tutorial.Eui;

public static class TutorialCompletionActions
{
    public const string Leave = "leave";
    public const string Stay = "stay";
}

[Serializable, NetSerializable]
public sealed partial record TutorialCompletionEuiAction(string Id, string Text, string? Tooltip, bool Disabled);

[Serializable, NetSerializable]
public sealed partial class TutorialCompletionEuiState(
    string title,
    string description,
    List<TutorialCompletionEuiAction> actions,
    string protoId) : EuiStateBase
{
    public readonly string Title = title;
    public readonly string Description = description;
    public readonly string ProtoId = protoId;
    public readonly List<TutorialCompletionEuiAction> Actions = actions;
}

[Serializable, NetSerializable]
public sealed partial class TutorialCompletionEuiActionMessage(string actionId) : EuiMessageBase
{
    public readonly string ActionId = actionId;
}
