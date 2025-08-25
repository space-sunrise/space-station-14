using Robust.Shared.Audio;

using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.SyndicateTeleporter;

[RegisterComponent]
public sealed partial class SyndicateTeleporterComponent : Component
{
    /// <summary>
    /// adds a random value to which you teleport, which is added to the guaranteed teleport value. from 0 to the set number. set 0 if you don't need randomness when teleporting
    /// </summary>
    [DataField]
    public int RandomDistanceValue = 4;

    /// <summary>
    /// this is the guaranteed number of tiles that you teleport to.
    /// </summary>
    [DataField]
    public float TeleportationValue = 4f;

    /// <summary>
    /// how many attempts do you have to teleport into the wall without a fatal outcome
    /// </summary>
    [DataField]
    public int SaveAttempts = 1;

    /// <summary>
    /// the distance to which you will be teleported when you teleport into a wall
    /// </summary>
    [DataField]
    public int SaveDistance = 3;

    [DataField("alarm"), AutoNetworkedField]
    public SoundSpecifier? AlarmSound = new SoundPathSpecifier("/Audio/Effects/beeps.ogg");

    /// <summary>
    /// the number of seconds the player stays in the wall. (just so that he would realize that he almost died)
    /// </summary>
    [DataField]
    public float CorrectTime = 0.5f; //how long will it actually stay in the wall in seconds

    [DataField]
    public float Timer = 0; // it is necessary to reset the timer.

    /// <summary>
    /// the technical part that shouldn't really interest you
    /// </summary>
    [DataField]
    public bool InWall = false; // it is necessary to determine whether the player is in the wall and whether to start an emergency teleport
    public EntityUid UserComp; // a way to transfer the User id from UseInHand to use it elsewhere

    /// <summary>
    /// Звук телепортации
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? TeleportSound = new SoundCollectionSpecifier("BrokenDevice");

}
