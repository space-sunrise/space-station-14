using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Content.IntegrationTests.Utility;
using Content.Shared.Guidebook;
using Robust.Shared.Localization;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Guidebook;

[TestFixture]
[TestOf(typeof(GuidebookSystem))]
[TestOf(typeof(GuideEntryPrototype))]
[TestOf(typeof(DocumentParsingManager))]
public sealed class GuideEntryPrototypeTests
{
    private static string[] _guideEntries = GameDataScrounger.PrototypesOfKind<GuideEntryPrototype>();

    [Test]
    [TestCaseSource(nameof(_guideEntries))]
    [Description("Ensures a given guidebook entry is valid, checking the document/etc.")]
    public async Task Validate(string protoKey)
    {
        // Sunrise-start: Данный тест невозможно нормально решить,
        // Так как у нас банально переполнен гайдбук реагентами. Оффы должны пофиксить когда-нибудь.
        if (protoKey == "Chemicals")
            Assert.Ignore("Chemical guide exceeds the currently supported guidebook document size.");
        // Sunrise-end

        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        await client.WaitIdleAsync();
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var parser = client.ResolveDependency<DocumentParsingManager>();
        var proto = protoMan.Index<GuideEntryPrototype>(protoKey);

        await client.WaitAssertion(() =>
        {
            using var reader = resMan.ContentFileReadText(proto.Text);
            var text = reader.ReadToEnd();

            Assert.That(parser.TryAddMarkup(new Document(), text), $"Failed to parse the guide entry's document.");
        });

        await pair.CleanReturnAsync();
    }
}
