using System.Linq;
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
        SubscribeLocalEvent<DiseaseInfectionCloudComponent, ComponentInit>(OnInit);
    }

    private void OnHandleState(Entity<DiseaseInfectionCloudComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not DiseaseInfectionCloudComponentState state)
            return;

        SetColorCloud((entity, entity.Comp), state.Color);
    }

    private void OnInit(Entity<DiseaseInfectionCloudComponent> entity, ref ComponentInit args)
    {
        SetColorCloud((entity, entity.Comp), entity.Comp.Data?.Color ?? Color.White);
    }

    public void SetColorCloud(Entity<DiseaseInfectionCloudComponent?> entity, Color color)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            _sprite.LayerSetColor((entity, sprite), i, color);
        }
    }
}