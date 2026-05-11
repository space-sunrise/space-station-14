using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.Clothing.Dirt;

public abstract class SharedClothingDirtSystem : EntitySystem
{
    [Dependency] protected readonly InventorySystem Inventory = default!;

    // только обувь при обычной ходьбе по луже
    public static readonly string[] WalkSlots = { "shoes" };

    // всё что на теле при падении или долгом лежании
    public static readonly string[] BodySlots = { "outerClothing", "jumpsuit", "shoes", "gloves", "head" };

    public void ApplyDirt(EntityUid item, Color color, float amount)
    {
        if (amount <= 0f)
            return;

        var dirt = EnsureComp<ClothingDirtComponent>(item);

        var existing = dirt.Layers.FirstOrDefault(l => ColorClose(l.Color, color));
        if (existing != null)
            existing.Intensity = Math.Clamp(existing.Intensity + amount, 0f, 100f);
        else
            dirt.Layers.Add(new DirtLayer { Color = color, Intensity = Math.Clamp(amount, 0f, 100f) });

        Recalc(dirt);
        Dirty(item, dirt);
    }

    public void CleanDirt(EntityUid item, float amount = 100f)
    {
        if (!TryComp<ClothingDirtComponent>(item, out var dirt))
            return;

        foreach (var layer in dirt.Layers)
            layer.Intensity = Math.Max(0f, layer.Intensity - amount);

        dirt.Layers.RemoveAll(l => l.Intensity <= 0f);
        Recalc(dirt);
        Dirty(item, dirt);
    }

    // пачкает все одетые предметы в указанных слотах
    public void DirtySlots(EntityUid mob, string[] slots, Color color, float amount)
    {
        foreach (var slot in slots)
        {
            if (Inventory.TryGetSlotEntity(mob, slot, out var item) && item.HasValue)
                ApplyDirt(item.Value, color, amount);
        }
    }

    private void Recalc(ClothingDirtComponent dirt)
    {
        if (dirt.Layers.Count == 0)
        {
            dirt.DirtLevel = 0f;
            dirt.DirtColor = Color.Transparent;
            return;
        }

        dirt.DirtLevel = Math.Min(dirt.Layers.Sum(l => l.Intensity), 100f);
        dirt.DirtColor = MixColors(dirt.Layers);
    }

    // взвешенное смешение по интенсивности слоёв
    private static Color MixColors(List<DirtLayer> layers)
    {
        var total = layers.Sum(l => l.Intensity);
        if (total <= 0f)
            return Color.Transparent;

        float r = 0f, g = 0f, b = 0f;
        foreach (var l in layers)
        {
            var w = l.Intensity / total;
            r += l.Color.R * w;
            g += l.Color.G * w;
            b += l.Color.B * w;
        }
        return new Color(r, g, b);
    }

    // считаем цвета одинаковыми если разница по каждому каналу < 0.15
    private static bool ColorClose(Color a, Color b)
        => Math.Abs(a.R - b.R) < 0.15f
        && Math.Abs(a.G - b.G) < 0.15f
        && Math.Abs(a.B - b.B) < 0.15f;
}
