using Content.Shared.Materials;
using Content.Shared.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Materials.MaterialSilo;

/// <summary>
/// Полностью независимая от ванильного OreSilo система хранилища материалов, работающая в пределах всего грида.
/// Не имеет общего кода с <c>SharedOreSiloSystem</c>, чтобы её изменение никогда не затрагивало ванильный силос.
/// </summary>
public abstract class SharedSunriseMaterialSiloSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<SunriseMaterialSiloClientComponent> _clientQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SunriseMaterialSiloComponent, ToggleSunriseMaterialSiloClientMessage>(OnToggleClient);
        SubscribeLocalEvent<SunriseMaterialSiloComponent, ComponentShutdown>(OnSiloShutdown);
        Subs.BuiEvents<SunriseMaterialSiloComponent>(SunriseMaterialSiloUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            });

        SubscribeLocalEvent<SunriseMaterialSiloClientComponent, GetStoredMaterialsEvent>(OnGetStoredMaterials);
        SubscribeLocalEvent<SunriseMaterialSiloClientComponent, ConsumeStoredMaterialsEvent>(OnConsumeStoredMaterials);
        SubscribeLocalEvent<SunriseMaterialSiloClientComponent, ComponentShutdown>(OnClientShutdown);

        _clientQuery = GetEntityQuery<SunriseMaterialSiloClientComponent>();
    }

    private void OnToggleClient(Entity<SunriseMaterialSiloComponent> ent, ref ToggleSunriseMaterialSiloClientMessage args)
    {
        var client = GetEntity(args.Client);

        if (!_clientQuery.TryComp(client, out var clientComp))
            return;

        if (ent.Comp.Clients.Contains(client)) // отключаем клиента
        {
            clientComp.Silo = null;
            Dirty(client, clientComp);
            ent.Comp.Clients.Remove(client);
            Dirty(ent);

            UpdateSiloUi(ent);
        }
        else // подключаем клиента
        {
            if (!CanTransmitMaterials((ent, ent), client))
                return;

            var clientMats = _materialStorage.GetStoredMaterials(client, true);
            var inverseMats = new Dictionary<string, int>();
            foreach (var (mat, amount) in clientMats)
            {
                inverseMats.Add(mat, -amount);
            }
            _materialStorage.TryChangeMaterialAmount(client, inverseMats, localOnly: true);
            _materialStorage.TryChangeMaterialAmount(ent.Owner, clientMats);

            ent.Comp.Clients.Add(client);
            Dirty(ent);
            clientComp.Silo = ent;
            Dirty(client, clientComp);

            UpdateSiloUi(ent);
        }
    }

    private void OnBoundUIOpened(Entity<SunriseMaterialSiloComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateSiloUi(ent);
    }

    private void OnSiloShutdown(Entity<SunriseMaterialSiloComponent> ent, ref ComponentShutdown args)
    {
        foreach (var client in ent.Comp.Clients)
        {
            if (!_clientQuery.TryComp(client, out var comp))
                continue;

            comp.Silo = null;
            Dirty(client, comp);
        }
    }

    protected virtual void UpdateSiloUi(Entity<SunriseMaterialSiloComponent> ent)
    {
    }

    private void OnGetStoredMaterials(Entity<SunriseMaterialSiloClientComponent> ent, ref GetStoredMaterialsEvent args)
    {
        if (args.LocalOnly)
            return;

        if (ent.Comp.Silo is not { } silo)
            return;

        if (!CanTransmitMaterials(silo, ent))
            return;

        var materials = _materialStorage.GetStoredMaterials(silo);

        foreach (var (mat, amount) in materials)
        {
            // Не выдаём материалы, к которым у сущности обычно нет доступа.
            if (!_materialStorage.IsMaterialWhitelisted((args.Entity, args.Entity), mat))
                continue;

            var existing = args.Materials.GetOrNew(mat);
            args.Materials[mat] = existing + amount;
        }
    }

    private void OnConsumeStoredMaterials(Entity<SunriseMaterialSiloClientComponent> ent, ref ConsumeStoredMaterialsEvent args)
    {
        if (args.LocalOnly)
            return;

        if (ent.Comp.Silo is not { } silo || !TryComp<MaterialStorageComponent>(silo, out var materialStorage))
            return;

        if (!CanTransmitMaterials(silo, ent))
            return;

        foreach (var (mat, amount) in args.Materials)
        {
            if (!_materialStorage.TryChangeMaterialAmount(silo, mat, amount, materialStorage))
                continue;
            args.Materials[mat] = 0;
        }
    }

    private void OnClientShutdown(Entity<SunriseMaterialSiloClientComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SunriseMaterialSiloComponent>(ent.Comp.Silo, out var silo))
            return;

        silo.Clients.Remove(ent);
        Dirty(ent.Comp.Silo.Value, silo);
        UpdateSiloUi((ent.Comp.Silo.Value, silo));
    }

    /// <summary>
    /// Проверяет, может ли данный клиент подключиться и получать материалы от силоса.
    /// Силос работает в пределах всего грида, поэтому дистанция не проверяется, только питание и общий грид.
    /// </summary>
    [PublicAPI]
    public bool CanTransmitMaterials(Entity<SunriseMaterialSiloComponent?, TransformComponent?> silo, EntityUid client)
    {
        if (!Resolve(silo, ref silo.Comp1, ref silo.Comp2))
            return false;

        if (!_powerReceiver.IsPowered(silo.Owner))
            return false;

        if (_transform.GetGrid(client) != _transform.GetGrid(silo.Owner))
            return false;

        return true;
    }
}
