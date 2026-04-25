using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee;
using Content.Client.Weapons.Melee;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client._Sunrise.Weapons.Melee.Systems;

public sealed class UseSecondaryMeleeAltInteractSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;

    private EntityQuery<HandsComponent> _handsQuery;
    private EntityQuery<UseSecondaryMeleeAltInteractComponent> _redirectQuery;

    public override void Initialize()
    {
        base.Initialize();
        _handsQuery = GetEntityQuery<HandsComponent>();
        _redirectQuery = GetEntityQuery<UseSecondaryMeleeAltInteractComponent>();

        CommandBinds.Builder
            .BindAfter(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(OnUseSecondary, true, true), typeof(SharedInteractionSystem))
            .Register<UseSecondaryMeleeAltInteractSystem>();
    }

    private bool OnUseSecondary(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not { Valid: true } user)
            return false;

        if (!_combatMode.IsInCombatMode(user))
            return false;

        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon))
            return false;

        if (!_redirectQuery.HasComp(weaponUid) || !_blocker.CanAttack(user, weapon: (weaponUid, weapon)))
            return false;

        if (!_handsQuery.TryComp(user, out var hands) || hands.ActiveHandId == null)
            return false;

        RaisePredictiveEvent(new RequestHandAltInteractEvent(hands.ActiveHandId));
        return true;
    }
}
