using System.Linq;
using Content.Server._Sunrise.Antag.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Antag;

public sealed class AntagRoleBlacklistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private List<AntagRoleBlacklistPrototype> _rules = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        RefreshRules();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.ByType.ContainsKey(typeof(AntagRoleBlacklistPrototype)))
            RefreshRules();
    }

    private void RefreshRules()
    {
        _rules = _prototypes.EnumeratePrototypes<AntagRoleBlacklistPrototype>().ToList();
    }

    /// <summary>
    /// Проверяет, заблокирована ли сущность для получения хотя бы одной из указанных ролей разума.
    /// </summary>
    public bool IsBlocked(EntityUid uid, IEnumerable<EntProtoId> mindRoles)
    {
        if (_rules.Count == 0)
            return false;

        foreach (var rule in _rules)
        {
            if (!_whitelist.IsValid(rule.Blacklist, uid))
                continue;

            foreach (var role in mindRoles)
            {
                if (rule.MindRoles.Contains(role))
                    return true;
            }
        }

        return false;
    }
}
