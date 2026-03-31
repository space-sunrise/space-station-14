using Content.Server.Administration.Managers;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Ghost;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.StationEvents.Components;
using Content.Server._Sunrise.Speech.Components;
using Content.Server.Speech.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared._Sunrise;
using Content.Shared._Sunrise.TTS;
using Content.Shared._Sunrise.CollectiveMind;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Polymorph;
using Content.Shared.Weapons.Melee;
using Content.Shared.Zombies;
using Content.Shared.Prying.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Temperature.Components;

namespace Content.Server.Zombies;

/// <summary>
///     Handles zombie propagation and inherent zombie traits
/// </summary>
/// <remarks>
///     Don't Shitcode Open Inside
/// </remarks>
public sealed partial class ZombieSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IBanManager _ban = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private static readonly ProtoId<TagPrototype> InvalidForGlobalSpawnSpellTag = "InvalidForGlobalSpawnSpell";
    private static readonly ProtoId<TagPrototype> CannotSuicideTag = "CannotSuicide";
    private static readonly ProtoId<NpcFactionPrototype> ZombieFaction = "Zombie";
    private const string FurryZombieVulpkaninSpecies = "Vulpkanin";
    private const string FurryZombieTajaranSpecies = "Tajaran";
    private static readonly ProtoId<PolymorphPrototype> FurryZombieVulpkaninPolymorph = "ZombieVirusPermanentlyVulpkanin";
    private static readonly ProtoId<PolymorphPrototype> FurryZombieTajaranPolymorph = "ZombieVirusPermanentlyTajaran";
    private static readonly ProtoId<SpeechVerbPrototype> FurryZombieVulpkaninSpeechVerb = "Vulpkanin";
    private static readonly ProtoId<SpeechVerbPrototype> FurryZombieTajaranSpeechVerb = "Felinid";
    private static readonly ProtoId<SpeechSoundsPrototype> FurryZombieVulpkaninSpeechSounds = "Vulpkanin";
    private static readonly ProtoId<SpeechSoundsPrototype> FurryZombieTajaranSpeechSounds = "Alto";
    private static readonly ProtoId<EmotePrototype>[] FurryZombieVulpkaninAllowedEmotes = ["Bark", "Snarl", "Whine", "Howl", "Growl"];
    private static readonly ProtoId<EmotePrototype>[] FurryZombieTajaranAllowedEmotes = ["Mew", "Meow", "Hisses", "Purr", "Growl"];
    private static readonly Dictionary<Sex, ProtoId<EmoteSoundsPrototype>> FurryZombieVulpkaninVocal = new()
    {
        { Sex.Male, "MaleVulpkanin" },
        { Sex.Female, "FemaleVulpkanin" },
        { Sex.Unsexed, "MaleVulpkanin" },
    };
    private static readonly Dictionary<Sex, ProtoId<EmoteSoundsPrototype>> FurryZombieTajaranVocal = new()
    {
        { Sex.Male, "MaleTajaran" },
        { Sex.Female, "FemaleTajaran" },
        { Sex.Unsexed, "MaleTajaran" },
    };
    private static readonly string MindRoleZombie = "MindRoleZombie";
    private static readonly List<ProtoId<AntagPrototype>> BannableZombiePrototypes = ["Zombie"];

    /// <summary>
    /// Handles an entity turning into a zombie when they die or go into crit
    /// </summary>
    private void OnDamageChanged(EntityUid uid, ZombifyOnDeathComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            ZombifyEntity(uid, args.Component);
        }
    }

    /// <summary>
    ///     This is the general purpose function to call if you want to zombify an entity.
    ///     It handles both humanoid and nonhumanoid transformation and everything should be called through it.
    /// </summary>
    /// <param name="target">the entity being zombified</param>
    /// <param name="mobState"></param>
    /// <remarks>
    ///     ALRIGHT BIG BOYS, GIRLS AND ANYONE ELSE. YOU'VE COME TO THE LAYER OF THE BEAST. THIS IS YOUR WARNING.
    ///     This function is the god function for zombie stuff, and it is cursed. I have
    ///     attempted to label everything thouroughly for your sanity. I have attempted to
    ///     rewrite this, but this is how it shall lie eternal. Turn back now.
    ///     -emo
    /// </remarks>
    public void ZombifyEntity(EntityUid target, MobStateComponent? mobState = null)
    {
        //Don't zombfiy zombies
        if (HasComp<ZombieComponent>(target) || HasComp<ZombieImmuneComponent>(target))
            return;

        if (!Resolve(target, ref mobState, logMissing: false))
            return;

        // Detach role-banned players before zombification
        if (TryComp<ActorComponent>(target, out var actor) && _ban.IsRoleBanned(actor.PlayerSession, BannableZombiePrototypes))
        {
            var sess = actor.PlayerSession;
            var message = Loc.GetString("zombie-roleban-ghosted");

            if (_mind.TryGetMind(sess, out var playerMindEnt, out var playerMind))
            {
                // Detach
                _ghost.SpawnGhost((playerMindEnt, playerMind), target);

                // Notify
                _chatMan.DispatchServerMessage(sess, message);
            }
            else
                Log.Error($"Mind for session '{sess}' could not be found");
        }

        var originalTarget = target;
        HumanoidAppearanceComponent? originalHumanoidAppearance = null;
        string? furryZombieSpecies = null;

        if (TryComp<HumanoidAppearanceComponent>(target, out var sourceHumanoidAppearance))
        {
            originalHumanoidAppearance = sourceHumanoidAppearance;
            furryZombieSpecies = PickFurryZombieSpecies(originalHumanoidAppearance.Species);

            if (_polymorph.PolymorphEntity(target, GetFurryZombiePolymorph(furryZombieSpecies)) is not { } polymorphedTarget)
            {
                Log.Error($"Failed to polymorph {ToPrettyString(target)} into furry zombie species {furryZombieSpecies}.");
                return;
            }

            target = polymorphedTarget;

            RemCompDeferred<PendingZombieComponent>(originalTarget);
            RemCompDeferred<ZombifyOnDeathComponent>(originalTarget);
        }

        //you're a real zombie now, son.
        var zombiecomp = AddComp<ZombieComponent>(target);

        //we need to basically remove all of these because zombies shouldn't
        //get diseases, breath, be thirst, be hungry, die in space, get double sentience, have offspring or be paraplegic.
        RemComp<RespiratorComponent>(target);
        RemComp<BarotraumaComponent>(target);
        RemComp<HungerComponent>(target);
        RemComp<ThirstComponent>(target);
        RemComp<ReproductiveComponent>(target);
        RemComp<ReproductivePartnerComponent>(target);
        RemComp<LegsParalyzedComponent>(target);
        RemComp<ComplexInteractionComponent>(target);
        RemComp<SentienceTargetComponent>(target);

        // Sunrise edit start - furry virus uses OwO accent instead of zombie speech replacement
        RemComp<ReplacementAccentComponent>(target);
        EnsureComp<OwOAccentComponent>(target);
        // Sunrise edit end

        //This is needed for stupid entities that fuck up combat mode component
        //in an attempt to make an entity not attack. This is the easiest way to do it.
        var combat = EnsureComp<CombatModeComponent>(target);
        RemComp<PacifiedComponent>(target);
        _combat.SetCanDisarm(target, false, combat);

        //This is the actual damage of the zombie. We assign the visual appearance
        //and range here because of stuff we'll find out later
        var melee = EnsureComp<MeleeWeaponComponent>(target);
        melee.Animation = zombiecomp.AttackAnimation;
        melee.WideAnimation = zombiecomp.AttackAnimation;
        melee.AltDisarm = false;
        melee.Range = 1.2f;
        melee.Angle = 0.0f;
        melee.HitSound = zombiecomp.BiteSound;

        DirtyFields(target, melee, null, fields:
        [
            nameof(MeleeWeaponComponent.Animation),
            nameof(MeleeWeaponComponent.WideAnimation),
            nameof(MeleeWeaponComponent.AltDisarm),
            nameof(MeleeWeaponComponent.Range),
            nameof(MeleeWeaponComponent.Angle),
            nameof(MeleeWeaponComponent.HitSound),
        ]);

        // Sunrise-Start
        RemComp<CuffableComponent>(target);

        var collectiveMindComponent = EnsureComp<CollectiveMindComponent>(target);
        foreach (var collectiveMind in collectiveMindComponent.Minds.ToArray())
        {
            collectiveMindComponent.Minds.Remove(collectiveMind);
        }

        if (!collectiveMindComponent.Minds.Contains("Zombie"))
            collectiveMindComponent.Minds.Add("Zombie");
        // Sunrise-End

        //We have specific stuff for humanoid zombies because they matter more
        if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp)) //huapcomp
        {
            if (furryZombieSpecies != null && originalHumanoidAppearance != null)
                ApplyFurryZombieAppearance(originalTarget, target, zombiecomp, furryZombieSpecies, huApComp, originalHumanoidAppearance);

            //This is done here because non-humanoids shouldn't get baller damage
            melee.Damage = zombiecomp.DamageOnBite;

            // humanoid zombies get to pry open doors and shit
            var pryComp = EnsureComp<PryingComponent>(target);
            pryComp.SpeedModifier = 0.75f; // Sunrise-Edit
            pryComp.PryPowered = true;
            pryComp.Force = true;

            Dirty(target, pryComp);
        }

        Dirty(target, melee);

        //The zombie gets the assigned damage weaknesses and strengths
        _damageable.SetDamageModifierSetId(target, "Zombie");

        //This makes it so the zombie doesn't take bloodloss damage.
        //NOTE: they are supposed to bleed, just not take damage
        _bloodstream.SetBloodLossThreshold(target, 0f);
        //Give them zombie blood
        _bloodstream.ChangeBloodReagents(target, zombiecomp.NewBloodReagents);

        //This is specifically here to combat insuls, because frying zombies on grilles is funny as shit.
        _inventory.TryUnequip(target, "gloves", true, true);
        //Should prevent instances of zombies using comms for information they shouldnt be able to have.
        _inventory.TryUnequip(target, "ears", true, true);

        //popup
        _popup.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), target, PopupType.LargeCaution);

        //Make it sentient if it's an animal or something
        _mind.MakeSentient(target);

        //Make the zombie not die in the cold. Good for space zombies
        if (TryComp<TemperatureDamageComponent>(target, out var tempComp))
            tempComp.ColdDamage.ClampMax(0);

        //Heals the zombie from all the damage it took while human
        _damageable.ClearAllDamage(target);
        _mobState.ChangeMobState(target, MobState.Alive);

        _faction.ClearFactions(target, dirty: false);
        _faction.AddFaction(target, ZombieFaction);

        //gives it the funny "Zombie ___" name.
        _nameMod.RefreshNameModifiers(target);

        _identity.QueueIdentityUpdate(target);

        var htn = EnsureComp<HTNComponent>(target);
        htn.RootTask = new HTNCompoundTask() { Task = "SimpleHostileCompound" };
        htn.Blackboard.SetValue(NPCBlackboard.Owner, target);
        _npc.SleepNPC(target, htn);

        //He's gotta have a mind
        var hasMind = _mind.TryGetMind(target, out var mindId, out var mind);
        if (hasMind && mind != null && _player.TryGetSessionById(mind.UserId, out var session))
        {
            //Zombie role for player manifest
            _role.MindAddRole(mindId, MindRoleZombie, mind: null, silent: true);

            //Greeting message for new bebe zombers
            _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));

            // Notificate player about new role assignment
            _audio.PlayGlobal(zombiecomp.GreetSoundNotification, session);
        }
        else
        {
            _npc.WakeNPC(target, htn);
        }

        if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !hasMind) //this specific component gives build test trouble so pop off, ig
        {
            //yet more hardcoding. Visit zombie.ftl for more information.
            var ghostRole = EnsureComp<GhostRoleComponent>(target);
            EnsureComp<GhostTakeoverAvailableComponent>(target);
            ghostRole.RoleName = Loc.GetString("zombie-generic");
            ghostRole.RoleDescription = Loc.GetString("zombie-role-desc");
            ghostRole.RoleRules = Loc.GetString("zombie-role-rules");
            ghostRole.MindRoles.Add(MindRoleZombie);
        }

        if (TryComp<HandsComponent>(target, out var handsComp))
        {
            _hands.RemoveHands(target);
            RemComp(target, handsComp);
        }

        // Sloth: What the fuck?
        // How long until compregistry lmao.
        RemComp<PullerComponent>(target);

        // No longer waiting to become a zombie:
        // Requires deferral because this is (probably) the event which called ZombifyEntity in the first place.
        RemCompDeferred<PendingZombieComponent>(target);

        //zombie gamemode stuff
        var ev = new EntityZombifiedEvent(target);
        RaiseLocalEvent(target, ref ev, true);
        //zombies get slowdown once they convert
        _movementSpeedModifier.RefreshMovementSpeedModifiers(target);

        //Need to prevent them from getting an item, they have no hands.
        // Also prevents them from becoming a Survivor. They're undead.
        _tag.AddTag(target, InvalidForGlobalSpawnSpellTag);
        _tag.AddTag(target, CannotSuicideTag);
    }

    private string PickFurryZombieSpecies(ProtoId<SpeciesPrototype> originalSpecies)
    {
        if (originalSpecies == FurryZombieVulpkaninSpecies)
            return FurryZombieTajaranSpecies;

        if (originalSpecies == FurryZombieTajaranSpecies)
            return FurryZombieVulpkaninSpecies;

        return _random.NextDouble() < 0.5
            ? FurryZombieVulpkaninSpecies
            : FurryZombieTajaranSpecies;
    }

    private string GetFurryZombieBodyType(string infectedSpecies, ProtoId<BodyTypePrototype> originalBodyType)
    {
        var originalBodyTypeId = originalBodyType.ToString();

        if (infectedSpecies == FurryZombieTajaranSpecies)
        {
            return originalBodyTypeId.Contains("Curved", StringComparison.OrdinalIgnoreCase)
                ? "TajaranCurved"
                : "TajaranNormal";
        }

        var isStraight = originalBodyTypeId.Contains("Straight", StringComparison.OrdinalIgnoreCase);
        var isBigMuzzle = originalBodyTypeId.Contains("Big", StringComparison.OrdinalIgnoreCase);

        return (isStraight, isBigMuzzle) switch
        {
            (true, true) => "VulpkaninStraightBigMuzzle",
            (true, false) => "VulpkaninStraightSmallMuzzle",
            (false, true) => "VulpkaninCurvedBigMuzzle",
            _ => "VulpkaninNormal",
        };
    }

    private ProtoId<PolymorphPrototype> GetFurryZombiePolymorph(string infectedSpecies)
    {
        return infectedSpecies == FurryZombieTajaranSpecies
            ? FurryZombieTajaranPolymorph
            : FurryZombieVulpkaninPolymorph;
    }

    private void ApplyFurryZombieAppearance(EntityUid originalTarget, EntityUid target, ZombieComponent zombiecomp,
        string infectedSpecies, HumanoidAppearanceComponent humanoid, HumanoidAppearanceComponent originalHumanoid)
    {
        _humanoidAppearance.SetSex(target, originalHumanoid.Sex, false, humanoid);
        _humanoidAppearance.SetGender((target, humanoid), originalHumanoid.Gender);
        humanoid.Age = originalHumanoid.Age;
        humanoid.Width = originalHumanoid.Width;
        humanoid.Height = originalHumanoid.Height;

        _humanoidAppearance.SetBodyType(target,
            GetFurryZombieBodyType(infectedSpecies, originalHumanoid.BodyType),
            false,
            humanoid);
        _humanoidAppearance.SetSkinColor(target, zombiecomp.SkinColor, verify: false, humanoid: humanoid);
        humanoid.EyeColor = zombiecomp.EyeColor;

        EnsureFurryZombieMarkings(humanoid);
        ForceFurryZombieMarkingColors(humanoid);
        ApplyFurryZombieSpeech(originalTarget, target, zombiecomp, infectedSpecies, humanoid);
        ApplyFurryZombieAccent(target, infectedSpecies);

        Dirty(target, humanoid);
    }

    private void ApplyFurryZombieAccent(EntityUid target, string infectedSpecies)
    {
        RemComp<TajaranAccentComponent>(target);
        RemComp<VulpaAccentComponent>(target);

        if (infectedSpecies == FurryZombieTajaranSpecies)
            EnsureComp<TajaranAccentComponent>(target);
        else
            EnsureComp<VulpaAccentComponent>(target);
    }

    private static void EnsureFurryZombieMarkings(HumanoidAppearanceComponent humanoid)
    {
        humanoid.MarkingSet.EnsureSpecies(humanoid.Species, humanoid.SkinColor);
        humanoid.MarkingSet.EnsureSexes(humanoid.Sex);
        humanoid.MarkingSet.EnsureDefault(humanoid.SkinColor, humanoid.EyeColor);
    }

    private static void ForceFurryZombieMarkingColors(HumanoidAppearanceComponent humanoid)
    {
        foreach (var markings in humanoid.MarkingSet.Markings.Values)
        {
            foreach (var marking in markings)
            {
                for (var colorIndex = 0; colorIndex < marking.MarkingColors.Count; colorIndex++)
                {
                    var alpha = marking.MarkingColors[colorIndex].A;
                    marking.SetColor(colorIndex, humanoid.SkinColor.WithAlpha(alpha));
                }
            }
        }
    }

    private bool TryRestorePreZombifiedPolymorphState(EntityUid source, EntityUid target)
    {
        if (!TryComp<PolymorphedEntityComponent>(source, out var polymorphed)
            || polymorphed.Parent is not { } parent
            || Deleted(parent))
        {
            return false;
        }

        if (TryComp<HumanoidAppearanceComponent>(parent, out var parentHumanoid)
            && TryComp<HumanoidAppearanceComponent>(target, out var targetHumanoid))
        {
            _humanoidAppearance.CloneAppearance(parent, target, parentHumanoid, targetHumanoid);
        }

        SyncPreZombifiedAccentComponents(parent, target);

        if (TryComp<SpeechComponent>(parent, out var sourceSpeech))
        {
            var targetSpeech = EnsureComp<SpeechComponent>(target);
            targetSpeech.SpeechSounds = sourceSpeech.SpeechSounds;
            targetSpeech.SpeechVerb = sourceSpeech.SpeechVerb;
            targetSpeech.AllowedEmotes = new(sourceSpeech.AllowedEmotes);
            Dirty(target, targetSpeech);
        }

        if (TryComp<VocalComponent>(parent, out var sourceVocal))
        {
            var targetVocal = EnsureComp<VocalComponent>(target);
            targetVocal.Sounds = sourceVocal.Sounds == null
                ? null
                : new Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>(sourceVocal.Sounds);
            targetVocal.EmoteSounds = sourceVocal.EmoteSounds;
            Dirty(target, targetVocal);
        }

        if (TryComp<BloodstreamComponent>(parent, out var stream)
            && stream.BloodReferenceSolution is { } reagents)
        {
            _bloodstream.ChangeBloodReagents(target, reagents.Clone());
        }

        return true;
    }

    private void SyncPreZombifiedAccentComponents(EntityUid source, EntityUid target)
    {
        if (HasComp<OwOAccentComponent>(source))
            EnsureComp<OwOAccentComponent>(target);
        else
            RemComp<OwOAccentComponent>(target);

        if (HasComp<TajaranAccentComponent>(source))
            EnsureComp<TajaranAccentComponent>(target);
        else
            RemComp<TajaranAccentComponent>(target);

        if (HasComp<VulpaAccentComponent>(source))
            EnsureComp<VulpaAccentComponent>(target);
        else
            RemComp<VulpaAccentComponent>(target);
    }

    private void ApplyFurryZombieSpeech(EntityUid originalTarget, EntityUid target, ZombieComponent zombiecomp,
        string infectedSpecies, HumanoidAppearanceComponent humanoid)
    {
        if (TryComp<SpeechComponent>(target, out var speechComp))
        {
            speechComp.SpeechVerb = infectedSpecies == FurryZombieTajaranSpecies
                ? FurryZombieTajaranSpeechVerb
                : FurryZombieVulpkaninSpeechVerb;
            speechComp.SpeechSounds = infectedSpecies == FurryZombieTajaranSpecies
                ? FurryZombieTajaranSpeechSounds
                : FurryZombieVulpkaninSpeechSounds;

            var allowedEmotes = TryComp<SpeechComponent>(originalTarget, out var originalSpeechComp)
                ? new List<ProtoId<EmotePrototype>>(originalSpeechComp.AllowedEmotes)
                : [];
            var furryEmotes = infectedSpecies == FurryZombieTajaranSpecies
                ? FurryZombieTajaranAllowedEmotes
                : FurryZombieVulpkaninAllowedEmotes;

            foreach (var emote in furryEmotes)
            {
                if (!allowedEmotes.Contains(emote))
                    allowedEmotes.Add(emote);
            }

            speechComp.AllowedEmotes = allowedEmotes;
            Dirty(target, speechComp);
        }

        if (TryComp<VocalComponent>(target, out var vocalComp))
        {
            var vocal = infectedSpecies == FurryZombieTajaranSpecies
                ? FurryZombieTajaranVocal
                : FurryZombieVulpkaninVocal;

            vocalComp.Sounds = new Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>(vocal);
            if (!vocalComp.Sounds.TryGetValue(humanoid.Sex, out var emoteSoundsId))
                emoteSoundsId = vocalComp.Sounds[Sex.Unsexed];

            vocalComp.EmoteSounds = emoteSoundsId;
            Dirty(target, vocalComp);
        }

        var voiceId = GetFurryZombieVoice(zombiecomp, infectedSpecies, humanoid.Sex);
        humanoid.Voice = voiceId;

        if (TryComp<TTSComponent>(target, out var ttsComp))
        {
            ttsComp.VoicePrototypeId = voiceId;
            Dirty(target, ttsComp);
        }
    }

    private ProtoId<TTSVoicePrototype> GetFurryZombieVoice(ZombieComponent zombiecomp, string infectedSpecies, Sex sex)
    {
        var resolvedSex = sex == Sex.Female
            ? Sex.Female
            : Sex.Male;

        if (infectedSpecies == FurryZombieTajaranSpecies)
        {
            return resolvedSex == Sex.Female
                ? zombiecomp.TajaranFemaleVoice
                : zombiecomp.TajaranMaleVoice;
        }

        return resolvedSex == Sex.Female
            ? zombiecomp.VulpkaninFemaleVoice
            : zombiecomp.VulpkaninMaleVoice;
    }
}
