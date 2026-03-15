using Content.Server.Humanoid.Components;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Humanoid.Markings; // Sunrise-Edit

namespace Content.Server.Humanoid.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        // If we have an initial profile/base layer set, do not randomize this humanoid.
        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return;

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);

        _visualBody.ApplyProfileTo(uid, profile);
        _humanoidProfile.ApplyProfileTo(uid, profile);

        if (component.RandomizeName)
            _metaData.SetEntityName(uid, profile.Name);

        // Sunrise-Start
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
        {
            if (component.SkinColor != null)
                _humanoid.SetSkinColor(uid, component.SkinColor.Value, humanoid: humanoidAppearance);
            
            if (component.HairColor != null)
                SetMarkingColor(uid, MarkingCategories.Hair, component.HairColor.Value, humanoidAppearance);
            
            if (component.FacialHairColor != null) // Да, цвет бороды, йоу.
                SetMarkingColor(uid, MarkingCategories.FacialHair, component.FacialHairColor.Value, humanoidAppearance);
        }
        // Sunrise-End
    }

    // Sunrise-Start
    private void SetMarkingColor(EntityUid uid, MarkingCategories category, Color color, 
        HumanoidAppearanceComponent humanoid)
    {
        if (!humanoid.MarkingSet.Markings.TryGetValue(category, out var markings))
            return;

        foreach (var marking in markings)
        {
            for (var i = 0; i < marking.MarkingColors.Count; i++)
            {
                marking.SetColor(i, color);
            }
        }
        
        Dirty(uid, humanoid);
    }
    // Sunrise-End
}