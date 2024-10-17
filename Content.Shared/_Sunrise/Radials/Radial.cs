using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Radials;

    [Serializable, NetSerializable, Virtual]
    public class Radial : IComparable
    {

        /// <summary>
        ///     Determines the priority of this type of verb when displaying in the verb-menu. See <see
        ///     cref="CompareTo"/>.
        /// </summary>
        public virtual int TypePriority => 0;

        [NonSerialized]
        public Action? Act;

        [NonSerialized]
        public object? ExecutionEventArgs;

        [NonSerialized]
        public EntityUid EventTarget = EntityUid.Invalid;

        [NonSerialized]
        public bool ClientExclusive;

        public LogImpact Impact = LogImpact.Low;

        public string Text = string.Empty;

        public string? Icon;

        public bool Disabled;

        public string? Message;

        public int Priority;

        public NetEntity? IconEntity;

        public bool? CloseMenu;

        public virtual bool CloseMenuDefault => true;

        public bool ConfirmationPopup = false;

        public bool? DoContactInteraction;

        public virtual bool DefaultDoContactInteraction => false;

        public int CompareTo(object? obj)
        {
            if (obj is not Radial radial)
                return -1;

            // Sort first by type-priority
            if (TypePriority != radial.TypePriority)
                return radial.TypePriority - TypePriority;

            // Then by verb-priority
            if (Priority != radial.Priority)
                return radial.Priority - Priority;

            // Then try use alphabetical verb text.
            if (Text != radial.Text)
            {
                return string.Compare(Text, radial.Text, StringComparison.CurrentCulture);
            }

            if (IconEntity != radial.IconEntity)
            {
                if (IconEntity == null)
                    return -1;

                if (radial.IconEntity == null)
                    return 1;

                return IconEntity.Value.CompareTo(radial.IconEntity.Value);
            }

            // Finally, compare icon texture paths. Note that this matters for verbs that don't have any text (e.g., the rotate-verbs)
            return string.Compare(Icon?.ToString(), radial.Icon?.ToString(), StringComparison.CurrentCulture);
        }

        // I hate this. Please somebody allow generics to be networked.
        /// <summary>
        ///     Collection of all verb types,
        /// </summary>
        /// <remarks>
        ///     Useful when iterating over verb types, though maybe this should be obtained and stored via reflection or
        ///     something (list of all classes that inherit from Verb). Currently used for networking (apparently Type
        ///     is not serializable?), and resolving console commands.
        /// </remarks>
        public static HashSet<Type> RadialTypes = new()
        {
            typeof(Radial),
            typeof(InteractionRadial),
        };
    }

    /// <summary>
    ///    Primary interaction verbs. This includes both use-in-hand and interacting with external entities.
    /// </summary>
    /// <remarks>
    ///    These verbs those that involve using the hands or the currently held item on some entity. These verbs usually
    ///    correspond to interactions that can be triggered by left-clicking or using 'Z', and often depend on the
    ///    currently held item. These verbs are collectively shown first in the context menu.
    /// </remarks>
    [Serializable, NetSerializable]
    public sealed class InteractionRadial : Radial
    {
        public override int TypePriority => 1;
        public override bool DefaultDoContactInteraction => true;

        public InteractionRadial() : base()
        {
        }
    }
