---
name: SS14 Graphics Overlays
description: Глубокий практический гайд по overlay-архитектуре SS14: OverlaySpace, lifecycle, связь с шейдерами, ScreenTexture, render targets, stencil-композиция, графические примитивы и оптимизация рендера.
---

# Оверлеи и графический пайплайн SS14

Этот skill покрывает только overlays: их lifecycle, рендер-проходы, кэширование ресурсов, stencil и примитивы 🙂
Подробности языка шейдеров и built-in функций смотри в отдельном shader-skill.

## Как overlays реально рендерятся

1. Менеджер хранит один экземпляр overlay на тип и сортирует их по `ZIndex`.
2. Для каждого overlay вызывается `BeforeDraw(...)`.
3. Если `RequestScreenTexture == true`, движок копирует текущий framebuffer в `ScreenTexture` (дорого).
4. Если `OverwriteTargetFrameBuffer == true`, целевой буфер очищается до рисования.
5. Вызывается `Draw(...)` в соответствующем пространстве (`ScreenSpace`/`WorldSpace` и др.).

Вывод:
- Логику раннего выхода держи в `BeforeDraw`, чтобы не триггерить лишнее копирование экрана.
- `Draw` должен быть максимально коротким и детерминированным.

## Выбор OverlaySpace

- `ScreenSpace`: UI-подобные эффекты поверх мира.
- `ScreenSpaceBelowWorld`: UI-эффекты под миром.
- `WorldSpace`: эффекты над миром, часто fullscreen по `WorldBounds`.
- `WorldSpaceBelowFOV`: эффекты под финальным FOV.
- `WorldSpaceEntities`: эффекты в том же слое, что сущности.
- `BeforeLighting`: спец-проходы перед применением буфера света.

## Базовый шаблон shader-overlay

```csharp
public sealed class ExampleOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        // Ранний выход: не рендерим эффект без нужного состояния.
        return _effectStrength > 0f && args.Viewport.Eye == _playerEye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("Strength", _effectStrength);

        args.WorldHandle.UseShader(_shader);
        args.WorldHandle.DrawRect(args.WorldBounds, Color.White); // fullscreen-проход
        args.WorldHandle.UseShader(null); // обязательно сбрасывай shader-state
    }
}
```

## Multi-pass через render target (blur/compose)

```csharp
protected override bool BeforeDraw(in OverlayDrawArgs args)
{
    var size = (Vector2i) (args.Viewport.Size * _downscale);
    var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

    // Создаем/пересоздаем RT только при изменении размера viewport.
    if (res.PassA == null || res.PassA.Size != size)
    {
        res.PassA?.Dispose();
        res.PassB?.Dispose();
        res.PassA = _clyde.CreateRenderTarget(size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb));
        res.PassB = _clyde.CreateRenderTarget(size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb));
    }

    return true;
}

protected override void Draw(in OverlayDrawArgs args)
{
    if (ScreenTexture == null)
        return;

    var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());
    var bounds = new Box2(Vector2.Zero, res.PassA!.Size);

    // Pass 1
    args.WorldHandle.RenderInRenderTarget(res.PassA, () =>
    {
        _blurX.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        args.WorldHandle.UseShader(_blurX);
        args.WorldHandle.DrawRect(bounds, Color.White);
    }, Color.Transparent);

    // Pass 2
    args.WorldHandle.RenderInRenderTarget(res.PassB!, () =>
    {
        _blurY.SetParameter("SCREEN_TEXTURE", res.PassA.Texture);
        args.WorldHandle.UseShader(_blurY);
        args.WorldHandle.DrawRect(bounds, Color.White);
    }, Color.Transparent);

    // Composite
    _final.SetParameter("SCREEN_TEXTURE", ScreenTexture);
    _final.SetParameter("BLURRED_TEXTURE", res.PassB.Texture);
    args.WorldHandle.UseShader(_final);
    args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
    args.WorldHandle.UseShader(null);
}
```

## Stencil-композиция: маска + отрисовка

```csharp
// 1) Записываем маску в промежуточный RT.
handle.RenderInRenderTarget(stencilRt, () =>
{
    handle.SetTransform(localMatrix);
    handle.DrawRect(maskRegion, Color.White); // область, где эффект должен действовать
}, Color.Transparent);

// 2) Применяем stencil-mask shader.
handle.UseShader(_proto.Index(stencilMaskShader).Instance());
handle.DrawTextureRect(stencilRt.Texture, worldBounds);

// 3) Рисуем финальный слой только в разрешенной stencil-области.
handle.UseShader(_proto.Index(stencilDrawShader).Instance());
handle.DrawTextureRect(effectRt.Texture, worldBounds);
handle.UseShader(null);
```

## Примитивы для визуализации и тактики

```csharp
// Линия направления.
args.WorldHandle.DrawLine(start, end, Color.Aqua);

// Круг в точке назначения.
args.WorldHandle.DrawCircle(end, 0.4f, Color.Red, false);
```

Используй это для:
- тактических индикаторов;
- отладочных векторов;
- направляющих/границ зон.

## Паттерны 😎

- Проверяй `args.Viewport.Eye` против глаза локального игрока перед дорогим рендером.
- Держи дорогое условие в `BeforeDraw`, а не в середине `Draw`.
- Кэшируй per-viewport ресурсы через `OverlayResourceCache<T>`.
- Пересоздавай RT только на resize или смену формата.
- Всегда сбрасывай графическое состояние: `UseShader(null)` и при необходимости `SetTransform(Matrix3x2.Identity)`.
- Управляй порядком эффектов через `ZIndex`, не через «случайный» порядок регистрации.

## Анти-паттерны

- Запрашивать `ScreenTexture`, если шейдер его не читает.
- Создавать render target каждый кадр без кэша.
- Оставлять shader/transform активными после `Draw` и ломать последующие overlay-проходы.
- Опираться на одинаковый `ZIndex` для строгого порядка наложения.
- Использовать `WorldAABB` для fullscreen-эффекта там, где нужен корректный вращаемый `WorldBounds` ⚠️

## Мини-чеклист перед PR

- У overlay есть четкий `OverlaySpace`.
- Дорогие проверки вынесены в `BeforeDraw`.
- Все временные RT освобождаются через `Dispose`.
- Состояние рендера после `Draw` возвращается в нейтральное.
- Поведение протестировано на нескольких значениях масштаба/зума viewport.
