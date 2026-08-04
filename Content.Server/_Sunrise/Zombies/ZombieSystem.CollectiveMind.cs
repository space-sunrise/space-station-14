using Content.Server._Sunrise.CollectiveMind;
using Content.Shared._Sunrise.CollectiveMind;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Пространство имён не соответствует структуре папок
namespace Content.Server.Zombies;

public sealed partial class ZombieSystem
{
    [Dependency] private readonly CollectiveMindSystem _collectiveMind = default!;

    private static readonly ProtoId<CollectiveMindPrototype> _zombieCollectiveMind = "Zombie";

    private void SetZombieCollectiveMind(EntityUid target)
    {
        var collectiveMind = EnsureComp<CollectiveMindComponent>(target);
        collectiveMind.Memberships.Clear();
        _collectiveMind.TryAddMember(target, _zombieCollectiveMind);
    }
}
