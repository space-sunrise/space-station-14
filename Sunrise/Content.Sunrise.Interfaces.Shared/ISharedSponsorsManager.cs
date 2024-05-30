using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.Sunrise.Interfaces.Shared;

public interface ISharedSponsorsManager
{
    public void Initialize();

    // Client
    public List<string> GetClientPrototypes();
    // Server
    public bool TryGetPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
}
