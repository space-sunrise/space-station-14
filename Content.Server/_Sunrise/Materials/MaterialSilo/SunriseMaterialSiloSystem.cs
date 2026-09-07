using Content.Server.Pinpointer;
using Content.Shared._Sunrise.Materials.MaterialSilo;
using Content.Shared.IdentityManagement;
using Robust.Server.GameStates;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Materials.MaterialSilo;

/// <inheritdoc/>
public sealed class SunriseMaterialSiloSystem : SharedSunriseMaterialSiloSystem
{
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    private const float PreloadRangeSquared = 225f; // ~1 screen

    private readonly HashSet<(NetEntity, string, string)> _clientInformation = new();
    private readonly HashSet<EntityUid> _silosToAdd = new();
    private readonly HashSet<EntityUid> _silosToRemove = new();

    protected override void UpdateSiloUi(Entity<SunriseMaterialSiloComponent> ent)
    {
        if (!_userInterface.IsUiOpen(ent.Owner, SunriseMaterialSiloUiKey.Key))
            return;
        _clientInformation.Clear();

        var xform = Transform(ent);
        var gridUid = xform.GridUid;

        // Силос работает на весь грид, поэтому в списке для подключения показываем всех ещё не привязанных
        // клиентов этого грида, а не только тех, что попадают в какой-то радиус.
        var query = EntityQueryEnumerator<SunriseMaterialSiloClientComponent, TransformComponent>();
        while (query.MoveNext(out var clientUid, out var clientComp, out var clientXform))
        {
            if (clientXform.GridUid != gridUid)
                continue;

            // не показываем уже привязанных клиентов в этом списке отдельно, они добавляются ниже
            if (clientComp.Silo is not null)
                continue;

            if (!CanTransmitMaterials((ent, ent, xform), clientUid))
                continue;

            var netEnt = GetNetEntity(clientUid);
            var name = Identity.Name(clientUid, EntityManager);
            var beacon = _navMap.GetNearestBeaconString(clientUid, onlyName: true);

            var txt = Loc.GetString("sunrise-material-silo-ui-itemlist-entry",
                ("name", name),
                ("beacon", beacon),
                ("linked", false),
                ("inRange", true));

            _clientInformation.Add((netEnt, txt, beacon));
        }

        // Все подключённые клиенты, включая тех, что временно не в питании/на другом гриде.
        foreach (var client in ent.Comp.Clients)
        {
            var netEnt = GetNetEntity(client);
            var name = Identity.Name(client, EntityManager);
            var beacon = _navMap.GetNearestBeaconString(client, onlyName: true);
            var inRange = CanTransmitMaterials((ent, ent, xform), client);

            var txt = Loc.GetString("sunrise-material-silo-ui-itemlist-entry",
                ("name", name),
                ("beacon", beacon),
                ("linked", true),
                ("inRange", inRange));

            _clientInformation.Add((netEnt, txt, beacon));
        }

        _userInterface.SetUiState(ent.Owner, SunriseMaterialSiloUiKey.Key, new SunriseMaterialSiloBuiState(_clientInformation));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Аналогично ванильному OreSilo: подгружаем силос через PVS-оверрайд игрокам рядом с подключёнными
        // клиентами, чтобы не было мисредикта по количеству материалов.
        var actorQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (actorQuery.MoveNext(out _, out var actorComp, out var actorXform))
        {
            _silosToAdd.Clear();
            _silosToRemove.Clear();

            var clientQuery = EntityQueryEnumerator<SunriseMaterialSiloClientComponent, TransformComponent>();
            while (clientQuery.MoveNext(out _, out var clientComp, out var clientXform))
            {
                if (clientComp.Silo == null)
                    continue;

                if (actorXform.GridUid != clientXform.GridUid)
                    continue;

                if ((actorXform.LocalPosition - clientXform.LocalPosition).LengthSquared() <= PreloadRangeSquared)
                {
                    _silosToAdd.Add(clientComp.Silo.Value);
                }
                else
                {
                    _silosToRemove.Add(clientComp.Silo.Value);
                }
            }

            foreach (var toRemove in _silosToRemove)
            {
                _pvsOverride.RemoveSessionOverride(toRemove, actorComp.PlayerSession);
            }
            foreach (var toAdd in _silosToAdd)
            {
                _pvsOverride.AddSessionOverride(toAdd, actorComp.PlayerSession);
            }
        }
    }
}
