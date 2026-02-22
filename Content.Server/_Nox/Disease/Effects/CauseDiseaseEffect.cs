// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using System.Linq;
using Content.Server._Nox.Disease.Systems;
using Content.Shared.Body.Components;
using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.Disease.Effects;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared._Nox.Disease.Prototypes;
using Content.Shared.Mobs.Components;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server._Nox.Disease.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CauseDiseaseEntityEffectsSystem : EntityEffectSystem<BloodstreamComponent, CauseDiseaseEffect>
{
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("CauseDiseaseEffect");
    }

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<CauseDiseaseEffect> args)
    {
        DiseaseData? data = null;

        var container = new Entity<SolutionContainerManagerComponent?>(entity.Owner, null);
        if (!_solutionContainer.ResolveSolution(container, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution)
            || bloodSolution == null)
        {
            _sawmill.Debug(
                $"CauseDiseaseEffect: no bloodstream solution resolved for target={entity.Owner} user={args.User}");
            return;
        }

        var contents = bloodSolution.Contents;
        _sawmill.Debug(
            $"CauseDiseaseEffect: start target={entity.Owner} user={args.User} scale={args.Scale} " +
            $"solutionVol={bloodSolution.Volume} reagents={contents.Count}");

        if (contents.Count == 0)
        {
            _sawmill.Debug($"CauseDiseaseEffect: no reagents in solution on target={entity.Owner}");
            return;
        }

        foreach (var (reagentId, quantity) in contents)
        {
            var dataList = reagentId.Data;

            _sawmill.Debug(
                $"CauseDiseaseEffect: reagent={reagentId.Prototype} qty={quantity} " +
                $"hasData={(dataList != null)}");

            if (dataList == null)
                continue;

            var candidate = dataList.OfType<DiseaseData>().FirstOrDefault();
            if (candidate == null)
                continue;

            data = candidate;
            break;
        }

        if (data == null)
        {
            _sawmill.Debug($"CauseDiseaseEffect: no DiseaseData found in solution on target={entity.Owner}");
            return;
        }

        var hasMobState = HasComp<MobStateComponent>(entity.Owner);
        var hasBloodstream = true;
        var canInfect = _diseaseSystem.CanInfect(entity.Owner, data);

        var whitelistComps = data.EntityWhitelist?.Components == null
            ? "<null>"
            : string.Join(",", data.EntityWhitelist.Components);

        _sawmill.Debug(
            $"CauseDiseaseEffect: DiseaseData strain='{data.StrainId}' infectivity={data.Infectivity:0.###} " +
            $"bodyWhitelist={data.BodyWhitelist.Count} whitelistRequireAll={data.EntityWhitelist?.RequireAll} " +
            $"whitelistComps=[{whitelistComps}]");

        _sawmill.Debug(
            $"CauseDiseaseEffect: precheck target={entity.Owner} hasMobState={hasMobState} " +
            $"hasBloodstream={hasBloodstream} canInfect={canInfect}");

        var infectionData = (DiseaseData)data.CloneForInfection();

        if (data.Infectivity > 0f)
        {
            infectionData.Infectivity = data.Infectivity;
        }
        else
        {
            // Фоллбек: если infectivity не задана, считаем её по симптомам (как в UI для sentient disease).
            var computed = 0f;
            foreach (var symptomId in infectionData.ActiveSymptom)
            {
                if (_prototypeManager.TryIndex<DiseaseSymptomPrototype>(symptomId, out var proto))
                    computed += proto.AddInfectivity;
            }

            infectionData.Infectivity = Math.Clamp(computed, 0f, 1f);
            _sawmill.Debug(
                $"CauseDiseaseEffect: infectivity fallback computed={infectionData.Infectivity:0.###} from symptoms={infectionData.ActiveSymptom.Count}");
        }

        _sawmill.Debug(
            $"CauseDiseaseEffect: calling ProbInfect target={entity.Owner} strain='{infectionData.StrainId}' infectivity={infectionData.Infectivity:0.###}");
        _diseaseSystem.ProbInfect(infectionData, entity.Owner, args.User);
    }
}
