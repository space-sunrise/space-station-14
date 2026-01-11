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
public sealed class PdaAnimationVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaAnimationVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange, after: new[] { typeof(PdaVisualizerSystem) });
    }

    private void OnAppearanceChange(Entity<PdaAnimationVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(ent.Owner, PdaVisuals.IdCardInserted, out var isCardInserted, args.Component))
            return;

        var sprite = (ent.Owner, args.Sprite);

        if (isCardInserted)
        {
            ApplyAnimatedState(sprite, ent.Comp);
            return;
        }

        ApplyStaticState(sprite, ent.Comp);
    }

    /// <summary>
    /// Применяет анимированное состояние PDA с включённой анимацией.
    /// </summary>
    private void ApplyAnimatedState((EntityUid Uid, SpriteComponent Sprite) sprite, PdaAnimationVisualsComponent comp)
    {
        _spriteSystem.LayerSetRsiState((sprite.Uid, sprite.Sprite), PdaVisualLayers.Base, comp.AnimatedState);
        _spriteSystem.LayerSetAutoAnimated((sprite.Uid, sprite.Sprite), PdaVisualLayers.Base, true);
        _spriteSystem.LayerSetRsiState((sprite.Uid, sprite.Sprite), PdaVisualLayers.IdLight, comp.IdInsertedLayerState);
        _spriteSystem.LayerSetVisible((sprite.Uid, sprite.Sprite), PdaVisualLayers.IdLight, true);
    }

    /// <summary>
    /// Применяет статичное состояние PDA. Если StaticState не указан,
    /// использует первый кадр анимации с остановленной анимацией.
    /// </summary>
    private void ApplyStaticState((EntityUid Uid, SpriteComponent Sprite) sprite, PdaAnimationVisualsComponent comp)
    {
        ApplyStaticBaseState(sprite, comp);
        _spriteSystem.LayerSetVisible((sprite.Uid, sprite.Sprite), PdaVisualLayers.IdLight, false);
    }

    /// <summary>
    /// Применяет статичный base state. Если StaticState указан - использует его,
    /// иначе использует первый кадр AnimatedState с остановленной анимацией.
    /// </summary>
    private void ApplyStaticBaseState((EntityUid Uid, SpriteComponent Sprite) sprite, PdaAnimationVisualsComponent comp)
    {
        var stateName = GetStaticStateName(comp);
        _spriteSystem.LayerSetRsiState((sprite.Uid, sprite.Sprite), PdaVisualLayers.Base, stateName);
        _spriteSystem.LayerSetAutoAnimated((sprite.Uid, sprite.Sprite), PdaVisualLayers.Base, false);
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
