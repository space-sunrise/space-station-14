using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Content.Shared.GameTicking;

namespace Content.Shared._Sunrise.CollectiveMind;

public sealed class CollectiveMindUpdateSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    private ISawmill _sawmill = default!;
    private static Dictionary<CollectiveMindPrototype, int> _globalMindIDTracker = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("CollectiveMindUpdateSystem");

        SubscribeLocalEvent<CollectiveMindComponent, ComponentStartup>(OnCollectiveMindInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnCollectiveMindInit(EntityUid uid, CollectiveMindComponent component, ComponentStartup args)
    {
        UpdateCollectiveMind(uid, component);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _globalMindIDTracker.Clear();
    }

    public void ForceCloneFrom(EntityUid sourceuid, EntityUid targetuid)
    {
        if (!TryComp<CollectiveMindComponent>(sourceuid, out var component))
            return;

        if (!TryComp<CollectiveMindComponent>(targetuid, out var targetComponent))
            return;

        targetComponent.Minds.Clear();

        foreach (var mind in component.Minds)
        {
            targetComponent.Minds.Add(mind.Key, mind.Value);
        }

        UpdateCollectiveMind(targetuid, targetComponent);
    }

    public void UpdateCollectiveMind(EntityUid uid, CollectiveMindComponent collective)
    {
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>())
        {
            if (prototype.Whitelist == null)
                continue;

            var meetsRequirements = _whitelistSystem.CheckBoth(uid, prototype.Blacklist, prototype.Whitelist);

            if (meetsRequirements)
            {
                if (collective.Minds.ContainsKey(prototype))
                    continue;

                collective.Minds.TryAdd(prototype, CreateNewCollectiveMindMemberData(prototype));
            }
            else
            {
                collective.Minds.Remove(prototype);
            }
        }
    }

    private static CollectiveMindMemberData CreateNewCollectiveMindMemberData(CollectiveMindPrototype prototype)
    {
        if (!_globalMindIDTracker.ContainsKey(prototype))
        {
            _globalMindIDTracker[prototype] = new CollectiveMindMemberData().MindId;
        }

        var data = new CollectiveMindMemberData
        {
            MindId = _globalMindIDTracker[prototype]
        };

        _globalMindIDTracker[prototype]++;

        return data;
    }
}
