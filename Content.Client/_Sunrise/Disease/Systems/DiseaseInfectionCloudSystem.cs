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
    }

    private void OnHandleState(EntityUid uid, DiseaseInfectionCloudComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not DiseaseInfectionCloudComponentState state)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            _sprite.LayerSetColor((uid, sprite), i, state.Color);
        }
    }
}