using Content.Server.Flash;
using Content.Server.GameTicking.Rules;
using Content.Shared.BloodBrother.Components;

namespace Content.Server.BloodBrother;

internal sealed class BloodBrotherConverterSystem : EntitySystem
{
    [Dependency] BloodBrotherRuleSystem _bbrule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherConverterComponent, AfterFlashedEvent>(OnFlash);
    }

    private void OnFlash(EntityUid uid, BloodBrotherConverterComponent comp, ref AfterFlashedEvent args)
    {
        if (!TryComp<SharedBloodBrotherComponent>(args.User, out var self))
            Log.Warning($"User {args.User} does not have SharedBloodBrotherComponent.");
            return;
        
        if (!TryComp<SharedBloodBrotherComponent>(args.Target, out _))
        {
            Log.Info($"Converting Target {args.Target} to blood brother with TeamID {self.TeamID}.");
            _bbrule.MakeBloodBrother(args.Target, self.TeamID);
        }
        else
        {
            Log.Info($"Target {args.Target} is already a blood brother.");
        }
    }
}
