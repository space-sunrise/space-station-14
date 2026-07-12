using Content.Shared._Sunrise.Silicons.StationAi; // Sunrise-Edit - иконка тела ИИ в HUD профессий
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.Standing;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowJobIconsSystem : EquipmentHudSystem<ShowJobIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private static readonly ProtoId<JobIconPrototype> JobIconForNoId = "JobIconNoId";
    private static readonly ProtoId<JobIconPrototype> StationAiBodyJobIcon = "JobIconStationAi"; // Sunrise-Edit - иконка тела ИИ

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, StatusIconComponent _, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (_standing.IsDown(uid)) // Sunrise-standing
            return;

        var iconId = JobIconForNoId;

        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp<IdCardComponent>(item, out var id))
                {
                    iconId = id.JobIcon;
                    break;
                }

                // PDA
                if (TryComp<PdaComponent>(item, out var pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    iconId = id.JobIcon;
                    break;
                }
            }
        }
        // Sunrise added start - иконка для тела ИИ, по другому никак
        else if (TryComp<StationAiBodyComponent>(uid, out var stationAiBody))
        {
            if (stationAiBody.LinkedAi == null)
                return;

            iconId = StationAiBodyJobIcon;
        }
        // Sunrise added end

        if (_prototype.Resolve(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
        else
            Log.Error($"Invalid job icon prototype: {iconPrototype}");
    }
}
