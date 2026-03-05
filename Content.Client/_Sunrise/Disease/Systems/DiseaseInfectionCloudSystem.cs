using Content.Shared._Sunrise.Disease.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._Sunrise.Disease.Systems;

public sealed class DiseaseInfectionCloudSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseInfectionCloudComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<DiseaseInfectionCloudComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not DiseaseInfectionCloudComponentState state)
            return;

        SetColorCloud((entity, entity.Comp), state.Color);
    }

    public void SetColorCloud(Entity<DiseaseInfectionCloudComponent?> entity, Color color)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite))
            return;

        _sprite.SetColor((entity.Owner, sprite), color);
    }
}