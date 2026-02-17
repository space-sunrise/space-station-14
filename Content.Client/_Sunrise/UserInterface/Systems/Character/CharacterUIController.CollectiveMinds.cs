using Content.Client._Sunrise.UserInterface.Systems.Character.Controls;
using Content.Shared._Sunrise.CollectiveMind;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Character;

public sealed partial class CharacterUIController
{
    private void AddCollectiveMindsToCharacterMenu(Dictionary<CollectiveMindPrototype, CollectiveMindMemberData>? minds)
    {
        if (_window == null)
            return;

        if (minds == null || minds.Count == 0)
            return;

        var mindsControl = new CharacterMindsControl
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };

        var mindDescriptionMessage = new FormattedMessage();
        mindDescriptionMessage.AddText(Loc.GetString("character-info-collective-minds-title"));

        foreach (var mindPrototype in minds)
        {
            mindDescriptionMessage.AddText("\n");
            mindDescriptionMessage.PushColor(mindPrototype.Key.Color);
            mindDescriptionMessage.AddText($"{mindPrototype.Key.LocalizedName}: +{mindPrototype.Key.KeyCode}");
            mindDescriptionMessage.AddText(" ");
            mindDescriptionMessage.AddText(Loc.GetString("character-info-collective-minds-number", ("mindId", mindPrototype.Value.MindId)));
            mindDescriptionMessage.Pop();
        }

        mindsControl.Description.SetMessage(mindDescriptionMessage);
        _window.Objectives.AddChild(mindsControl);
    }
}

