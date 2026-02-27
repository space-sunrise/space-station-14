using Content.Server.Vocalization.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Vocalization.Systems;

/// <inheritdoc cref="DatasetVocalizerComponent"/>
public sealed class DatasetVocalizationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DatasetVocalizerComponent, ComponentInit>(OnComponentInit); // Sunrise-Edit
        SubscribeLocalEvent<DatasetVocalizerComponent, TryVocalizeEvent>(OnTryVocalize);
    }
    // Sunrise-Start
    private void OnComponentInit(Entity<DatasetVocalizerComponent> ent, ref ComponentInit args)
    {
        var vocalizer = EnsureComp<VocalizerComponent>(ent);

        if (ent.Comp.MinVocalizeInterval is { } min)
            vocalizer.MinVocalizeInterval = min;

        if (ent.Comp.MaxVocalizeInterval is { } max)
            vocalizer.MaxVocalizeInterval = max;

        if (vocalizer.MaxVocalizeInterval < vocalizer.MinVocalizeInterval)
            vocalizer.MaxVocalizeInterval = vocalizer.MinVocalizeInterval;

        if (ent.Comp.HideChat is { } hideChat)
            vocalizer.HideChat = hideChat;
    }
    // Sunrise-End

    private void OnTryVocalize(Entity<DatasetVocalizerComponent> ent, ref TryVocalizeEvent args)
    {
        if (args.Handled)
            return;

        var dataset = _protoMan.Index(ent.Comp.Dataset);

        args.Message = _random.Pick(dataset);
        args.Handled = true;
    }
}
