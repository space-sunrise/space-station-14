using Content.Server.CartridgeLoader;
using Content.Server.PDA;
using Content.Server._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared._Sunrise.Messenger;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Player;
using Robust.Server.GameObjects;

namespace Content.Server._Sunrise.Messenger;

public sealed class MessengerSystem : EntitySystem
{
    [Dependency] private readonly PdaSystem _pda = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<OpenMessengerRequestEvent>(OnOpenMessengerRequest);
    }

    private void OnOpenMessengerRequest(OpenMessengerRequestEvent ev, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryFindPDA(user.Value, out var pda))
            return;

        // Try to find the messenger program
        if (!_cartridgeLoader.TryGetProgram<MessengerCartridgeComponent>(pda.Value, out var programUid) || programUid is not { } program)
            return;

        // Activate it
        _cartridgeLoader.ActivateProgram(pda.Value, program);

        // Open PDA UI
        _ui.OpenUi(pda.Value, PdaUiKey.Key, args.SenderSession);
    }

    private bool TryFindPDA(EntityUid user, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EntityUid? pda)
    {
        pda = null;

        // 1. Check active hand
        if (_hands.TryGetActiveItem(user, out var heldItem) && HasComp<PdaComponent>(heldItem))
        {
            pda = heldItem;
            return true;
        }

        // 2. Check ID slot
        if (_inventory.TryGetSlotEntity(user, "id", out var idItem) && HasComp<PdaComponent>(idItem))
        {
            pda = idItem;
            return true;
        }

        // 3. Check all hands
        foreach (var item in _hands.EnumerateHeld(user))
        {
            if (HasComp<PdaComponent>(item))
            {
                pda = item;
                return true;
            }
        }

        return false;
    }
}
