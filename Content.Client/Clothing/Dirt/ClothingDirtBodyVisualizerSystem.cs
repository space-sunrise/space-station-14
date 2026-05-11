using Content.Client.Clothing;
using Content.Client.Humanoid;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Dirt;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client.Clothing.Dirt;

/// <summary>
/// Клиентская система отображения загрязнений одежды НЕПОСРЕДСТВЕННО НА ТЕЛЕ персонажа.
///
/// Принцип работы:
/// Одежда в SS14 на теле рендерится как отдельные слои спрайта гуманоида
/// (добавляются через ClientClothingSystem при экипировке).
/// Мы перехватываем это и добавляем поверх каждого слоя одежды
/// дополнительный слой-оверлей с цветным пятном загрязнения.
///
/// Ключ слоя оверлея = "dirt_overlay_" + имя слота (напр. "dirt_overlay_outerClothing")
/// Это позволяет независимо управлять грязью для каждого предмета одежды.
/// </summary>
public sealed class ClothingDirtBodyVisualizerSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    // Префикс ключа оверлейного слоя на спрайте персонажа
    private const string DirtOverlayPrefix = "dirt_overlay_";

    // Путь к текстуре оверлея (полупрозрачный квадрат 32x32, нарисованный вручную)
    private const string DirtOverlayTexture = "/Textures/Clothing/dirt_overlay.rsi";
    private const string DirtOverlayState = "dirt";

    // Слоты одежды которые мы отслеживаем → соответствующий визуальный слой на гуманоиде
    // Ключ = inventory slot, значение = layer key на спрайте гуманоида (из HumanoidVisualLayers)
    private static readonly Dictionary<string, string> SlotToHumanoidLayer = new()
    {
        { "jumpsuit",      "jumpsuit"      },
        { "outerClothing", "outerClothing" },
        { "shoes",         "shoes"         },
        { "gloves",        "gloves"        },
        { "head",          "head"          },
        { "mask",          "mask"          },
    };

    public override void Initialize()
    {
        base.Initialize();

        // Подписываемся на смену состояния ClothingDirtComponent (сетевая синхронизация)
        SubscribeLocalEvent<ClothingDirtComponent, ComponentHandleState>(OnDirtStateChanged);

        // Когда одежда надевается/снимается — обновляем оверлей
        SubscribeLocalEvent<ClothingDirtComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ClothingDirtComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnDirtStateChanged(EntityUid clothingUid, ClothingDirtComponent dirt, ref ComponentHandleState args)
    {
        // Находим персонажа, который носит эту одежду
        if (!TryGetWearer(clothingUid, out var wearer, out var slotName))
            return;

        UpdateBodyOverlay(wearer.Value, slotName!, clothingUid, dirt);
    }

    private void OnEquipped(EntityUid clothingUid, ClothingDirtComponent dirt, GotEquippedEvent args)
    {
        if (!SlotToHumanoidLayer.ContainsKey(args.Slot))
            return;

        UpdateBodyOverlay(args.Equipee, args.Slot, clothingUid, dirt);
    }

    private void OnUnequipped(EntityUid clothingUid, ClothingDirtComponent dirt, GotUnequippedEvent args)
    {
        if (!SlotToHumanoidLayer.ContainsKey(args.Slot))
            return;

        RemoveBodyOverlay(args.Equipee, args.Slot);
    }

    /// <summary>
    /// Обновляет (или создаёт) оверлейный слой грязи на спрайте персонажа.
    /// </summary>
    private void UpdateBodyOverlay(EntityUid wearer, string slot, EntityUid clothingUid, ClothingDirtComponent dirt)
    {
        if (!TryComp<SpriteComponent>(wearer, out var wearerSprite))
            return;

        var overlayKey = DirtOverlayPrefix + slot;

        if (dirt.DirtLevel <= 0f)
        {
            // Скрываем оверлей если чистое
            if (wearerSprite.LayerMapTryGet(overlayKey, out var hideIdx))
                wearerSprite.LayerSetVisible(hideIdx, false);
            return;
        }

        // Создаём слой если его нет
        if (!wearerSprite.LayerMapTryGet(overlayKey, out var layerIdx))
        {
            // Вставляем слой СРАЗУ ПОСЛЕ слоя соответствующей одежды
            // Это гарантирует что грязь рисуется поверх одежды, но под следующим слоем
            layerIdx = InsertDirtLayerAfterClothing(wearerSprite, slot, overlayKey);
        }

        wearerSprite.LayerSetVisible(layerIdx, true);
        wearerSprite.LayerSetRSI(layerIdx, new ResPath(DirtOverlayTexture));
        wearerSprite.LayerSetState(layerIdx, DirtOverlayState);

        // Прозрачность зависит от уровня загрязнения
        var alpha = MathHelper.Lerp(0.15f, 0.85f, dirt.DirtLevel);
        wearerSprite.LayerSetColor(layerIdx, dirt.DirtColor.WithAlpha(alpha));
    }

    /// <summary>
    /// Удаляет оверлей грязи при снятии одежды.
    /// </summary>
    private void RemoveBodyOverlay(EntityUid wearer, string slot)
    {
        if (!TryComp<SpriteComponent>(wearer, out var wearerSprite))
            return;

        var overlayKey = DirtOverlayPrefix + slot;
        if (wearerSprite.LayerMapTryGet(overlayKey, out var layerIdx))
            wearerSprite.LayerSetVisible(layerIdx, false);
    }

    /// <summary>
    /// Вставляет новый слой оверлея грязи сразу после слоя одежды на спрайте гуманоида.
    /// Возвращает индекс нового слоя.
    /// </summary>
    private int InsertDirtLayerAfterClothing(SpriteComponent sprite, string slot, string overlayKey)
    {
        // Ищем слой одежды по ключу (SS14 использует имя слота как ключ слоя)
        if (sprite.LayerMapTryGet(slot, out var clothingLayerIdx))
        {
            // Вставляем после слоя одежды
            var newIdx = sprite.AddLayer(
                new SpriteSpecifier.Rsi(new ResPath(DirtOverlayTexture), DirtOverlayState),
                clothingLayerIdx + 1);
            sprite.LayerMapSet(overlayKey, newIdx);
            return newIdx;
        }
        else
        {
            // Слой одежды не нашли — просто добавляем сверху
            var newIdx = sprite.AddLayer(
                new SpriteSpecifier.Rsi(new ResPath(DirtOverlayTexture), DirtOverlayState));
            sprite.LayerMapSet(overlayKey, newIdx);
            return newIdx;
        }
    }

    /// <summary>
    /// Находит персонажа-носителя предмета одежды и слот, в котором он надет.
    /// </summary>
    private bool TryGetWearer(EntityUid clothingUid, out EntityUid? wearer, out string? slotName)
    {
        wearer = null;
        slotName = null;

        // В SS14 надетая одежда находится в ContainerSlot инвентаря персонажа
        if (!_entMan.TryGetComponent<TransformComponent>(clothingUid, out var xform))
            return false;

        var parent = xform.ParentUid;
        if (!_entMan.EntityExists(parent))
            return false;

        // Проверяем все слоты инвентаря родителя
        foreach (var slot in SlotToHumanoidLayer.Keys)
        {
            if (!_inventory.TryGetSlotEntity(parent, slot, out var slotItem))
                continue;

            if (slotItem == clothingUid)
            {
                wearer = parent;
                slotName = slot;
                return true;
            }
        }

        return false;
    }
}
