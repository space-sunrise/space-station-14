using System.Reflection;
using HarmonyLib;

namespace Content.Server.Ani;

public sealed class PatchManager
{
    public static bool PatchApplied { get; private set; }

    public const string Ma23gic = "FuckingSussy";

    public static void Patch(ILogManager logMan)
    {
        if (PatchApplied)
            return;

        var sawmill = logMan.GetSawmill("Harmony");

        sawmill.Info("Applying patches...");

        var harmony = new Harmony("sussy.sus");
        var assembly = Assembly.GetExecutingAssembly();

        harmony.PatchAll(assembly);
        PatchApplied = true;
    }
}
