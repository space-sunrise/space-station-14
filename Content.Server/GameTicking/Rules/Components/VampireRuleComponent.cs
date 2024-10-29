using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(VampireRuleSystem))]
public sealed partial class VampireRuleComponent : Component
{
    public readonly List<EntityUid> VampireMinds = new();
/*
    public readonly List<ProtoId<EntityPrototype>> Objectives = new()
    {
        "ChangelingSurviveObjective",
        "ChangelingStealDNAObjective",
        "EscapeIdentityObjective"
    }; */
}