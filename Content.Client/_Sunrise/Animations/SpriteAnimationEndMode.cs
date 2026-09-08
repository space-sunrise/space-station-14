namespace Content.Client._Sunrise.Animations;

public enum SpriteAnimationEndMode : byte
{
    // после конца анимации всё возвращается на круги своя
    Release,
    // последний вклад анимации сохранится, сброс происходит только при Stop или выхода с pvs
    Hold,
}
