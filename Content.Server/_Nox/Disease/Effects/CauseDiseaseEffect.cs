// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using System.Linq;
using Content.Server._Nox.Disease.Systems;
using Content.Shared.Body.Components;
using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.Disease.Effects;
using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Robust.Shared.Log;

namespace Content.Server._Nox.Disease.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CauseDiseaseEntityEffectsSystem : EntityEffectSystem<SolutionComponent, CauseDiseaseEffect>
{
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("CauseDiseaseEffect");
    }

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<CauseDiseaseEffect> args)
    {
        DiseaseData? data = null;

        var solution = entity.Comp.Solution;

        var contents = solution.Contents;
        _sawmill.Debug(
            $"CauseDiseaseEffect: start target={entity.Owner} user={args.User} scale={args.Scale} " +
            $"solutionVol={solution.Volume} reagents={contents.Count}");

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
        var hasBloodstream = HasComp<BloodstreamComponent>(entity.Owner);
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

        if (data.Infectivity <= 0f)
        {
            _sawmill.Debug(
                $"CauseDiseaseEffect: infectivity is {data.Infectivity:0.###} (<= 0), infection chance will be zero unless overridden elsewhere.");
        }

        DiseaseComponent component = new DiseaseComponent(data);

        _sawmill.Debug($"CauseDiseaseEffect: calling ProbInfect target={entity.Owner} strain='{component.Data.StrainId}'");
        _diseaseSystem.ProbInfect(component.Data, entity.Owner);
    }
}
