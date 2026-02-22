using Content.Shared.Voting;
using Content.Shared._Sunrise.PlanetPrison; // Sunrise-Edit

namespace Content.Client.Voting;

public sealed class VotingSystem : EntitySystem
{
    public event Action<VotePlayerListResponseEvent>? VotePlayerListResponse; //Provides a list of players elligble for vote actions

    /// <summary>Список игроков на текущей карте тюрьмы (для PrisonExclude в VoteCallMenu).</summary>
    public event Action<PrisonMapPlayersResponseEvent>? PrisonPlayerListResponse; // Sunrise-Edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<VotePlayerListResponseEvent>(OnVotePlayerListResponseEvent);
        SubscribeNetworkEvent<PrisonMapPlayersResponseEvent>(OnPrisonMapPlayersResponseEvent); // Sunrise-Edit
    }

    private void OnVotePlayerListResponseEvent(VotePlayerListResponseEvent msg)
    {
        VotePlayerListResponse?.Invoke(msg);
    }

    private void OnPrisonMapPlayersResponseEvent(PrisonMapPlayersResponseEvent msg) // Sunrise-Edit
    {
        PrisonPlayerListResponse?.Invoke(msg);
    }

    public void RequestVotePlayerList()
    {
        RaiseNetworkEvent(new VotePlayerListRequestEvent());
    }

    /// <summary>Запросить список игроков на карте тюрьмы (для PrisonExclude в VoteCallMenu).</summary>
    public void RequestPrisonPlayerList() // Sunrise-Edit
    {
        RaiseNetworkEvent(new PrisonMapPlayersRequestMessage());
    }
}
