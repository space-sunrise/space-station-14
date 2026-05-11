using Content.Shared.Clothing.Dirt;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Clothing.Dirt;

public sealed class ClothingDirtCleanSystem : SharedClothingDirtSystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    // реагенты которые смывают грязь
    private static readonly HashSet<string> Cleaners = new()
    {
        "Water", "SpaceCleaner", "Soap",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingDirtComponent, InteractUsingEvent>(OnCleanTry);
        SubscribeLocalEvent<ClothingDirtComponent, ClothingCleanDoAfter>(OnCleanDone);
    }

    private void OnCleanTry(EntityUid uid, ClothingDirtComponent dirt, InteractUsingEvent args)
    {
        if (dirt.DirtLevel <= 0f)
            return;

        if (!TryComp<SolutionContainerManagerComponent>(args.Used, out var mgr))
            return;

        var hasClean = mgr.Solutions.Values
            .Any(sol => sol.Contents.Any(r => Cleaners.Contains(r.ReagentId)));

        if (!hasClean)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager, args.User, 2f,
            new ClothingCleanDoAfter(),
            uid, target: uid, used: args.Used)
        {
            BreakOnMove = true,
            NeedHand = true,
        });

        args.Handled = true;
    }

    private void OnCleanDone(EntityUid uid, ClothingDirtComponent _, ClothingCleanDoAfter args)
    {
        if (args.Cancelled)
            return;

        CleanDirt(uid, 50f); // одна чистка снимает половину
    }
}

[Serializable, NetSerializable]
public sealed partial class ClothingCleanDoAfter : SimpleDoAfterEvent { }
