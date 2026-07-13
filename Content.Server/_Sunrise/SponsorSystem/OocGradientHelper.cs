// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server._Sunrise.PlayerCache;
using Content.Shared._Sunrise.SponsorSystem;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Sunrise.Interfaces.Shared;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server._Sunrise.SponsorSystem;

/// <summary>
/// Серверный хелпер для формирования градиентных OOC имен и титулов спонсоров.
/// </summary>
public static class ServerOocGradientHelper
{
    /// <summary>
    /// Пытается сформировать градиентное имя и титул для OOC, если у пользователя выбран и разрешен градиент.
    /// </summary>
    public static bool TryFormatGradientName(
        NetUserId userId,
        string username,
        ISharedSponsorsManager? sponsorsManager,
        IPlayerManager? playerManager,
        INetConfigurationManager? netConfig,
        PlayerCacheManager? playerCacheManager,
        [NotNullWhen(true)] out string? formattedName)
    {
        formattedName = null;
        if (sponsorsManager == null)
            return false;

        string? selectedColor = null;
        if (playerManager != null && netConfig != null && playerManager.TryGetSessionById(userId, out var session))
        {
            selectedColor = netConfig.GetClientCVar(session.Channel, SunriseCCVars.SponsorOocColor);
        }

        if (string.IsNullOrEmpty(selectedColor) && playerCacheManager != null && playerCacheManager.TryGetOocColor(userId, out var cachedColor))
        {
            selectedColor = cachedColor;
        }

        if (string.IsNullOrEmpty(selectedColor) || !OocGradientHelper.IsGradientId(selectedColor))
            return false;

        if (!sponsorsManager.TryGetAllowedOocGradients(userId, out var allowedGradients) || !allowedGradients.Contains(selectedColor))
            return false;

        sponsorsManager.TryGetOocTitle(userId, out var oocTitle);
        if (string.IsNullOrEmpty(oocTitle) && playerCacheManager != null)
        {
            playerCacheManager.TryGetOocTitle(userId, out oocTitle);
        }

        var gradTitle = string.IsNullOrEmpty(oocTitle) ? "" : $"[bold]\\[{OocGradientHelper.ApplyGradientById(oocTitle, selectedColor)}\\][/bold] ";
        var gradName = OocGradientHelper.ApplyGradientById(username, selectedColor);
        formattedName = $"{gradTitle}{gradName}";
        return true;
    }
}
