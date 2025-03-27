﻿using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Boss.Components;

[RegisterComponent]
public sealed partial class HellSpawnRushComponent : Component
{
    [DataField]
    public EntityUid? RuneUid;

    [DataField]
    public Vector2 CameraKickback = new(-5f, -5f);

    [DataField]
    public float CameraKickRange = 3f;

    [DataField]
    public bool DoCameraKickOnLand = true;

    [DataField]
    public float Range = 8f;

    [DataField]
    public EntProtoId? RushAction = "ActionHellSpawnRush";

    [DataField] public EntityUid? RushActionEntity;

    [DataField]
    public DamageSpecifier ThrowHitDamageDict = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Brute", 50 },
            { "Structural", 150 }, // this ensures that structures like doors are destroyed
        },
    };

    [DataField]
    public EntityWhitelist? Blacklist;
}
