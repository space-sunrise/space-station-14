// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Server._Nox.Disease.Components;
using Content.Shared._Nox.Disease;
using Content.Shared.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Nox.Disease.Components;
using Content.Shared.Popups;
using Content.Shared.Forensics.Components;
using Content.Shared.Examine;

namespace Content.Server._Nox.Disease.Systems;

public sealed class DiseaseDataCollectorSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseDataCollectorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DiseaseDataCollectorComponent, CollectDiseaseDataDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DiseaseDataCollectorComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, DiseaseDataCollectorComponent component, ExaminedEvent args)
    {
        if (component.Data != null)
            args.PushMarkup(Loc.GetString("disease-collector-has-data"));
        else
            args.PushMarkup(Loc.GetString("disease-collector-not-has-data"));
    }

    private void OnAfterInteract(Entity<DiseaseDataCollectorComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!CanBeUsed((entity, entity.Comp), target, args.User))
            return;

        _popup.PopupEntity(Loc.GetString("disease-collector-warn-target"), target, target, PopupType.Medium);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(entity.Comp.Duration), new CollectDiseaseDataDoAfterEvent(), entity, target: target, used: entity)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = entity.Comp.Distance
        });

    }

    private void OnDoAfter(Entity<DiseaseDataCollectorComponent> entity, ref CollectDiseaseDataDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<DiseaseComponent>(target, out var disease))
            return;

        if (TryComp<DnaComponent>(args.Target, out var dna))
            entity.Comp.DNA = dna.DNA ?? string.Empty;
        else
            entity.Comp.DNA = Loc.GetString("drug-collector-dna-not-found");

        // Собираем данные вируса
        entity.Comp.Data = (DiseaseData)disease.Data.Clone();
        entity.Comp.IsUsed = true;

        args.Handled = true;
    }

    private bool CanBeUsed(Entity<DiseaseDataCollectorComponent?> source, EntityUid target, EntityUid user)
    {
        if (!Resolve(source, ref source.Comp, false))
            return false;

        if (source.Comp.IsUsed)
        {
            _popup.PopupEntity(Loc.GetString("disease-collector-is-used"), user, user);

            return false;
        }

        if (!_ingestion.HasMouthAvailable(user, target))
        {
            _popup.PopupEntity(Loc.GetString("disease-collector-no-mouth"), user, user);

            return false;
        }

        return true;
    }
}
