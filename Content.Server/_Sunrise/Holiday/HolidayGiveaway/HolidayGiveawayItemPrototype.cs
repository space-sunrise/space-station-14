using Content.Server.Holiday;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Holiday.HolidayGiveaway;

/// <summary>
/// Прототип для подарков, которые будут выданы в определенный праздник.
/// <seealso cref="HolidayGiveawaySystem"/>
/// <seealso cref="PreventHolidayGiveawayComponent"/>
/// </summary>
[Prototype]
public sealed partial class HolidayGiveawayItemPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<HolidayPrototype> Holiday;

    [DataField(required: true)]
    public EntProtoId Prototype;
}
