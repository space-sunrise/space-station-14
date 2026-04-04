using Content.Server._Sunrise.LockableEquipment;
using Content.Shared._Sunrise.LockableEquipment;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.LockableEquipment;

public sealed class LockableEquipmentRegistrySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _registeredKeys = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LockableEquipmentComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, LockableEquipmentComponent component, ComponentInit args)
    {
        _registeredKeys[uid] = new HashSet<EntityUid>();
    }

    public void RegisterKey(EntityUid equipment, EntityUid key)
    {
        if (!HasComp<LockableEquipmentComponent>(equipment))
            return;

        if (!_registeredKeys.ContainsKey(equipment))
            _registeredKeys[equipment] = new HashSet<EntityUid>();

        _registeredKeys[equipment].Add(key);
    }

    public void UnregisterKey(EntityUid equipment, EntityUid key)
    {
        if (_registeredKeys.TryGetValue(equipment, out var keys))
        {
            keys.Remove(key);
        }
    }

    public bool IsKeyRegistered(EntityUid equipment, EntityUid key)
    {
        return _registeredKeys.TryGetValue(equipment, out var keys) && keys.Contains(key);
    }

    public IEnumerable<EntityUid> GetRegisteredKeys(EntityUid equipment)
    {
        return _registeredKeys.TryGetValue(equipment, out var keys) ? keys : Enumerable.Empty<EntityUid>();
    }
}