using Content.Shared.Clothing.Dirt;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Server.Clothing.Dirt;

/// <summary>
/// Глагол "Отмыть" — позволяет очистить загрязнённую одежду.
/// В будущем можно расширить: раковина, ведро с водой, химчистка.
/// </summary>
public sealed class ClothingDirtCleanSystem : EntitySystem
{
    [Dependency] private readonly SharedClothingDirtSystem _dirt = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingDirtComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ClothingDirtComponent, ClothingCleanDoAfterEvent>(OnCleanDoAfter);
    }

    private void OnGetVerbs(EntityUid uid, ClothingDirtComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || component.DirtLevel <= 0f)
            return;

        args.Verbs.Add(new UtilityVerb
        {
            Act = () =>
            {
                var ev = new DoAfterArgs(EntityManager, args.User, 3f,
                    new ClothingCleanDoAfterEvent(), uid, uid)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = true,
                };
                _doAfter.TryStartDoAfter(ev);
                _popup.PopupEntity(Loc.GetString("clothing-dirt-clean-start"), uid, args.User);
            },
            Text = Loc.GetString("clothing-dirt-clean-verb"),
        });
    }

    private void OnCleanDoAfter(EntityUid uid, ClothingDirtComponent component, ClothingCleanDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _dirt.CleanClothing(uid, component);
        _popup.PopupEntity(Loc.GetString("clothing-dirt-clean-done"), uid, args.User);
    }
}

[Serializable, NetSerializable]
public sealed partial class ClothingCleanDoAfterEvent : SimpleDoAfterEvent { }
