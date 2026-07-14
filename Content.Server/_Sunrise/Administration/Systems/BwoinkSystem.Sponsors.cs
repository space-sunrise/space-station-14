using Content.Server._Sunrise.SponsorSystem;
using Content.Shared._Sunrise.Messenger;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.SponsorSystem;
using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public partial class BwoinkSystem
{
    private string FormatPlayerNameWithSponsors(AdminData? senderAdmin, ICommonSession senderSession, string adminPrefix, string name)
    {
        var escapedUsername = FormattedMessage.EscapeText(name);

        string? sponsorTitle = null;
        string? sponsorColorHex = null;
        bool isGradient = false;

        var isActiveAdmin = senderAdmin != null;
        var isSponsor = _sponsorsManager != null && _sponsorsManager.IsSponsor(senderSession.UserId);
        var isAllowedAdminBypass = isActiveAdmin && isSponsor;

        if (_sponsorsManager != null)
        {
            _sponsorsManager.TryGetOocTitle(senderSession.UserId, out sponsorTitle);
            if (_sponsorsManager.TryGetOocColor(senderSession.UserId, out var color))
                sponsorColorHex = "#" + color.Value.ToHexNoAlpha();
        }

        var selectedTitleCVar = _netConfig.GetClientCVar(senderSession.Channel, SunriseCCVars.SponsorOocTitle);
        if (!string.IsNullOrEmpty(selectedTitleCVar))
            sponsorTitle = selectedTitleCVar == "@none" ? null : selectedTitleCVar;

        var selectedColorCVar = _netConfig.GetClientCVar(senderSession.Channel, SunriseCCVars.SponsorOocColor);
        if (!string.IsNullOrEmpty(selectedColorCVar))
            sponsorColorHex = selectedColorCVar == "@none" ? null : selectedColorCVar;

        if (sponsorTitle != null && OocGradientHelper.TryResolveTitle(sponsorTitle, out var resolvedTitle))
            sponsorTitle = resolvedTitle;

        isGradient = OocGradientHelper.IsGradientId(sponsorColorHex);

        if (isActiveAdmin)
        {
            if (string.IsNullOrWhiteSpace(sponsorColorHex) && !isGradient)
            {
                if (senderAdmin!.Flags == AdminFlags.Adminhelp)
                    sponsorColorHex = "purple";
                else if (senderAdmin.HasFlag(AdminFlags.Adminhelp))
                    sponsorColorHex = "red";
                else
                    sponsorColorHex = "purple";
            }
        }

        string result;
        if (isGradient && _sponsorsManager != null && ServerOocGradientHelper.TryFormatGradientName(senderSession.UserId, name, _sponsorsManager, _playerManager, _netConfig, _playerCacheManager, out var gradFormatted))
        {
            result = gradFormatted;
        }
        else
        {
            var titlePart = !string.IsNullOrWhiteSpace(sponsorTitle)
                ? $"\\[{FormattedMessage.EscapeText(sponsorTitle)}\\] "
                : adminPrefix;

            if (!string.IsNullOrWhiteSpace(sponsorColorHex))
                result = $"[color={sponsorColorHex}]{titlePart}{escapedUsername}[/color]";
            else
                result = $"{titlePart}{escapedUsername}";
        }

        var hasEmojiRights = (_sponsorsManager != null && _sponsorsManager.IsAllowedOocTitleEmoji(senderSession.UserId)) || isAllowedAdminBypass;
        if (hasEmojiRights)
        {
            var emoji = _netConfig.GetClientCVar(senderSession.Channel, SunriseCCVars.SponsorOocEmoji);
            if (!string.IsNullOrWhiteSpace(emoji))
            {
                var emojiId = emoji.Trim(':');
                var emojiSystem = EntityManager.System<SharedEmojiSystem>();
                if (emojiSystem.IsEmojiAllowedForPlayer(emojiId, senderSession.UserId, _sponsorsManager))
                {
                    result = $"[emoji id=\"{emojiId}\" size=50] {result}";
                }
            }
        }

        return result;
    }
}
