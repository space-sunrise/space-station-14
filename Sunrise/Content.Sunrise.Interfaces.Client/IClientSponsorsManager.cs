using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Maths;

namespace Content.Sunrise.Interfaces.Client;

public interface IClientSponsorsManager : ISharedSponsorsManager
{
    public List<string> Prototypes { get; }
    public bool PriorityJoin { get; }
    public Color? OocColor { get; }
    public int ExtraCharSlots { get; }
    public bool AllowedRespawn { get; }
}
