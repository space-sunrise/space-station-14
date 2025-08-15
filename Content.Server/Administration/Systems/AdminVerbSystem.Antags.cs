using Content.Server._Sunrise.AssaultOps;
using Content.Server._Sunrise.BloodCult.GameRule;
using Content.Server._Sunrise.FleshCult.GameRule;
using Content.Server.Administration.Commands;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Server.Clothing.Systems;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly OutfitSystem _outfit = default!;

    private static readonly EntProtoId DefaultTraitorRule = "Traitor";
    private static readonly EntProtoId DefaultInitialInfectedRule = "Zombie";
    private static readonly EntProtoId DefaultNukeOpRule = "LoneOpsSpawn";
    private static readonly EntProtoId DefaultRevsRule = "Revolutionary";
    private static readonly EntProtoId DefaultThiefRule = "Thief";
    private static readonly EntProtoId DefaultChangelingRule = "Changeling";
    private static readonly EntProtoId ParadoxCloneRuleId = "ParadoxCloneSpawn";
    private static readonly ProtoId<StartingGearPrototype> PirateGearId = "PirateGear";
    private static readonly EntProtoId DefaultAssaultOpsRule = "AssaultOps";
    private static readonly EntProtoId DefaultFleshCultRule = "FleshCult";
    private static readonly EntProtoId DefaultVampireRule = "Vampire";

    // All antag verbs have names so invokeverb works.
    private void AddAntagVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (!HasComp<MindContainerComponent>(args.Target) || !TryComp<ActorComponent>(args.Target, out var targetActor))
            return;

        var targetPlayer = targetActor.PlayerSession;

        var traitorName = Loc.GetString("admin-verb-text-make-traitor");
        Verb traitor = new()
        {
            Text = traitorName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "Syndicate"),
            Act = () =>
            {
                _antag.ForceMakeAntag<TraitorRuleComponent>(targetPlayer, DefaultTraitorRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", traitorName, Loc.GetString("admin-verb-make-traitor")),
        };
        args.Verbs.Add(traitor);

        var initialInfectedName = Loc.GetString("admin-verb-text-make-initial-infected");
        Verb initialInfected = new()
        {
            Text = initialInfectedName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "InitialInfected"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ZombieRuleComponent>(targetPlayer, DefaultInitialInfectedRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", initialInfectedName, Loc.GetString("admin-verb-make-initial-infected")),
        };
        args.Verbs.Add(initialInfected);

        var zombieName = Loc.GetString("admin-verb-text-make-zombie");
        Verb zombie = new()
        {
            Text = zombieName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "Zombie"),
            Act = () =>
            {
                _zombie.ZombifyEntity(args.Target);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", zombieName, Loc.GetString("admin-verb-make-zombie")),
        };
        args.Verbs.Add(zombie);

        var nukeOpName = Loc.GetString("admin-verb-text-make-nuclear-operative");
        Verb nukeOp = new()
        {
            Text = nukeOpName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Clothing/Head/Hardsuits/syndicate.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<NukeopsRuleComponent>(targetPlayer, DefaultNukeOpRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", nukeOpName, Loc.GetString("admin-verb-make-nuclear-operative")),
        };
        args.Verbs.Add(nukeOp);

        var pirateName = Loc.GetString("admin-verb-text-make-pirate");
        Verb pirate = new()
        {
            Text = pirateName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Clothing/Head/Hats/pirate.rsi"), "icon"),
            Act = () =>
            {
                // pirates just get an outfit because they don't really have logic associated with them
                _outfit.SetOutfit(args.Target, PirateGearId);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", pirateName, Loc.GetString("admin-verb-make-pirate")),
        };
        args.Verbs.Add(pirate);

        var headRevName = Loc.GetString("admin-verb-text-make-head-rev");
        Verb headRev = new()
        {
            Text = headRevName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "HeadRevolutionary"),
            Act = () =>
            {
                _antag.ForceMakeAntag<RevolutionaryRuleComponent>(targetPlayer, DefaultRevsRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", headRevName, Loc.GetString("admin-verb-make-head-rev")),
        };
        args.Verbs.Add(headRev);

        var thiefName = Loc.GetString("admin-verb-text-make-thief");
        Verb thief = new()
        {
            Text = thiefName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Hands/Gloves/Color/black.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ThiefRuleComponent>(targetPlayer, DefaultThiefRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", thiefName, Loc.GetString("admin-verb-make-thief")),
        };
        args.Verbs.Add(thief);

        var changelingName = Loc.GetString("admin-verb-text-make-changeling");
        Verb changeling = new()
        {
            Text = changelingName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Weapons/Melee/armblade.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ChangelingRuleComponent>(targetPlayer, DefaultChangelingRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", changelingName, Loc.GetString("admin-verb-make-changeling")),
        };
        args.Verbs.Add(changeling);

        var paradoxCloneName = Loc.GetString("admin-verb-text-make-paradox-clone");
        Verb paradox = new()
        {
            Text = paradoxCloneName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "ParadoxClone"),
            Act = () =>
            {
                var ruleEnt = _gameTicker.AddGameRule(ParadoxCloneRuleId);

                if (!TryComp<ParadoxCloneRuleComponent>(ruleEnt, out var paradoxCloneRuleComp))
                    return;

                paradoxCloneRuleComp.OriginalBody = args.Target; // override the target player

                _gameTicker.StartGameRule(ruleEnt);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", paradoxCloneName, Loc.GetString("admin-verb-make-paradox-clone")),
        };

        if (HasComp<HumanoidAppearanceComponent>(args.Target)) // only humanoids can be cloned
            args.Verbs.Add(paradox);

        // Sunrise-Start

        Verb vampire = new()
        {
            Text = Loc.GetString("admin-verb-text-make-vampire"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_vampire.rsi"),
                "unholystrength"),
            Act = () =>
            {
                _antag.ForceMakeAntag<VampireRuleComponent>(targetPlayer, DefaultVampireRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-vampire"),
        };
        args.Verbs.Add(vampire);

        Verb assaultOperative = new()
        {
            Text = Loc.GetString("admin-verb-text-make-assault-operative"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Wallmounts/posters.rsi"),
                "poster46_contraband"),
            Act = () =>
            {
                _antag.ForceMakeAntag<AssaultOpsRuleComponent>(targetPlayer, DefaultAssaultOpsRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-assault-operative"),
        };
        args.Verbs.Add(assaultOperative);

        Verb fleshCultist = new()
        {
            Text = "Make Flesh Cultist",
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Texture(
                new ResPath("_Sunrise/FleshCult/Interface/Actions/fleshCultistFleshHeart.png")),
            Act = () =>
            {
                _antag.ForceMakeAntag<FleshCultRuleComponent>(targetPlayer, DefaultFleshCultRule);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-flesh-cultist"),
        };
        args.Verbs.Add(fleshCultist);

        Verb bloodCultist = new()
        {
            Text = Loc.GetString("admin-verb-text-make-cultist"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Weapons/Melee/cult_dagger.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<BloodCultRuleComponent>(targetPlayer, "BloodCult");
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-make-cultist"),
        };
        args.Verbs.Add(bloodCultist);
        // Sunrise-End
    }
}
