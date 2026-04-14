using System.Numerics;
using Content.Server.Gravity;
using Content.Server.NPC.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    [Dependency] private readonly GravitySystem _gravity = default!;

    private float GetAcceleration(Entity<MovementSpeedModifierComponent?> ent, bool weightless)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return weightless ? MovementSpeedModifierComponent.DefaultWeightlessAcceleration : MovementSpeedModifierComponent.DefaultAcceleration;

        return weightless ? ent.Comp.WeightlessAcceleration : ent.Comp.Acceleration;
    }

    private float GetFriction(Entity<MovementSpeedModifierComponent?> ent, bool weightless)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return weightless ? MovementSpeedModifierComponent.DefaultWeightlessFriction : MovementSpeedModifierComponent.DefaultFriction;

        return weightless ? ent.Comp.WeightlessFriction : ent.Comp.Friction;
    }

    /// <summary>
    /// Gets the fraction this value is between min and max.
    /// </summary>
    private float MapValue(float value, float minValue, float maxValue)
    {
        if (maxValue > minValue)
        {
            var mapped = (value - minValue) / (maxValue - minValue);
            return Math.Clamp(mapped, 0f, 1f);
        }

        return value >= minValue ? 1f : 0f;
    }

    /// <summary>
    /// Determines the movement direction taking into account braking and tangential velocity correction.
    /// </summary>
    private void ApplySunriseMovement(
        Span<float> interest,
        Angle offsetRot,
        Vector2 direction,
        PhysicsComponent body,
        NPCSteeringComponent steering,
        float acceleration,
        float friction,
        float moveSpeed,
        float frameTime,
        bool finalInRange,
        bool velocityHigh,
        ref float moveMultiplier)
    {
        var velLen = body.LinearVelocity.Length();
        var haveToBrake = finalInRange && velocityHigh;

        var realAccel = acceleration * moveSpeed;
        var frameAccel = realAccel * frameTime;

        // check our tangential velocity
        var normVel = direction * Vector2.Dot(body.LinearVelocity, direction) / direction.LengthSquared();
        var tgVel = body.LinearVelocity - normVel;

        var moveType = SunriseMovementType.MovingToTarget;

        // we're near final node but haven't braked, do so
        if (haveToBrake)
        {
            // how much distance we'll pass before hitting our desired max speed
            var brakePath = (velLen - steering.InRangeMaxSpeed ?? 0f) / friction;
            var hardBrake = brakePath > MathF.Min(0.5f, steering.Range);

            moveType = hardBrake ? SunriseMovementType.Braking : SunriseMovementType.Coasting;
        }
        else
        {
            const float circlingTolerance = 0.5f;

            var dirLen = direction.Length();
            var arrived = dirLen <= steering.Range;
            var tangentialBrake = !arrived && realAccel * circlingTolerance < tgVel.LengthSquared() / dirLen;

            moveType = tangentialBrake ? SunriseMovementType.BrakingTangential : SunriseMovementType.MovingToTarget;
        }

        switch (moveType)
        {
            case SunriseMovementType.MovingToTarget:
                moveMultiplier = 1f;
                ApplySeek(interest, offsetRot.RotateVec(direction.Normalized()), 1f);
                break;
            case SunriseMovementType.Braking:
                if (velLen > 0f)
                {
                    var cvel = body.LinearVelocity;
                    _mover.Friction(0f, frameTime, friction, ref cvel);
                    moveMultiplier = MapValue(cvel.Length(), 0f, frameAccel);
                    ApplySeek(interest, -offsetRot.RotateVec(body.LinearVelocity / velLen), 1f);
                }
                break;
            case SunriseMovementType.BrakingTangential:
                if (velLen > 0f)
                {
                    moveMultiplier = MapValue(tgVel.Length(), 0f, frameAccel);
                    ApplySeek(interest, -offsetRot.RotateVec(tgVel.Normalized()), tgVel.Length() / velLen);
                }
                break;
            case SunriseMovementType.Coasting:
                moveMultiplier = 0f;
                break;
        }
    }

    private enum SunriseMovementType
    {
        MovingToTarget,
        Braking,
        BrakingTangential,
        Coasting,
    }
}
