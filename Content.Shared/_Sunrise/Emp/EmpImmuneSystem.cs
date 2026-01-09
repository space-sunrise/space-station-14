using Content.Shared.Emp;

namespace Content.Shared._Sunrise.Emp;

public sealed class EmpImmuneSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpImmuneComponent, EmpAttemptEvent>(OnEmpAttempt);
    }

    private void OnEmpAttempt(Entity<EmpImmuneComponent> ent, ref EmpAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
