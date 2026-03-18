using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface.Controls;


namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _updatingHairMirroring;

    private void UpdateHairControlsSunrise()
    {
        UpdateHairMirroringControls();
        UpdateCMarkingsHair();
        UpdateCMarkingsFacialHair();
    }

    private void OnHairStyleExtendedColorChangedSunrise((int slot, Marking marking) newColor)
    {
        if (Profile is null || newColor.marking.MarkingEffects.Count == 0)
            return;

        Profile = Profile.WithCharacterAppearance(
            Profile.Appearance.WithHairExtendedColor(newColor.marking.MarkingEffects[0]));
        UpdateCMarkingsHair();
        ReloadPreview();
    }

    private void OnFacialHairStyleExtendedColorChangedSunrise((int slot, Marking marking) newColor)
    {
        if (Profile is null || newColor.marking.MarkingEffects.Count == 0)
            return;

        Profile = Profile.WithCharacterAppearance(
            Profile.Appearance.WithFacialHairExtendedColor(newColor.marking.MarkingEffects[0]));
        UpdateCMarkingsFacialHair();
        ReloadPreview();
    }

    private void OnHairStyleColorChangedSunrise((int slot, Marking marking) newColor)
    {
        if (Profile is null || newColor.marking.MarkingColors.Count == 0)
            return;

        var newExtended = newColor.marking.MarkingEffects[0].Clone();
        Profile = Profile.WithCharacterAppearance(
            Profile.Appearance.WithHairColor(newColor.marking.MarkingColors[0], newExtended));
        UpdateCMarkingsHair();
        ReloadPreview();
    }

    private void OnFacialHairStyleColorChangedSunrise((int slot, Marking marking) newColor)
    {
        if (Profile is null || newColor.marking.MarkingColors.Count == 0)
            return;

        var newExtended = newColor.marking.MarkingEffects[0].Clone();
        Profile = Profile.WithCharacterAppearance(
            Profile.Appearance.WithFacialHairColor(newColor.marking.MarkingColors[0], newExtended));
        UpdateCMarkingsFacialHair();
        ReloadPreview();
    }

    private void OnHairMirroringToggledSunrise(BaseButton.ButtonToggledEventArgs args)
    {
        if (Profile is null || _updatingHairMirroring)
            return;

        Profile = Profile.WithCharacterAppearance(
            Profile.Appearance.WithHairMirroring(args.Pressed));
        ReloadPreview();
    }

    private void UpdateHairMirroringControls()
    {
        if (Profile == null)
            return;

        _updatingHairMirroring = true;
        HairMirroring.Pressed = Profile.Appearance.HairMirrored;
        _updatingHairMirroring = false;
    }
}
