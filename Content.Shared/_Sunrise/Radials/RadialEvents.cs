using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Radials;

    [Serializable, NetSerializable]
    public sealed class RequestServerRadialsEvent : EntityEventArgs
    {
        public readonly NetEntity EntityUid;

        public readonly List<string> RadialTypes = new();

        public readonly NetEntity? SlotOwner;

        public readonly bool AdminRequest;

        public RequestServerRadialsEvent(NetEntity entityUid, IEnumerable<Type> radialTypes, NetEntity? slotOwner = null, bool adminRequest = false)
        {
            EntityUid = entityUid;
            SlotOwner = slotOwner;
            AdminRequest = adminRequest;

            foreach (var type in radialTypes)
            {
                //DebugTools.Assert(typeof(Radial).IsAssignableFrom(type));
                RadialTypes.Add(type.Name);
            }
        }
    }

    [Serializable, NetSerializable]
    public sealed class RadialsResponseEvent : EntityEventArgs
    {
        public readonly List<Radial>? Radials;
        public readonly NetEntity Entity;

        public RadialsResponseEvent(NetEntity entity, SortedSet<Radial>? radials)
        {
            Entity = entity;

            if (radials == null)
                return;

            Radials = new(radials);
        }
    }

    [Serializable, NetSerializable]
    public sealed class ExecuteRadialEvent : EntityEventArgs
    {
        public readonly NetEntity Target;
        public readonly Radial RequestedRadial;

        public ExecuteRadialEvent(NetEntity target, Radial requestedRadial)
        {
            Target = target;
            RequestedRadial = requestedRadial;
        }
    }

    public sealed class GetRadialsEvent<TValue> : EntityEventArgs where TValue : Radial
    {

        public readonly SortedSet<TValue> Radials = new();

        public readonly bool CanAccess = false;

        public readonly EntityUid Target;

        public readonly EntityUid User;

        public readonly bool CanInteract;

        public readonly HandsComponent? Hands;

        public readonly EntityUid? Using;

        public GetRadialsEvent(EntityUid user, EntityUid target, EntityUid? @using, HandsComponent? hands, bool canInteract, bool canAccess)
        {
            User = user;
            Target = target;
            Using = @using;
            Hands = hands;
            CanAccess = canAccess;
            CanInteract = canInteract;
        }
    }
