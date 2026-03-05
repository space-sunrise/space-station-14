using Content.Shared._Sunrise.Disease;
using Content.Shared._Sunrise.Disease.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Disease.Systems;

public sealed class DiseaseContaminationSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> ContaminationShader = "DiseaseContamination";

    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EyeSystem _eye = default!;
    private readonly Dictionary<EntityUid, ShaderInstance> _shaderInstances = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseContaminationComponent, ComponentHandleState>(OnContaminationState);

        SubscribeLocalEvent<DiseaseContaminationComponent, ComponentStartup>(OnContaminationStartup);
        SubscribeLocalEvent<DiseaseContaminationComponent, ComponentShutdown>(OnContaminationShutdown);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, ComponentStartup>(OnDetectorUserStartup);
        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, ComponentShutdown>(OnDetectorUserShutdown);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, GetVisMaskEvent>(OnGetVisMask);
    }

    private void OnGetVisMask(Entity<DiseaseInfectionDetectorUserComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= BaseDiseaseSettings.DiseaseInfectionVisibilityFlag;
    }

    private void OnContaminationStartup(Entity<DiseaseContaminationComponent> ent, ref ComponentStartup args)
    {
        UpdateShader(ent.Owner, ent.Comp);
    }

    private void OnContaminationShutdown(Entity<DiseaseContaminationComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        ClearShader((ent.Owner, sprite));
    }

    private void OnPlayerAttached(Entity<DiseaseInfectionDetectorUserComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _eye.RefreshVisibilityMask(args.Entity);
        UpdateAllContaminationShaders();
    }

    private void OnPlayerDetached(Entity<DiseaseInfectionDetectorUserComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _eye.RefreshVisibilityMask(args.Entity);
        UpdateAllContaminationShaders();
    }

    private void OnDetectorUserStartup(Entity<DiseaseInfectionDetectorUserComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _eye.RefreshVisibilityMask(ent.Owner);
        UpdateAllContaminationShaders();
    }

    private void OnDetectorUserShutdown(Entity<DiseaseInfectionDetectorUserComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _eye.RefreshVisibilityMask(ent.Owner);
        UpdateAllContaminationShaders();
    }
    
    private void OnContaminationState(Entity<DiseaseContaminationComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DiseaseContaminationComponentState state)
            return;

        ent.Comp.Contamination = state.Contamination;
        ent.Comp.Color = state.Color;

        UpdateShader(ent.Owner, ent.Comp);
    }

    private void UpdateAllContaminationShaders()
    {
        var query = EntityQueryEnumerator<DiseaseContaminationComponent>();
        while (query.MoveNext(out var uid, out var contamination))
        {
            UpdateShader(uid, contamination);
        }
    }

    private void UpdateShader(EntityUid uid, DiseaseContaminationComponent contamination)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var shouldShow = CanSeeContamination() && contamination.Contamination > 0f;
        if (!shouldShow)
        {
            ClearShader((uid, sprite));
            return;
        }

        if (!_shaderInstances.TryGetValue(uid, out var instance))
        {
            instance = _prototype.Index(ContaminationShader).InstanceUnique();
            _shaderInstances[uid] = instance;
        }

        if (sprite.PostShader != null && sprite.PostShader != instance)
            return;

        instance.SetParameter("contaminationAmount", Math.Clamp(contamination.Contamination, 0f, 1f));
        instance.SetParameter("contaminationColor", contamination.Color);
        sprite.PostShader = instance;
    }

    private void ClearShader(Entity<SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!_shaderInstances.TryGetValue(ent.Owner, out var instance))
            return;

        if (ent.Comp.PostShader == instance)
            ent.Comp.PostShader = null;

        _shaderInstances.Remove(ent.Owner);
    }

    private bool CanSeeContamination()
    {
        if (_player.LocalEntity is not { } localEntity)
            return false;

        if (!TryComp<EyeComponent>(localEntity, out var eye))
            return false;

        return (eye.VisibilityMask & BaseDiseaseSettings.DiseaseInfectionVisibilityFlag) != 0;
    }
}