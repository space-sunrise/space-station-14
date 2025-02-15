using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.ChatIcons;

public sealed class PointIconsSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    private const string FontSize = "10";

    private const string CautionColor = "c62828";
    private const string BaseColor = "aeabc4";

    private readonly Dictionary<PopupType, string> _fontSizeDict = new ()
    {
        { PopupType.Medium, "12" },
        { PopupType.MediumCaution, "12" },
        { PopupType.Large, "15" },
        { PopupType.LargeCaution, "15" }
    };


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaDataComponent, EntityPopupedEvent>(OnPopup);
    }

    private void OnPopup(Entity<MetaDataComponent> ent, ref EntityPopupedEvent args)
    {
        if (_playerManager.LocalEntity == null)
            return;

        if (!_examine.InRangeUnOccluded(_playerManager.LocalEntity.Value, Transform(ent).Coordinates, 10))
            return;

        var fontsize = _fontSizeDict.GetValueOrDefault(args.Type, FontSize);
        var fontcolor = args.Type is PopupType.LargeCaution or PopupType.MediumCaution or PopupType.SmallCaution
            ? CautionColor
            : BaseColor;

        var tag = Loc.GetString("ent-texture-tag-short", ("id", ent.Owner.Id));
        var wrappedMessage = $"[font size={fontsize}][color=#{fontcolor}]{args.Message + tag}[/color][/font]";

        var chatMsg = new ChatMessage(ChatChannel.Emotes,
            args.Message,
            wrappedMessage,
            NetEntity.Invalid,
            null);

        _uiManager.GetUIController<ChatUIController>().ProcessChatMessage(chatMsg);
    }
}

public sealed class EntityPopupedEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly PopupType Type;

    public EntityPopupedEvent(string message, PopupType type)
    {
        Message = message;
        Type = type;
    }
}
