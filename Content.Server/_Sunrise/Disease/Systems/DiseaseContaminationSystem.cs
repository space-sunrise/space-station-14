using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.TimeWindow;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameStates;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed class DiseaseContaminationSystem : EntitySystem
{
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindow = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseContaminationComponent, ComponentInit>(OnContaminationInit);
        SubscribeLocalEvent<DiseaseContaminationComponent, ComponentGetState>(OnGetState);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseContaminationComponent>();
        while (query.MoveNext(out var uid, out var contamination))
        {
            if (contamination.Contamination <= 0f || contamination.Data == null)
                continue;

            if (!_timedWindow.IsExpired(contamination.SpreadWindow))
                continue;

            _timedWindow.Reset(contamination.SpreadWindow);

            if (!TryComp<DiseaseComponent>(uid, out var disease))
                continue;

            _disease.InfectAround(uid, contamination.SpreadRange, disease);
        }
    }

    private void OnGetState(Entity<DiseaseContaminationComponent> ent, ref ComponentGetState args)
    {
        if (ent.Comp.Data == null)
            return;

        args.State = new DiseaseContaminationComponentState(ent.Comp.Data.Color, ent.Comp.Contamination);
    }

    private void OnContaminationInit(Entity<DiseaseContaminationComponent> ent, ref ComponentInit args)
    {
        _timedWindow.Reset(ent.Comp.SpreadWindow);
    }

    public bool TryContaminateFromCloud(Entity<DiseaseInfectionCloudComponent?> cloud, EntityUid target)
    {
        if (!Resolve(cloud, ref cloud.Comp, false))
            return false;

        if (cloud.Comp.Data == null)
            return false;

        var cloudData = cloud.Comp.Data;

        var contamination = EnsureComp<DiseaseContaminationComponent>(target);

        var contaminationData = contamination.Data;
        if (contaminationData == null || _disease.ThisDiseasIsStronger(cloudData, contaminationData))
        {
            contaminationData = (DiseaseData)cloudData.CloneForInfection();
            contamination.Data = contaminationData;
        }

        var gain = Math.Max(0.01f, contaminationData.Infectivity * contamination.CollisionContaminationGain);
        contamination.Contamination = Math.Clamp(contamination.Contamination + gain, 0f, 1f);
        contamination.Color = cloudData.Color;

        Dirty(target, contamination);
        return true;
    }
}