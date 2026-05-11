using Content.Shared.Clothing.Dirt;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Standing;

namespace Content.Server.Clothing.Dirt;

/// <summary>
/// Серверная система загрязнения одежды.
///
/// Триггеры загрязнения:
/// 1. Падение в лужу (KnockedDown + контакт с PuddleComponent) → вся одежда, коричневый
/// 2. Ходьба по луже → только обувь, коричневый
/// 3. Пулевой/физический урон → куртка + комбинезон, цвет крови расы
///
/// Цвет крови по расам:
/// Human       → #AA0000 (красный)
/// Vulpkanin   → #DDAA00 (жёлтый)
/// Lizard      → #228B22 (зелёный)
/// Arachnid    → #0055AA (синий)
/// Moth        → #CC44CC (фиолетовый)
/// Diona       → #44CC44 (светло-зелёный)
/// Slime       → #00CCCC (бирюзовый)
/// Vox         → #336600 (тёмно-зелёный)
/// Dwarf       → #AA0000 (красный, как человек)
/// </summary>
public sealed class ClothingDirtServerSystem : SharedClothingDirtSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private static readonly Dictionary<string, Color> RaceBloodColors = new()
    {
        { "Human",     Color.FromHex("#AA0000") },
        { "Vulpkanin", Color.FromHex("#DDAA00") },
        { "Lizard",    Color.FromHex("#228B22") },
        { "Arachnid",  Color.FromHex("#0055AA") },
        { "Moth",      Color.FromHex("#CC44CC") },
        { "Diona",     Color.FromHex("#44CC44") },
        { "Slime",     Color.FromHex("#00CCCC") },
        { "Vox",       Color.FromHex("#336600") },
        { "Dwarf",     Color.FromHex("#AA0000") },
    };

    private static readonly Color MudColor = Color.FromHex("#5C3D1E");

    private static readonly string[] AllClothingSlots =
        { "jumpsuit", "outerClothing", "shoes", "gloves", "head", "mask" };
    private static readonly string[] ShoeSlots = { "shoes" };
    private static readonly string[] BulletDamageSlots = { "outerClothing", "jumpsuit" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingDirtReceiverComponent, ComponentInit>(OnReceiverInit);
        SubscribeLocalEvent<ClothingDirtReceiverComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ClothingDirtReceiverComponent, SteppedOnPuddleEvent>(OnSteppedOnPuddle);
    }

    private void OnReceiverInit(EntityUid uid, ClothingDirtReceiverComponent component, ComponentInit args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        var speciesId = humanoid.Species.Id;
        if (RaceBloodColors.TryGetValue(speciesId, out var color))
            component.BloodColor = color;

        Dirty(uid, component);
    }

    private void OnDamageChanged(EntityUid uid, ClothingDirtReceiverComponent receiver, DamageChangedEvent args)
    {
        if (args.DamageDelta == null || !args.DamageIncreased)
            return;

        // Реагируем на колющий (пули) или сильный тупой урон
        var hasBulletDamage = false;
        if (args.DamageDelta.DamageDict.TryGetValue("Piercing", out var piercing) && piercing > 0)
            hasBulletDamage = true;
        if (args.DamageDelta.DamageDict.TryGetValue("Blunt", out var blunt) && blunt > 8)
            hasBulletDamage = true;

        if (!hasBulletDamage)
            return;

        DirtySlots(uid, receiver.BloodColor, BulletDamageSlots, 0.20f, bloodOnly: true);
    }

    private void OnSteppedOnPuddle(EntityUid uid, ClothingDirtReceiverComponent receiver, SteppedOnPuddleEvent args)
    {
        // Упал (нокдаун/стан) → вся одежда
        bool isFallen = HasComp<KnockedDownComponent>(uid) || HasComp<StunnedComponent>(uid);

        if (isFallen)
            DirtySlots(uid, MudColor, AllClothingSlots, 0.35f, bloodOnly: false);
        else
            DirtySlots(uid, MudColor, ShoeSlots, 0.15f, bloodOnly: false);
    }

    private void DirtySlots(EntityUid wearer, Color color, string[] slots, float amount, bool bloodOnly)
    {
        foreach (var slot in slots)
        {
            if (!_inventory.TryGetSlotEntity(wearer, slot, out var item) || item == null)
                continue;

            if (!TryComp<ClothingDirtComponent>(item, out var dirt))
                continue;

            // Кровяные пятна только на предметах с CanGetBloody = true
            if (bloodOnly && !dirt.CanGetBloody)
                continue;

            AddDirt(item.Value, dirt, amount, color);
        }
    }
}
