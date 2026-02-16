---
name: SS14 Graphics Shaders
description: Глубокий практический гайд по шейдерам SS14 и SWSL: синтаксис, presets, built-in переменные/функции, параметры, отладка и архитектурные решения. Используй при задачах про shader prototype, uniform, light_mode/blend_mode, stencil, совместимость и GPU-эффекты.
---

# Шейдеры SS14 (SWSL)

Этот skill покрывает только шейдерную часть: язык, рантайм, встроенные функции, параметры и отладку 🙂
Если задача про lifecycle оверлеев, `OverlaySpace`, render target и примитивы, используй отдельный skill про overlays.

## Короткое дерево решений

1. Нужен только режим смешивания/освещения/stencil без кастомной математики?
- Да: выбирай `kind: canvas`.
- Нет: выбирай `kind: source`.

2. Нужна кастомная вершина (деформация, отдельные varyings)?
- Да: добавляй `vertex()`.
- Нет: достаточно `fragment()`.

3. Нужен полный контроль над преобразованием вершин и без авто-света?
- Да: `preset raw`.
- Нет: `preset default`.

## Модель исполнения

1. Прототип шейдера определяет `kind` и параметры по умолчанию.
2. SWSL парсится в `uniform`/`varying`/`const`/функции + include-зависимости.
3. Движок оборачивает код в `default` или `raw`-шаблон и добавляет общую библиотеку функций.
4. Во время рендера `ShaderInstance` получает значения параметров из C# и применяется к draw-call.

## Язык SWSL: что поддержано

- Директивы верхнего уровня: `light_mode`, `blend_mode`, `preset`.
- Объявления: `uniform`, `varying`, `const`.
- Функции: обычные helper-функции + специальные entrypoint-функции `vertex()` и `fragment()`.
- Параметры функций: поддержаны `in`, `out`, `inout`.
- Препроцессор: `#include`, `#ifdef`, `#ifndef`, `#else`, `#endif`.

Критично:
- Для числовых типов (`float`, `int`, `vec*`, `mat*`) ставь qualifier (`lowp`/`mediump`/`highp`), иначе получишь нестабильность между GPU.
- Массивы поддержаны не для всех типов. Безопасная зона: `float[]`, `vec2[]`, `vec4[]`, `bool[]`.

## Built-in переменные и функции

Доступные общие uniforms:
- `TIME`
- `SCREEN_PIXEL_SIZE`
- `TEXTURE`
- `TEXTURE_PIXEL_SIZE`
- `projectionMatrix`
- `viewMatrix`

Часто используемые функции:
- `zTexture(uv)` и `zTextureSpec(tex, uv)` для корректного сэмплинга.
- `zAdjustResult(col)` для корректного финального вывода цвета.
- `zFromSrgb(col)` / `zToSrgb(col)` для преобразований цветового пространства.
- `zGrayscale(...)`, `zGrayscale_BT709(...)`, `zGrayscale_BT601(...)`.
- `zRandom(...)`, `zNoise(...)`, `zFBM(...)`.
- `zCircleGradient(...)`.
- `zClydeShadowDepthPack(...)` / `zClydeShadowDepthUnpack(...)`.

Дополнительно в `preset raw`:
- `apply_mvp(vertex)` и `pixel_snap(vertex)` для вершинной стадии.

## Пример A: базовый fullscreen fragment

```glsl
uniform sampler2D SCREEN_TEXTURE;

void fragment() {
    // Берем текущий экран с учетом движковых правил выборки.
    highp vec4 src = zTextureSpec(SCREEN_TEXTURE, UV);

    // Переводим картинку в grayscale через движковую функцию.
    highp float gray = zGrayscale(src.rgb);

    // Возвращаем результат с исходной альфой.
    COLOR = vec4(vec3(gray), src.a);
}
```

## Пример B: вершинная деформация через varying

```glsl
uniform sampler2D displacementMap;
uniform highp float displacementSize;
uniform highp vec4 displacementUV;

varying highp vec2 displacementUVOut;

void vertex() {
    // Передаем UV для карты смещения из вершины во фрагмент.
    displacementUVOut = mix(displacementUV.xy, displacementUV.zw, tCoord2);
}

void fragment() {
    // Читаем карту смещения и сдвигаем сэмплирование основной текстуры.
    highp vec4 disp = texture2D(displacementMap, displacementUVOut);
    highp vec2 offset = (disp.xy - vec2(128.0 / 255.0)) / (1.0 - 128.0 / 255.0);
    COLOR = zTexture(UV + offset * TEXTURE_PIXEL_SIZE * displacementSize * vec2(1.0, -1.0));
    COLOR.a *= disp.a; // Альфа displacement-карты работает как маска.
}
```

## Пример C: корректная работа с ShaderInstance в C#

```csharp
// Берем уникальный инстанс, чтобы безопасно менять uniform-ы.
var shader = prototype.Index(shaderId).InstanceUnique();

// Параметры эффекта задаем каждый кадр/тик по необходимости.
shader.SetParameter("Strength", strength);
shader.SetParameter("SCREEN_TEXTURE", screenTexture);

handle.UseShader(shader);
handle.DrawRect(bounds, Color.White); // Реальный draw-call, куда применяется шейдер.
handle.UseShader(null);               // Явный сброс состояния.
```

## Паттерны 🙂

- Используй `InstanceUnique()` для mutable-параметров.
- Делай тяжёлые параметры (`strength`, `phase`, `count`) внешними uniform-ами, а не зашивай в `const`.
- Для шумов/итераций используй более низкую точность там, где визуально допустимо.
- Делай ранний выход по альфе/маске, если пиксель не должен рендериться.
- Используй движковые helper-функции (`zTexture*`, `zGrayscale`, `zCircleGradient`) вместо повторного изобретения.
- Для итеративной отладки используй перезагрузку шейдеров без рестарта клиента.

## Анти-паттерны

- Менять параметры у `Instance()` (shared immutable) и получать рантайм-исключения.
- Полагаться на устаревшие doc-имена переменных вместо фактического поведения текущих шаблонов.
- Ожидать, что любой тип поддерживает массивы uniform-ов.
- Писать шейдер без precision qualifier-ов и считать, что он «везде одинаковый».
- Слепо переносить старые примеры без проверки даты и комментариев о проблемах ⚠️

## Мини-чеклист перед применением

- Выбран корректный `kind` (`canvas`/`source`).
- Установлен правильный `preset` (`default`/`raw`) под задачу.
- Все важные numeric-типы имеют precision qualifier.
- Параметры, меняющиеся в рантайме, поданы через `SetParameter`.
- Есть fallback-поведение для слабых/проблемных графических конфигураций.
