using Content.Server.Preferences.Managers;
using Content.Server.Revolutionary.Components;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Mindshield.Components;
using Content.Shared.Preferences;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Verbs;
using Content.Shared.Zombies;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.GameTicking.Rules;

public sealed partial class RevolutionaryRuleSystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;

    private static readonly ProtoId<AntagPrototype> HeadRevolutionaryAntag = "HeadRev";
    private static readonly ProtoId<AntagPrototype> RevolutionaryAntag = "Rev";

    private void InitializeWillingConvertExamine()
    {
        SubscribeLocalEvent<ActorComponent, GetVerbsEvent<ExamineVerb>>(OnGetWillingConvertExamineVerbs);
    }

    private void OnGetWillingConvertExamineVerbs(Entity<ActorComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanAccess ||
            !IsWillingRevolutionaryConvertTarget(ent, args.User))
        {
            return;
        }

        var user = args.User;
        var target = ent.Owner;

        args.Verbs.Add(new ExamineVerb
        {
            Act = () =>
            {
                var markup = FormattedMessage.FromMarkupOrThrow(Loc.GetString("rev-examine-willing-convert"));
                _examine.SendExamineTooltip(user, target, markup, false, false);
            },
            Text = Loc.GetString("rev-examine-willing-convert-verb"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
        });
    }

    private bool IsWillingRevolutionaryConvertTarget(Entity<ActorComponent> ent, EntityUid examiner)
    {
        if (!_examine.IsInDetailsRange(examiner, ent.Owner))
            return false;

        if (!HasComp<RevolutionaryComponent>(examiner) &&
            !HasComp<HeadRevolutionaryComponent>(examiner))
        {
            return false;
        }

        if (!HasComp<HumanoidAppearanceComponent>(ent.Owner) ||
            HasComp<RevolutionaryComponent>(ent.Owner) ||
            HasComp<HeadRevolutionaryComponent>(ent.Owner) ||
            HasComp<MindShieldComponent>(ent.Owner) ||
            HasComp<CommandStaffComponent>(ent.Owner) ||
            HasComp<ZombieComponent>(ent.Owner) ||
            !_mobState.IsAlive(ent.Owner))
        {
            return false;
        }

        if (!_preferences.TryGetCachedPreferences(ent.Comp.PlayerSession.UserId, out var preferences) ||
            preferences.SelectedCharacter is not HumanoidCharacterProfile profile)
        {
            return false;
        }

        return profile.AntagPreferences.Contains(RevolutionaryAntag) ||
               profile.AntagPreferences.Contains(HeadRevolutionaryAntag);
    }
}
