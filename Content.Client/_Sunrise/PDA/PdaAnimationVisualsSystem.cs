using Content.Client.PDA;
using Content.Shared._Sunrise.PDA;
using Content.Shared.PDA;
using Robust.Client.GameObjects;
using PdaVisualLayers = Content.Client.PDA.PdaVisualizerSystem.PdaVisualLayers;

namespace Content.Client._Sunrise.PDA;

/// <summary>
/// Система для обработки переключения между статичным и анимированным состояниями PDA
/// в зависимости от наличия ID карты.
/// Конфигурация задаётся через компонент <see cref="PdaAnimationVisualsComponent"/> в прототипах.
/// </summary>
// VisualizerSystem автоматически подписывается на AppearanceChangeEvent и предоставляет
// готовые зависимости (AppearanceSystem, SpriteSystem)
public sealed class PdaAnimationVisualsSystem : VisualizerSystem<PdaAnimationVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PdaAnimationVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, PdaVisuals.IdCardInserted, out var isCardInserted, args.Component))
            return;

        var sprite = new Entity<SpriteComponent>(uid, args.Sprite);

        if (isCardInserted)
        {
            ApplyAnimatedState(sprite, comp);
            return;
        }

        ApplyStaticState(sprite, comp);
    }

    /// <summary>
    /// Применяет анимированное состояние PDA с включённой анимацией.
    /// </summary>
    private void ApplyAnimatedState(Entity<SpriteComponent> sprite, PdaAnimationVisualsComponent comp)
    {
        SpriteSystem.LayerSetRsiState(sprite.AsNullable(), PdaVisualLayers.Base, comp.AnimatedState);
        SpriteSystem.LayerSetAutoAnimated(sprite.AsNullable(), PdaVisualLayers.Base, true);
        SpriteSystem.LayerSetRsiState(sprite.AsNullable(), PdaVisualLayers.IdLight, comp.IdInsertedLayerState);
        SpriteSystem.LayerSetVisible(sprite.AsNullable(), PdaVisualLayers.IdLight, true);
    }

    /// <summary>
    /// Применяет статичное состояние PDA. Если StaticState не указан,
    /// использует первый кадр анимации с остановленной анимацией.
    /// </summary>
    private void ApplyStaticState(Entity<SpriteComponent> sprite, PdaAnimationVisualsComponent comp)
    {
        ApplyStaticBaseState(sprite, comp);
        SpriteSystem.LayerSetVisible(sprite.AsNullable(), PdaVisualLayers.IdLight, false);
    }

    /// <summary>
    /// Применяет статичный base state. Если StaticState указан - использует его,
    /// иначе использует первый кадр AnimatedState с остановленной анимацией.
    /// </summary>
    private void ApplyStaticBaseState(Entity<SpriteComponent> sprite, PdaAnimationVisualsComponent comp)
    {
        var stateName = GetStaticStateName(comp);
        SpriteSystem.LayerSetRsiState(sprite.AsNullable(), PdaVisualLayers.Base, stateName);
        SpriteSystem.LayerSetAutoAnimated(sprite.AsNullable(), PdaVisualLayers.Base, false);
    }

    /// <summary>
    /// Возвращает имя state для статичного отображения.
    /// Если StaticState не указан, возвращает AnimatedState для использования первого кадра.
    /// </summary>
    private static string GetStaticStateName(PdaAnimationVisualsComponent comp)
    {
        return comp.StaticState ?? comp.AnimatedState;
    }
}
