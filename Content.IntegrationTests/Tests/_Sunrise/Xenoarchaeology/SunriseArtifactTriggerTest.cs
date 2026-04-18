using Content.Shared._Sunrise.Research.Artifact;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Sunrise.Xenoarchaeology;

[TestFixture]
public sealed class SunriseArtifactTriggerTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: SunriseTestArtifactTriggerArtifact
  parent: BaseXenoArtifact
  name: artifact
  components:
  - type: XenoArtifact
    isGenerationRequired: false
    effectsTable: !type:NestedSelector
      tableId: XenoArtifactEffectsDefaultTable

- type: entity
  id: SunriseTestArtifactTriggerHealthAnalyzerNode
  name: artifact node
  components:
  - type: XenoArtifactNode
    maxDurability: 3
  - type: ArtifactHealthAnalyzerInteractionTrigger
""";

    [Test]
    public async Task HealthAnalyzerTrigger_EntityAnalyzedEvent_StartsUnlocking()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var artifactSystem = entManager.System<SharedXenoArtifactSystem>();

        await server.WaitPost(() =>
        {
            AssertTriggerStartsUnlocking<EntityAnalyzedEvent>(
                entManager,
                artifactSystem,
                "SunriseTestArtifactTriggerHealthAnalyzerNode",
                (entityManager, artifactUid, ev) => entityManager.EventBus.RaiseLocalEvent(artifactUid, ev));
        });
        await server.WaitRunTicks(1);

        await pair.CleanReturnAsync();
    }

    private static void AssertTriggerStartsUnlocking<TEvent>(
        IEntityManager entManager,
        SharedXenoArtifactSystem artifactSystem,
        string nodePrototype,
        Action<IEntityManager, EntityUid, TEvent> raiseEvent)
        where TEvent : HandledEntityEventArgs, new()
    {
        var artifactUid = entManager.Spawn("SunriseTestArtifactTriggerArtifact");
        Entity<XenoArtifactComponent> artifactEnt = (artifactUid, entManager.GetComponent<XenoArtifactComponent>(artifactUid));

        Assert.That(artifactSystem.AddNode(artifactEnt, nodePrototype, out var node, false));
        Assert.That(node, Is.Not.Null);

        var nodeIndex = artifactSystem.GetIndex(artifactEnt, node!.Value);
        var ev = new TEvent();
        raiseEvent(entManager, artifactUid, ev);

        Assert.That(ev.Handled, Is.True);
        Assert.That(entManager.TryGetComponent<XenoArtifactUnlockingComponent>(artifactUid, out var unlocking), Is.True);
        Assert.That(unlocking!.TriggeredNodeIndexes, Contains.Item(nodeIndex));
    }
}
