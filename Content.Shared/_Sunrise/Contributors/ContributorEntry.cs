using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Contributors
{
    [Serializable, NetSerializable]
    public sealed record ContributorEntry(
        int GithubId,
        string GithubLogin,
        string SS14UserId,
        string SS14Username,
        int Contributions);
}
