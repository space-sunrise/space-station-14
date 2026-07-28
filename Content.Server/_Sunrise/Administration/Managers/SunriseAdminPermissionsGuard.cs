using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Administration.Managers;

internal static class SunriseAdminPermissionsGuard
{
    public static bool IsBlocked(IAdminManager adminManager, ICommonSession? player)
    {
        return adminManager is AdminManager manager &&
               manager.IsSunriseAdminPermissionsUiBlocked(player);
    }
}
