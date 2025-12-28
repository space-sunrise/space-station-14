

using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared._Sunrise.HardsuitInjection.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.HardsuitInjection.EntitySystems;

public sealed partial class InjectSystem
{
    private void InitializeDoAfterEvents()
    {
        SubscribeLocalEvent<InjectComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbs);
        SubscribeLocalEvent<InjectComponent, ToggleSlotDoAfterEvent>(OnDoAfterComplete);
    }

    private void OnGetVerbs(EntityUid uid, InjectComponent component, GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.Container == null) return;

        var text = component.VerbText ?? (component.ToggleInjectionActionEntity == null ? null : Name(component.ToggleInjectionActionEntity.Value));

        if (text == null) return;
        if (!_inventorySystem.InSlotWithFlags(uid, component.RequiredFlags)) return;

        var wearer = Transform(uid).ParentUid;

        if (args.User != wearer && component.StripDelay == null) return;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Text = Loc.GetString(text),
        };

        if (args.User == wearer)
        {
            verb.EventTarget = uid;
            verb.ExecutionEventArgs = new ToggleECEvent() { Performer = args.User };
        }
        else
        {
            verb.Act = () => StartDoAfter(args.User, uid, Transform(uid).ParentUid, component);
        }

        args.Verbs.Add(verb);
    }

    private void StartDoAfter(EntityUid user, EntityUid item, EntityUid wearer, InjectComponent component)
    {
        if (component.StripDelay == null) return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, null, component.StripDelay.Value);

        var args = new DoAfterArgs(EntityManager, user, time, new ToggleSlotDoAfterEvent(), item, wearer, item)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args)) return;
        if (component.Locked)
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} is trying to open ES of {_entManager.ToPrettyString(wearer):wearer}");
        else
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} is trying to close ES of {_entManager.ToPrettyString(wearer):wearer}");

        if (stealth) return;

        var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", item));
        _popupSystem.PopupEntity(popup, wearer, wearer, PopupType.Large);
    }

    private void OnDoAfterComplete(EntityUid uid, InjectComponent component, ToggleSlotDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled) return;
        if (_netManager.IsClient) return;

        ToggleEC(uid, args.User);
        args.Handled = true;
    }
}
