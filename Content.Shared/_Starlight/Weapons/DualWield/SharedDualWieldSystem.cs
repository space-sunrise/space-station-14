using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Sunrise.Weapons.DualWield;

/// <summary>
/// Sunrise-Edit: Управляет режимом "стрельбы по македонски" (двойная стрельба).
///
/// Автоматически активирует режим когда в руках находятся 2 пистолета с CanDualWieldComponent.
/// Автоматически деактивирует режим когда оружие выпадает или берется только одно.
///
/// Обрабатывает:
/// - Переключение режима
/// - Штрафы к точности
/// - Отключение при выпадении оружия
/// - Автоматическую активацию/деактивацию
/// </summary>
public sealed class SharedDualWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Sunrise-Edit: Режим стрельбы по македонски — штраф точности
        SubscribeLocalEvent<CanDualWieldComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        // Sunrise-Edit: Режим стрельбы по македонски — отключение при выпадении оружия
        SubscribeLocalEvent<GunComponent, GotUnequippedHandEvent>(OnGunUnequipped);
    }

    /// <summary>
    /// OnEvent: Применяет штраф точности при активном режиме двойной стрельбы.
    /// </summary>
    private void OnGunRefreshModifiers(Entity<CanDualWieldComponent> gun, ref GunRefreshModifiersEvent args)
    {
        if (gun.Comp.DualWieldInaccuracyPenalty <= 0f)
            return;

        // Пистолет находится в ContainerSlot, родитель которого - держатель оружия
        var holder = Transform(gun).ParentUid;
        if (!TryComp<DualWieldComponent>(holder, out var dw) || !dw.Active)
            return;

        // Проверяем, что пистолет принадлежит активному режиму
        if (dw.LeftGun != gun.Owner && dw.RightGun != gun.Owner)
            return;

        // Применяем штраф к углам разброса
        var penalty = Angle.FromDegrees(gun.Comp.DualWieldInaccuracyPenalty);
        args.MinAngle += penalty;
        args.MaxAngle += penalty;
    }

    /// <summary>
    /// OnEvent: Отключает режим при выпадении одного из пистолетов.
    /// </summary>
    private void OnGunUnequipped(Entity<GunComponent> gun, ref GotUnequippedHandEvent args)
    {
        if (!TryComp<DualWieldComponent>(args.User, out var dw) || !dw.Active)
            return;

        // Проверяем, что выпал один из наших пистолетов
        if (dw.LeftGun != gun.Owner && dw.RightGun != gun.Owner)
            return;

        // Отключаем режим
        DisableDualWield(args.User, dw);
        _popup.PopupClient(Loc.GetString("dual-wield-interrupted"), args.User, args.User);
    }

    /// <summary>
    /// Update: Проверяет наличие двух пистолетов в руках для автоматической активации/деактивации режима.
    /// Sunrise-Edit: Автоматическое управление режимом двойной стрельбы
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Ищем все персонажей с компонентом рук
        var query = EntityQueryEnumerator<HandsComponent>();
        while (query.MoveNext(out var uid, out var handsComp))
        {
            // Проверяем есть ли у него уже активный режим
            if (!TryComp<DualWieldComponent>(uid, out var dw))
            {
                // Нет режима - проверяем можно ли активировать
                if (TryGetBothDualWieldGuns(uid, handsComp, out var gun1, out var gun2))
                {
                    dw = EnsureComp<DualWieldComponent>(uid);
                    EnableDualWield(uid, dw, gun1, gun2);
                    _popup.PopupClient(Loc.GetString("dual-wield-enabled"), uid, uid);
                }
            }
            else if (dw.Active)
            {
                // Режим активен - проверяем нужно ли его отключить
                if (!TryGetBothDualWieldGuns(uid, handsComp, out _, out _))
                {
                    DisableDualWield(uid, dw);
                    _popup.PopupClient(Loc.GetString("dual-wield-interrupted"), uid, uid);
                }
            }
        }
    }

    /// <summary>
    /// EnableDualWield: Включает режим с двумя пистолетами.
    /// Sunrise-Edit: Активация режима двойной стрельбы
    /// </summary>
    private void EnableDualWield(EntityUid user, DualWieldComponent dw, EntityUid gun1, EntityUid gun2)
    {
        dw.Active = true;
        dw.LeftGun = gun1;
        dw.RightGun = gun2;

        // Начинаем со стрельбы из активной руки
        var activeItem = _hands.GetActiveItem((user, default));
        dw.NextIsLeft = activeItem == gun1;

        Dirty(user, dw);
        _gun.RefreshModifiers((gun1, default!));
        _gun.RefreshModifiers((gun2, default!));
    }

    /// <summary>
    /// DisableDualWield: Отключает режим и снимает штрафы точности.
    /// Sunrise-Edit: Деактивация режима двойной стрельбы
    /// </summary>
    private void DisableDualWield(EntityUid user, DualWieldComponent dw)
    {
        dw.Active = false;
        Dirty(user, dw);
        _gun.RefreshModifiers((dw.LeftGun, default!));
        _gun.RefreshModifiers((dw.RightGun, default!));
    }

    /// <summary>
    /// TryGetBothDualWieldGuns: Проверяет наличие 2-х пистолетов с CanDualWieldComponent в руках.
    /// gun1 - пистолет из активной руки;
    /// gun2 - пистолет из другой руки.
    /// Sunrise-Edit: Проверка наличия двух пистолетов
    /// </summary>
    private bool TryGetBothDualWieldGuns(
        EntityUid user,
        HandsComponent handsComp,
        out EntityUid gun1,
        out EntityUid gun2)
    {
        gun1 = EntityUid.Invalid;
        gun2 = EntityUid.Invalid;

        // EnumerateHeld начинает с активной руки — gun1 получается автоматически
        foreach (var held in _hands.EnumerateHeld((user, handsComp)))
        {
            if (!HasComp<GunComponent>(held) || !HasComp<CanDualWieldComponent>(held))
                continue;

            if (gun1 == EntityUid.Invalid)
                gun1 = held;
            else if (gun2 == EntityUid.Invalid)
            {
                gun2 = held;
                break;
            }
        }

        return gun1 != EntityUid.Invalid && gun2 != EntityUid.Invalid;
    }

    /// <summary>
    /// TryGetBothGuns: Возвращает оба пистолета из рук (без проверки CanDualWieldComponent).
    /// Используется для возможного manual toggle в будущем.
    /// Sunrise-Edit: Получение обоих пистолетов
    /// </summary>
    public bool TryGetBothGuns(
        EntityUid user,
        HandsComponent handsComp,
        out EntityUid gun1,
        out EntityUid gun2)
    {
        gun1 = EntityUid.Invalid;
        gun2 = EntityUid.Invalid;

        foreach (var held in _hands.EnumerateHeld((user, handsComp)))
        {
            if (!HasComp<GunComponent>(held))
                continue;

            if (gun1 == EntityUid.Invalid)
                gun1 = held;
            else if (gun2 == EntityUid.Invalid)
            {
                gun2 = held;
                break;
            }
        }

        return gun1 != EntityUid.Invalid && gun2 != EntityUid.Invalid;
    }
}
