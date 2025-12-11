using Content.Shared.Examine;
using Content.Shared.IdentityManagement;

namespace Content.Shared.DynamicDesc;

public sealed class DynamicDescSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DynamicDescComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<DynamicDescComponent> entity, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(entity, EntityManager);

        args.PushMarkup(entity.Comp.Content);
    }
}
