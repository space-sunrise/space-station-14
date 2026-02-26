// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using System.Linq;
using Content.Server._Nox.Disease.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._Nox.Disease.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Nox.Disease.Systems;

public sealed class DiseaseOutbreakRule : StationEventSystem<DiseaseOutbreakRuleComponent>
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("DiseaseOutbreakRule");
    }

    protected override void Added(EntityUid uid, DiseaseOutbreakRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        List<EntityUid> ents = new List<EntityUid>();

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is { } entity)
                ents.Add(entity);
        }

        if (component.Data == null)
            component.Data = _diseaseSystem.GenerateDiseaseData(component.SymptomsByDanger, component.BodyCount);

        var validEntities = ents
            .Where(ent => _diseaseSystem.CanInfect(ent, component.Data))
            .ToList();

        if (validEntities.Count <= 0)
        {
            _sawmill.Info("Не найдено сущностей, которых можно подвергнуть заражению вирусом.");
            return;
        }

        var toAdd = Math.Min(component.NumberPrimaryPacienst, validEntities.Count);

        for (var i = 0; i < toAdd; i++)
        {
            var picked = _random.PickAndTake(validEntities);

            _diseaseSystem.InfectEntity(component.Data, picked);

            var comp = EnsureComp<PrimaryPacientComponent>(picked);
            comp.StrainId = component.Data.StrainId;

            if (validEntities.Count == 0)
                break;
        }

    }
}
