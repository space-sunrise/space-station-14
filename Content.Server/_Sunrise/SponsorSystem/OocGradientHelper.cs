// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server._Sunrise.PlayerCache;
using Content.Server.Administration.Managers;
using Content.Shared._Sunrise.SponsorSystem;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Sunrise.Interfaces.Shared;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Shared.Administration;

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
        ISharedPlayerManager? playerManager,
        INetConfigurationManager? netConfig,
        PlayerCacheManager? playerCacheManager,
        [NotNullWhen(true)] out string? formattedName)
    {
        formattedName = null;
        if (sponsorsManager == null)
            return false;

        ICommonSession? session = null;
        if (playerManager != null)
        {
            playerManager.TryGetSessionById(userId, out session);
        }

        string? selectedColor = null;
        if (session != null && netConfig != null)
        {
            selectedColor = netConfig.GetClientCVar(session.Channel, SunriseCCVars.SponsorOocColor);
        }

        if (string.IsNullOrEmpty(selectedColor) && playerCacheManager != null && playerCacheManager.TryGetOocColor(userId, out var cachedColor))
        {
            selectedColor = cachedColor;
        }

        if (string.IsNullOrEmpty(selectedColor) || !OocGradientHelper.IsGradientId(selectedColor))
            return false;

        var isAllowedGradient = false;
        var isSponsor = sponsorsManager.IsSponsor(userId);
        var adminManager = IoCManager.Resolve<IAdminManager>();
        var isAllowedAdminBypass = false;
        AdminData? adminData = null;
        
        if (session != null)
        {
            isAllowedAdminBypass = isSponsor && adminManager.IsAdmin(session, includeDeAdmin: false);
            if (isAllowedAdminBypass)
            {
                adminData = adminManager.GetAdminData(session, includeDeAdmin: false);
            }
        }

        if (sponsorsManager.TryGetAllowedOocGradients(userId, out var allowedGradients) && allowedGradients.Contains(selectedColor))
        {
            isAllowedGradient = true;
        }
        else if (isAllowedAdminBypass)
        {
            isAllowedGradient = true;
        }

        if (!isAllowedGradient)
            return false;

        string? oocTitle = null;
        if (session != null && netConfig != null)
        {
            var selectedTitle = netConfig.GetClientCVar(session.Channel, SunriseCCVars.SponsorOocTitle);
            if (!string.IsNullOrEmpty(selectedTitle) && selectedTitle != "@none")
            {
                var isAllowedTitle = false;
                if (sponsorsManager.TryGetPrototypes(userId, out var prototypes) && prototypes.Contains(selectedTitle))
                {
                    isAllowedTitle = true;
                }
                else if (isAllowedAdminBypass)
                {
                    if (adminData != null && (selectedTitle == adminData.Title || OocGradientHelper.TryResolveTitle(selectedTitle, out _)))
                    {
                        isAllowedTitle = true;
                    }
                }

                if (isAllowedTitle)
                {
                    if (OocGradientHelper.TryResolveTitle(selectedTitle, out var resolvedTitle))
                        oocTitle = resolvedTitle;
                    else
                        oocTitle = selectedTitle;
                }
            }
        }

        if (oocTitle == null)
        {
            sponsorsManager.TryGetOocTitle(userId, out oocTitle);
            if (string.IsNullOrEmpty(oocTitle) && playerCacheManager != null)
            {
                playerCacheManager.TryGetOocTitle(userId, out oocTitle);
            }
        }

        if ((string.IsNullOrEmpty(oocTitle) || oocTitle == "@none") && isAllowedAdminBypass && adminData != null)
        {
            oocTitle = adminData.Title;
        }

        var gradTitle = string.IsNullOrEmpty(oocTitle) ? "" : $"[bold]\\[{OocGradientHelper.ApplyGradientById(oocTitle, selectedColor)}\\][/bold] ";
        var gradName = OocGradientHelper.ApplyGradientById(username, selectedColor);
        formattedName = $"{gradTitle}{gradName}";
        return true;
    }
}
