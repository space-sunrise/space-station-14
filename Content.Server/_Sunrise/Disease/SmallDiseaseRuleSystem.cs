using Content.Shared._Sunrise.Disease;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Store;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Map;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Disease;

public sealed class SmallDiseaseRuleSystem : GameRuleSystem<SmallDiseaseRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    protected override void Started(EntityUid uid, SmallDiseaseRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        Timer.Spawn(TimeSpan.FromMinutes(6), () =>
        {
            var message = Loc.GetString("disease-biohazard-announcement");
            var sender = Loc.GetString("disease-biohazard-announcement-sender");

            _chatSystem.DispatchGlobalAnnouncement(message, sender, playDefault: true, colorOverride: Color.Red);
        });

        // 1. Find potential victims (Humanoid, Has Mind, Not Dead, Not Sick)
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent, MobStateComponent, TransformComponent>();
        var candidates = new List<EntityUid>();

        while (query.MoveNext(out var entity, out _, out _, out _, out var xform))
        {
            if (HasComp<SickComponent>(entity))
                continue;

            // Simple check to ensure they are on a station/grid generally
            if (xform.GridUid == null)
                continue;

            candidates.Add(entity);
        }

        if (candidates.Count == 0)
            return;

        // 2. Spawn the Disease Entity (Dummy)
        var diseaseUid = Spawn("MobDisease", MapCoordinates.Nullspace);
        if (!TryComp<DiseaseRoleComponent>(diseaseUid, out var diseaseComp))
        {
            return;
        }

        // 3. Configure Symptoms
        // Always add Cough
        diseaseComp.Symptoms.TryAdd("Cough", new SymptomData(2, 5)); // Cough is level 2 typically

        var currentPoints = 0;
        var availableSymptoms = new List<ListingPrototype>();

        // Gather all symptom listings
        foreach (var listing in _prototypeManager.EnumeratePrototypes<ListingPrototype>())
        {
            if (listing.Categories.Contains("DiseaseSymptomsCategory"))
            {
                availableSymptoms.Add(listing);
            }
        }

        // Randomly add until 50 points
        // Safety loop
        for (int i = 0; i < 20 && currentPoints < component.TargetSymptomPoints; i++)
        {
            if (availableSymptoms.Count == 0) break;

            var pick = _random.Pick(availableSymptoms);
            var cost = pick.Cost.GetValueOrDefault("DiseasePoints", 0);

            // Avoid adding same symptom twice
            if (!diseaseComp.Symptoms.ContainsKey(pick.ID))
            {
                // Determine level (rough approximation or default)
                // We'll use 0-5 as safe defaults or check ID specific logic if strictly needed
                // Using 1-5 generic
                diseaseComp.Symptoms.Add(pick.ID, new SymptomData(1, 5));
                currentPoints += cost.Int();
            }
        }

        // 4. Infect Targets
        _random.Shuffle(candidates);
        var targetCount = Math.Min(component.TargetInfectedCount, candidates.Count);

        for (int i = 0; i < targetCount; i++)
        {
            var victim = candidates[i];
            var sick = EnsureComp<SickComponent>(victim);
            sick.owner = diseaseUid;
            diseaseComp.Infected.Add(victim);
        }
    }
}
