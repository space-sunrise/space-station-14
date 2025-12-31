using System;
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

            foreach (var type in types)
            {
                try
                {
                    // Проверяем, что тип из текущей сборки и имеет атрибуты Harmony
                    if (type.Assembly != assembly)
                        continue;

                    // Применяем патчи к типу
                    harmony.CreateClassProcessor(type).Patch();
                    patchedCount++;
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но продолжаем применять патчи к другим типам
                    sawmill.Warning($"Failed to patch type {type.FullName}: {ex.Message}");
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
