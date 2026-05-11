using Content.Shared.Examine;

namespace Content.Shared.Clothing.Dirt;

/// <summary>
/// Общая (Shared) часть системы загрязнения одежды.
/// Содержит examine-логику и утилитарные методы изменения загрязнения.
/// </summary>
public abstract class SharedClothingDirtSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingDirtComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, ClothingDirtComponent component, ExaminedEvent args)
    {
        if (component.DirtLevel <= 0f)
            return;

        var percent = (int)(component.DirtLevel * 100f);

        string description;
        if (percent < 10)
            description = Loc.GetString("clothing-dirt-examine-slight");
        else if (percent < 30)
            description = Loc.GetString("clothing-dirt-examine-light");
        else if (percent < 60)
            description = Loc.GetString("clothing-dirt-examine-moderate");
        else if (percent < 85)
            description = Loc.GetString("clothing-dirt-examine-heavy");
        else
            description = Loc.GetString("clothing-dirt-examine-soaked");

        args.PushMarkup(Loc.GetString("clothing-dirt-examine-message",
            ("percent", percent),
            ("description", description)));
    }

    /// <summary>
    /// Добавляет загрязнение на предмет одежды.
    /// Если уже есть другой цвет — смешивает пропорционально количеству.
    /// </summary>
    public void AddDirt(EntityUid uid, ClothingDirtComponent component, float amount, Color color)
    {
        var oldLevel = component.DirtLevel;
        component.DirtLevel = MathF.Min(component.DirtLevel + amount, 1.0f);

        // Смешиваем цвета пропорционально: больший вклад = доминирующий цвет
        if (oldLevel > 0.01f && component.DirtColor != color)
        {
            var blendFactor = amount / component.DirtLevel;
            component.DirtColor = Color.InterpolateBetween(component.DirtColor, color,
                MathF.Min(blendFactor, 0.7f));
        }
        else
        {
            component.DirtColor = color;
        }

        Dirty(uid, component);
    }

    /// <summary>
    /// Уменьшает уровень загрязнения (очистка).
    /// </summary>
    public void ReduceDirt(EntityUid uid, ClothingDirtComponent component, float amount)
    {
        component.DirtLevel = MathF.Max(component.DirtLevel - amount, 0f);
        if (component.DirtLevel <= 0f)
            component.DirtColor = Color.FromHex("#5C3D1E");
        Dirty(uid, component);
    }

    /// <summary>
    /// Полная очистка предмета.
    /// </summary>
    public void CleanClothing(EntityUid uid, ClothingDirtComponent component)
    {
        component.DirtLevel = 0f;
        component.DirtColor = Color.FromHex("#5C3D1E");
        Dirty(uid, component);
    }
}
