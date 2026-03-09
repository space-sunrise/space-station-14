using System.Collections.Generic;
using System.Linq;
using Content.Client.Administration.Managers;
using Content.Server.Administration.Managers;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Client.UserInterface;

namespace Content.IntegrationTests.Tests.Sunrise.DynamicAppearance;

public abstract class DynamicAppearanceSecurityTestBase : InteractionTest
{
    protected const string LockedAppearanceProtoId = "MobSlimePersonLockedAppearance";
    protected const string OnlyNameEditingProtoId = "MobSlimePersonOnlyNameEditing";
    private const string SponsorOnlyVoiceId = "Biden";
    protected const string InjectedBodyTypeId = "HumanSlimMale";
    private const string InjectedHeadLayerId = "MobHumanSlimHeadMale";

    public override async Task Setup()
    {
        await base.Setup();
        await DeAdminTestPlayer();
    }

    private async Task DeAdminTestPlayer()
    {
        var serverAdmin = Server.ResolveDependency<IAdminManager>();
        var clientAdmin = Client.ResolveDependency<IClientAdminManager>();

        await Server.WaitPost(() => serverAdmin.DeAdmin(ServerSession));
        await RunTicks(10);

        await Server.WaitAssertion(() =>
        {
            Assert.That(serverAdmin.IsAdmin(ServerSession), Is.False,
                "DynamicAppearance security tests must run as a non-admin player.");
        });

        await Client.WaitAssertion(() =>
        {
            Assert.That(clientAdmin.IsAdmin(), Is.False,
                "Client still has admin privileges, so DynamicAppearance security tests would be invalid.");
        });

        await Server.WaitPost(() =>
        {
            if (SEntMan.TryGetComponent<DynamicAppearanceComponent>(SEntMan.GetEntity(Player), out var dynamicAppearance))
                dynamicAppearance.SaveDelay = TimeSpan.Zero;
        });
    }

    protected async Task OpenAppearanceUi(NetEntity target)
    {
        await Client.WaitPost(() =>
        {
            var uid = CEntMan.GetEntity(target);
            var ui = CEntMan.GetComponent<UserInterfaceComponent>(uid);
            CUiSys.OpenUi((uid, ui), DynamicAppearanceUiKey.Key, predicted: true);
        });

        await RunTicks(15);
    }

    protected async Task AssertAppearanceUiRejected(NetEntity target, string message)
    {
        await Client.WaitAssertion(() =>
        {
            var uid = CEntMan.GetEntity(target);
            var ui = CEntMan.GetComponent<UserInterfaceComponent>(uid);

            var hasBui = CUiSys.TryGetOpenUi((uid, ui), DynamicAppearanceUiKey.Key, out var bui);
            Assert.That(hasBui && bui!.IsOpened, Is.False, message);
        });

        await RunTicks(15);

        await Server.WaitAssertion(() =>
        {
            Assert.That(IsUiOpen(DynamicAppearanceUiKey.Key), Is.False, message);
        });
    }

    protected static DynamicAppearanceState BuildState(IEntityManager entMan, EntityUid uid)
    {
        var humanoid = entMan.GetComponent<HumanoidAppearanceComponent>(uid);
        var meta = entMan.GetComponent<MetaDataComponent>(uid);

        return new DynamicAppearanceState(
            new MarkingSet(humanoid.MarkingSet),
            humanoid.Species,
            humanoid.Sex,
            humanoid.Age,
            humanoid.Gender,
            humanoid.Voice,
            humanoid.SkinColor,
            humanoid.EyeColor,
            new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(humanoid.CustomBaseLayers),
            humanoid.BodyType,
            humanoid.Width,
            humanoid.Height,
            meta.EntityName);
    }

    protected async Task AssertSponsorVoiceInjectionRejected(NetEntity target)
    {
        var state = default(DynamicAppearanceState);
        await Server.WaitPost(() => state = BuildState(SEntMan, SEntMan.GetEntity(target)));

        state = state with
        {
            Sex = Sex.Male,
            Voice = SponsorOnlyVoiceId,
        };

        await SendBui(DynamicAppearanceUiKey.Key, new DynamicAppearanceSaveMessage(state), target);

        await Server.WaitAssertion(() =>
        {
            var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(SEntMan.GetEntity(target));
            Assert.That(humanoid.Voice, Is.Not.EqualTo(SponsorOnlyVoiceId),
                "Non-sponsor managed to inject a sponsor-only voice through DynamicAppearance.");
        });
    }

    protected async Task AssertInvalidBodyTypeInjectionRejected(NetEntity target)
    {
        var state = default(DynamicAppearanceState);
        await Server.WaitPost(() => state = BuildState(SEntMan, SEntMan.GetEntity(target)));

        state = state with
        {
            BodyType = InjectedBodyTypeId,
        };

        await SendBui(DynamicAppearanceUiKey.Key, new DynamicAppearanceSaveMessage(state), target);

        await Server.WaitAssertion(() =>
        {
            var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(SEntMan.GetEntity(target));
            Assert.That(humanoid.BodyType, Is.Not.EqualTo(InjectedBodyTypeId),
                "DynamicAppearance accepted an invalid body type through a forged save payload.");
        });
    }

    protected async Task AssertCustomBaseLayerInjectionRejected(NetEntity target)
    {
        var state = default(DynamicAppearanceState);
        await Server.WaitPost(() => state = BuildState(SEntMan, SEntMan.GetEntity(target)));

        Assert.That(state.CustomBaseLayers.ContainsKey(HumanoidVisualLayers.Head), Is.False,
            "Test target unexpectedly started with a custom head base layer.");

        await Server.WaitPost(() =>
        {
            state.CustomBaseLayers[HumanoidVisualLayers.Head] = new CustomBaseLayerInfo
            {
                Id = InjectedHeadLayerId,
                Color = Color.Red,
            };
        });

        await SendBui(DynamicAppearanceUiKey.Key, new DynamicAppearanceSaveMessage(state), target);

        await Server.WaitAssertion(() =>
        {
            var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(SEntMan.GetEntity(target));
            Assert.That(humanoid.CustomBaseLayers.ContainsKey(HumanoidVisualLayers.Head), Is.False,
                "DynamicAppearance accepted a forged custom base layer even though the UI has no such control.");
        });
    }

    protected async Task AssertMalformedMarkingPayloadRejected(NetEntity target)
    {
        var state = default(DynamicAppearanceState);
        await Server.WaitPost(() => state = BuildState(SEntMan, SEntMan.GetEntity(target)));

        await Server.WaitPost(() =>
        {
            var uid = SEntMan.GetEntity(target);
            var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(uid);
            var protoMan = Server.ResolveDependency<IPrototypeManager>();
            var markingManager = Server.ResolveDependency<MarkingManager>();

            var markings = state.MarkingSet.GetForwardEnumerator().Select(marking => new Marking(marking)).ToList();

            if (markings.Count == 0)
            {
                var fallback = markingManager.Markings.Values.First(proto =>
                    markingManager.CanBeApplied(humanoid.Species, humanoid.Sex, proto, protoMan));
                markings.Add(fallback.AsMarking());
            }

            markings[0] = new Marking(markings[0].MarkingId, new List<Color>());
            state = state with { MarkingSet = new MarkingSet(markings) };
        });

        await SendBui(DynamicAppearanceUiKey.Key, new DynamicAppearanceSaveMessage(state), target);

        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.GetEntity(target);
            var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(uid);
            var protoMan = Server.ResolveDependency<IPrototypeManager>();

            Assert.That(
                humanoid.MarkingSet.GetForwardEnumerator().All(marking =>
                    protoMan.Index<MarkingPrototype>(marking.MarkingId).Sprites.Count == marking.MarkingColors.Count),
                Is.True,
                "DynamicAppearance accepted a malformed marking payload with an invalid color count.");
        });
    }
}

[TestFixture]
public sealed class DynamicAppearanceSecurityTest : DynamicAppearanceSecurityTestBase
{
    protected override string PlayerPrototype => "MobSlimePerson";

    [Test]
    public async Task NonOwnerCannotOpenOtherEntityUi()
    {
        await SpawnTarget("MobSlimePerson");

        await OpenAppearanceUi(Target!.Value);

        await AssertAppearanceUiRejected(Target.Value,
            "DynamicAppearance UI unexpectedly opened for a non-owner.");
    }

    [Test]
    public async Task OwnerCannotInjectSponsorVoiceViaSaveMessage()
    {
        Target = Player;

        await OpenAppearanceUi(Player);
        Assert.That(TryGetBui(DynamicAppearanceUiKey.Key, out _, Player), Is.True,
            "Owner failed to open their own DynamicAppearance UI.");

        await AssertSponsorVoiceInjectionRejected(Player);
    }

    [Test]
    public async Task OwnerCannotInjectInvalidBodyTypeViaSaveMessage()
    {
        Target = Player;

        await OpenAppearanceUi(Player);
        Assert.That(TryGetBui(DynamicAppearanceUiKey.Key, out _, Player), Is.True,
            "Owner failed to open their own DynamicAppearance UI.");

        await AssertInvalidBodyTypeInjectionRejected(Player);
    }

    [Test]
    public async Task OwnerCannotInjectCustomBaseLayersViaSaveMessage()
    {
        Target = Player;

        await OpenAppearanceUi(Player);
        Assert.That(TryGetBui(DynamicAppearanceUiKey.Key, out _, Player), Is.True,
            "Owner failed to open their own DynamicAppearance UI.");

        await AssertCustomBaseLayerInjectionRejected(Player);
    }

    [Test]
    public async Task OwnerCannotInjectMalformedMarkingPayloadViaSaveMessage()
    {
        Target = Player;

        await OpenAppearanceUi(Player);
        Assert.That(TryGetBui(DynamicAppearanceUiKey.Key, out _, Player), Is.True,
            "Owner failed to open their own DynamicAppearance UI.");

        await AssertMalformedMarkingPayloadRejected(Player);
    }
}

[TestFixture]
public sealed class DynamicAppearanceLockedSecurityTest : DynamicAppearanceSecurityTestBase
{
    protected override string PlayerPrototype => LockedAppearanceProtoId;

    [TestPrototypes]
    private const string LockedAllTestProto = """
- type: entity
  parent: MobSlimePerson
  id: MobSlimePersonLockedAppearance
  components:
  - type: DynamicAppearance
    allowedFields: None
    saveDelay: 0
""";

    [Test]
    public async Task OwnerCannotOpenUiWhenAllFieldsAreDisabled()
    {
        Target = Player;

        await OpenAppearanceUi(Player);

        await AssertAppearanceUiRejected(Player,
            "DynamicAppearance UI unexpectedly opened while all fields were disabled.");
    }
}

[TestFixture]
public sealed class DynamicAppearanceOnlyNameSecurityTest : DynamicAppearanceSecurityTestBase
{
    protected override string PlayerPrototype => OnlyNameEditingProtoId;

    [TestPrototypes]
    private const string LockedNameEditingTestProto = """
- type: entity
  parent: MobSlimePerson
  id: MobSlimePersonOnlyNameEditing
  components:
  - type: DynamicAppearance
    allowedFields: Name
    saveDelay: 0
""";

    private readonly ProtoId<MarkingPrototype> _testMarkingId = "VulpBellyFox";

    [Test]
    public async Task OwnerCannotEditNonNameFieldsWhenOnlyNameEditingAllowed()
    {
        Target = Player;

        await OpenAppearanceUi(Player);

        var protoMan = Server.ResolveDependency<IPrototypeManager>();

        Assert.That(TryGetBui(DynamicAppearanceUiKey.Key, out _, Player), Is.True,
            "Owner failed to open their own DynamicAppearance UI.");

        var state = default(DynamicAppearanceState);
        await Server.WaitPost(() => state = BuildState(SEntMan, SEntMan.GetEntity(Player)));
        var modifiedState = default(DynamicAppearanceState);

        await Server.WaitPost(() =>
        {
            modifiedState = state with
            {
                Age = 42,
                Name = "Urist Someone",
                Sex = Sex.Unsexed,
                Gender = Robust.Shared.Enums.Gender.Neuter,
                Species = "Vulpkanin",
                BodyType = InjectedBodyTypeId,
                SkinColor = Color.DarkOrange,
                EyeColor = Color.Cyan,
                MarkingSet = new MarkingSet(new List<Marking>
                {
                    protoMan.Index<MarkingPrototype>(_testMarkingId).AsMarking(),
                }),
                Width = 1.1f,
                Height = 1.1f,
            };
        });

        await SendBui(DynamicAppearanceUiKey.Key, new DynamicAppearanceSaveMessage(modifiedState), Player);

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(SEntMan.GetEntity(Player));
                // Age
                Assert.That(humanoid.Age, Is.Not.EqualTo(modifiedState.Age),
                    "DynamicAppearance accepted a save message that edited the age field even though only name editing was allowed.");
                // Sex
                Assert.That(humanoid.Sex, Is.Not.EqualTo(modifiedState.Sex),
                    "DynamicAppearance accepted a save message that edited the sex field even though only name editing was allowed.");
                // Gender
                Assert.That(humanoid.Gender, Is.Not.EqualTo(modifiedState.Gender),
                    "DynamicAppearance accepted a save message that edited the gender field even though only name editing was allowed.");
                // Species
                Assert.That(humanoid.Species, Is.Not.EqualTo(modifiedState.Species),
                    "DynamicAppearance accepted a save message that edited the species field even though only name editing was allowed.");
                // Body type
                Assert.That(humanoid.BodyType, Is.Not.EqualTo(modifiedState.BodyType),
                    "DynamicAppearance accepted a save message that edited the body type field even though only name editing was allowed.");
                // Skin Color
                Assert.That(humanoid.SkinColor, Is.Not.EqualTo(modifiedState.SkinColor),
                    "DynamicAppearance accepted a save message that edited the skin color field even though only name editing was allowed.");
                // Eye color
                Assert.That(humanoid.EyeColor, Is.Not.EqualTo(modifiedState.EyeColor),
                    "DynamicAppearance accepted a save message that edited the eye color field even though only name editing was allowed.");
                // Markings
                Assert.That(humanoid.MarkingSet, Is.Not.EqualTo(modifiedState.MarkingSet),
                    "DynamicAppearance accepted a save message that edited the markings field even though only name editing was allowed.");
                // Size
                Assert.That(humanoid.Width, Is.Not.EqualTo(modifiedState.Width),
                    "DynamicAppearance accepted a save message that edited the width field even though only name editing was allowed.");
                Assert.That(humanoid.Height, Is.Not.EqualTo(modifiedState.Height),
                    "DynamicAppearance accepted a save message that edited the height field even though only name editing was allowed.");

                // Name should change, though
                var meta = SEntMan.GetComponent<MetaDataComponent>(SEntMan.GetEntity(Player));
                Assert.That(meta.EntityName, Is.EqualTo(modifiedState.Name),
                    "DynamicAppearance did not accept a save message that edited the name field even though name editing was the only allowed edit.");
            });
        });
    }
}

[TestFixture]
public sealed class DynamicAppearanceSpeciesInventorySyncTest : DynamicAppearanceSecurityTestBase
{
    private const string SpeciesEditingProtoId = "MobHumanSpeciesEditing";

    protected override string PlayerPrototype => SpeciesEditingProtoId;

    [TestPrototypes]
    private const string SpeciesEditingTestProto = """
- type: entity
  parent: MobHuman
  id: MobHumanSpeciesEditing
  components:
  - type: DynamicAppearance
    allowedFields: Species, BodyType
    saveDelay: 0
""";

    [Test]
    public async Task OwnerSpeciesChangeUpdatesInventorySpeciesData()
    {
        Target = Player;

        await OpenAppearanceUi(Player);
        Assert.That(TryGetBui(DynamicAppearanceUiKey.Key, out _, Player), Is.True,
            "Owner failed to open their own DynamicAppearance UI.");

        var state = default(DynamicAppearanceState);
        await Server.WaitPost(() => state = BuildState(SEntMan, SEntMan.GetEntity(Player)));

        state = state with
        {
            Species = "Reptilian",
            BodyType = "ReptilianNormal",
        };

        await SendBui(DynamicAppearanceUiKey.Key, new DynamicAppearanceSaveMessage(state), Player);

        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.GetEntity(Player);
            var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(uid);
            var inventory = SEntMan.GetComponent<InventoryComponent>(uid);

            Assert.Multiple(() =>
            {
                Assert.That(humanoid.Species, Is.EqualTo("Reptilian"),
                    "DynamicAppearance failed to apply the requested species.");
                Assert.That(humanoid.BodyType, Is.EqualTo("ReptilianNormal"),
                    "DynamicAppearance failed to apply the target species body type.");
                Assert.That(inventory.SpeciesId, Is.EqualTo("reptilian"),
                    "Inventory species visuals were not refreshed after changing species.");
            });
        });
    }
}
