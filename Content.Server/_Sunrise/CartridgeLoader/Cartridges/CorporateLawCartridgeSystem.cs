using Content.Server.CartridgeLoader;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared._Sunrise.Laws;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CartridgeLoader.Cartridges;

public sealed class CorporateLawCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = null!;
    [Dependency] private readonly IPrototypeManager _prototype = null!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CorporateLawCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(EntityUid uid, CorporateLawCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        var lawsetId = _config.GetCVar(SunriseCCVars.CorporateLawSet);
        if (!_prototype.TryIndex<CorporateLawsetPrototype>(lawsetId, out var lawset))
            return;

        var sections = new List<LawSection>();

        foreach (var sectionId in lawset.Sections)
        {
            if (!_prototype.TryIndex<CorporateLawSectionPrototype>(sectionId, out var section))
                continue;

            var entries = new List<LawEntry>();
            foreach (var entryId in section.Entries)
            {
                if (!_prototype.TryIndex<CorporateLawPrototype>(entryId, out var entry))
                    continue;

                entries.Add(new LawEntry
                {
                    Identifier = entry.LawIdentifier,
                    Title = Loc.GetString(entry.Title),
                    Description = Loc.GetString(entry.Description)
                });
            }

            sections.Add(new LawSection
            {
                Title = Loc.GetString(section.Title),
                Color = section.Color,
                Entries = entries
            });
        }

        var state = new CorporateLawUiState(sections);
        _cartridgeLoader.UpdateCartridgeUiState(args.Loader, state);
    }
}
