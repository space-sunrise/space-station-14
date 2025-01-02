// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using Content.Shared._Sunrise.Disease;
using Content.Server.Objectives;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Sunrise.Disease;

public sealed class DiseaseRuleSystem : GameRuleSystem<DiseaseRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnObjectivesTextGetInfo(EntityUid uid, DiseaseRuleComponent comp, ref ObjectivesTextGetInfoEvent args)
    {
        args.Minds = comp.DiseasesMinds;
        args.AgentName = "разумная болезнь";
    }

    protected override void AppendRoundEndText(EntityUid uid,
        DiseaseRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var sick = EntityQueryEnumerator<SickComponent>();
        var immune = EntityQueryEnumerator<DiseaseImmuneComponent>();
        var disease = EntityQueryEnumerator<DiseaseRoleComponent>();
        int infected = 0;
        int immuned = 0;
        int infects = 0;
        while (sick.MoveNext(out _))
        {
            infects++;
        }
        while (immune.MoveNext(out _))
        {
            immuned++;
        }
        while (disease.MoveNext(out var comp))
        {
            infected = comp.SickOfAllTime;
        }
        args.AddLine(Loc.GetString("disease-round-end-result"));
        args.AddLine(Loc.GetString("disease-round-end-result-infected", ("count", infected)));
        args.AddLine(Loc.GetString("disease-round-end-result-infects", ("count", infects)));
        args.AddLine(Loc.GetString("disease-round-end-result-immuned", ("count", immuned)));
    }
}
