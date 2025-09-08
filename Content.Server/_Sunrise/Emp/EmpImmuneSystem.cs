using Content.Server.Emp;

namespace Content.Server._Sunrise.Emp;

public sealed class EmpImmuneSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpImmuneComponent, EmpAttemptEvent>(OnEmpAttempt);
    }

    private void OnEmpAttempt(EntityUid uid, EmpImmuneComponent comp, EmpAttemptEvent args)
    {
        args.Cancel();
    }
}
