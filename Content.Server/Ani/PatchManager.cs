using System.Linq;
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

        try
        {
            // Применяем патчи только к типам из текущей сборки
            var types = assembly.GetTypes();
            var patchedCount = 0;
            var failedCount = 0;

            // Логируем все типы с атрибутом HarmonyPatch для диагностики
            var allPatchTypes = new List<Type>();
            foreach (var type in types)
            {
                if (type.Assembly == assembly)
                {
                    var hasHarmonyPatch = type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0;
                    if (hasHarmonyPatch)
                    {
                        allPatchTypes.Add(type);
                    }
                }
            }
            sawmill.Info($"Found {allPatchTypes.Count} patch types: {string.Join(", ", allPatchTypes.Select(t => t.FullName))}");

            foreach (var type in types)
            {
                try
                {
                    // Проверяем, что тип из текущей сборки и имеет атрибуты Harmony
                    if (type.Assembly != assembly)
                        continue;

                    // Проверяем, есть ли атрибут HarmonyPatch
                    var hasHarmonyPatch = type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0;
                    if (!hasHarmonyPatch)
                        continue;

                    sawmill.Info($"Applying patch to type: {type.FullName}");

                    // Применяем патчи к типу
                    var processor = harmony.CreateClassProcessor(type);
                    var patchInfo = processor.Patch();

                    if (patchInfo != null)
                    {
                        sawmill.Info($"Successfully patched type: {type.FullName}");
                    }
                    else
                    {
                        sawmill.Warning($"Patch returned null for type: {type.FullName}");
                    }

                    patchedCount++;
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но продолжаем применять патчи к другим типам
                    sawmill.Warning($"Failed to patch type {type.FullName}: {ex.GetType().Name}: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        sawmill.Warning($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    }
                    sawmill.Warning($"Stack trace: {ex.StackTrace}");
                    failedCount++;
                }
            }

            sawmill.Info($"Patches applied: {patchedCount} successful, {failedCount} failed");
        }
        catch (Exception ex)
        {
            sawmill.Error($"Critical error during patching: {ex}");
            throw;
        }

        PatchApplied = true;
    }
}
