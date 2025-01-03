using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._Sunrise.BloodCult.Actions;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

public sealed class CultSpellProviderSystem: EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, CultMagicBloodCallEvent>(OnCultMagicBlood);
        SubscribeLocalEvent<CultSpellProviderComponent, CultSpellProviderSelectedBuiMessage>(OnCultMagicBloodSelected);
        SubscribeLocalEvent<CultSpellProviderComponent, GetVerbsEvent<ActivationVerb>>(OnDaggerActivationVerb);
        SubscribeLocalEvent<CultSpellProviderComponent, ActivateInWorldEvent>(OnDaggerActivate);
    }

    private void OnDaggerActivate(EntityUid uid, CultSpellProviderComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<BloodCultistComponent>(args.User, out _) || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ui.OpenUi(uid, CultSpellProviderUiKey.Key, actor.PlayerSession);
    }

    private void OnDaggerActivationVerb(EntityUid uid, CultSpellProviderComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<BloodCultistComponent>(args.User, out _) || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        args.Verbs.Add(new ActivationVerb()
        {
            Text = "Вырезать заклинание",
            Act = () =>
            {
                _ui.OpenUi(uid, CultSpellProviderUiKey.Key, actor.PlayerSession);
            }
        });
    }

    private void OnCultMagicBloodSelected(EntityUid uid, CultSpellProviderComponent component, CultSpellProviderSelectedBuiMessage args)
    {
        if (!TryComp<BloodCultistComponent>(args.Actor, out var comp) ||
            !TryComp<ActionsComponent>(args.Actor, out var actionsComponent))
            return;

        var cultistsActions = 0;

        var action = BloodCultistComponent.CultistActions.FirstOrDefault(x => x.Equals(args.ActionType));

        var duplicated = false;
        foreach (var userAction in actionsComponent.Actions)
        {
            var entityPrototypeId = MetaData(userAction).EntityPrototype?.ID;
            if (entityPrototypeId != null && BloodCultistComponent.CultistActions.Contains(entityPrototypeId))
                cultistsActions++;

            if (entityPrototypeId == action)
                duplicated = true;
        }

        if (action == null)
            return;

        if (duplicated)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-duplicated-empowers"), uid);
            return;
        }

        var maxAllowedActions = 1;
        var timeToGetSpell = 10;
        var bloodTake = 20;

        var xform = Transform(uid);

        if (CheckNearbyEmpowerRune(xform.Coordinates))
        {
            maxAllowedActions = 4;
            timeToGetSpell = 4;
            bloodTake = 8;
        }

        if (cultistsActions >= maxAllowedActions)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-too-much-empowers"), uid);
            return;
        }

        var ev = new CultMagicBloodCallEvent
        {
            ActionId = action,
            BloodTake = bloodTake
        };

        var argsDoAfterEvent = new DoAfterArgs(_entityManager, args.Actor, timeToGetSpell, ev, args.Actor)
        {
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfterSystem.TryStartDoAfter(argsDoAfterEvent);
    }

    private void OnCultMagicBlood(EntityUid uid, BloodCultistComponent comp, CultMagicBloodCallEvent args)
    {
        if (args.Cancelled)
            return;

        var howMuchBloodTake = -args.BloodTake;
        var action = args.ActionId;
        var user = args.User;

        if (HasComp<CultBuffComponent>(user))
            howMuchBloodTake /= 2;

        if (!TryComp<BloodstreamComponent>(user, out var bloodstreamComponent))
            return;

        _bloodstreamSystem.TryModifyBloodLevel(user, howMuchBloodTake, bloodstreamComponent);
        _audio.PlayPvs("/Audio/_Sunrise/BloodCult/blood.ogg", user, AudioParams.Default.WithMaxDistance(2f));

        EntityUid? actionId = null;
        _actionsSystem.AddAction(user, ref actionId, action);
    }

    private bool CheckNearbyEmpowerRune(EntityCoordinates coordinates)
    {
        var radius = 1.0f;

        foreach (var lookupUid in _lookup.GetEntitiesInRange(coordinates, radius))
        {
            if (HasComp<CultEmpowerComponent>(lookupUid))
                return true;
        }

        return false;
    }
}
