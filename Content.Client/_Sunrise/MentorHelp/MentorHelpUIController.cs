using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._Sunrise.MentorHelp;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Administration;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._Sunrise.MentorHelp;

/// <summary>
/// UI controller for mentor help system
/// </summary>
[UsedImplicitly]
public sealed class MentorHelpUIController : UIController, IOnSystemChanged<MentorHelpSystem>, IOnStateChanged<GameplayState>, IOnStateChanged<LobbyState>
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [UISystemDependency] private readonly AudioSystem _audio = default!;

    private MentorHelpSystem? _mentorHelpSystem;
    public IMentorHelpUIHandler? UIHelper;
    private bool _hasMentorPermissions;
    private bool _hasUnreadTickets;
    private bool _mentorHelpSoundEnabled;
    private string? _mentorHelpSound;

    private Button? LobbyMHelpButton => (UIManager.ActiveScreen as LobbyGui)?.MHelpButton;
    private MenuButton? GameMHelpButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.MHelpButton;

    protected override string SawmillName => "c.s.go.es.mhelp";

    public override void Initialize()
    {
        base.Initialize();

        _adminManager.AdminStatusUpdated += OnAdminStatusUpdated;
        _config.OnValueChanged(SunriseCCVars.MentorHelpSound, v => _mentorHelpSound = v, true); // Reuse ahelp sound for now
        _config.OnValueChanged(SunriseCCVars.MentorHelpSoundEnabled, v => _mentorHelpSoundEnabled = v, true);
    }

    public void OnSystemLoaded(MentorHelpSystem system)
    {
        _mentorHelpSystem = system;
        _mentorHelpSystem.OnTicketUpdated += OnTicketUpdated;
        _mentorHelpSystem.OnTicketsListReceived += OnTicketsListReceived;
        _mentorHelpSystem.OnTicketMessagesReceived += OnTicketMessagesReceived;
        _mentorHelpSystem.OnOpenTicketReceived += OnOpenTicketReceived;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenMentorHelp,
                InputCmdHandler.FromDelegate(_ => Open()))
            .Register<MentorHelpUIController>();
    }

    private void OnOpenTicketReceived(object? sender, MentorHelpOpenTicketMessage message)
    {
        EnsureUIHelper();

        // Open the window and instruct UI to open the specific ticket
        Open();
        UIHelper?.OpenTicket(message.TicketId);
    }

    public void OnSystemUnloaded(MentorHelpSystem system)
    {
        CommandBinds.Unregister<MentorHelpUIController>();

        if (_mentorHelpSystem != null)
        {
            _mentorHelpSystem.OnTicketUpdated -= OnTicketUpdated;
            _mentorHelpSystem.OnTicketsListReceived -= OnTicketsListReceived;
            _mentorHelpSystem.OnTicketMessagesReceived -= OnTicketMessagesReceived;
            _mentorHelpSystem = null;
        }

        if (GameMHelpButton != null)
            GameMHelpButton.OnPressed -= MHelpButtonPressed;

        if (LobbyMHelpButton != null)
            LobbyMHelpButton.OnPressed -= MHelpButtonPressed;
    }

    public void OnStateEntered(GameplayState state)
    {
        EnsureUIHelper();
        SubscribeToButtons();
    }

    public void OnStateExited(GameplayState state)
    {
        // Keep UI helper for potential return to game
    }

    public void OnStateEntered(LobbyState state)
    {
        EnsureUIHelper();
        SubscribeToButtons();
    }

    public void OnStateExited(LobbyState state)
    {
        // Keep UI helper for potential return to lobby
    }

    private void SubscribeToButtons()
    {
        if (GameMHelpButton != null)
            GameMHelpButton.OnPressed += MHelpButtonPressed;

        if (LobbyMHelpButton != null)
            LobbyMHelpButton.OnPressed += MHelpButtonPressed;
    }

    private void MHelpButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        Open();
    }

    private void OnAdminStatusUpdated()
    {
        _hasMentorPermissions = _adminManager.HasFlag(AdminFlags.Mentor);

        if (UIHelper is not { IsOpen: true })
            return;

        EnsureUIHelper();
    }

    private void OnTicketUpdated(object? sender, MentorHelpTicketUpdateMessage message)
    {
        if (_mentorHelpSound != null && _mentorHelpSoundEnabled)
        {
            _audio.PlayGlobal(_mentorHelpSound, Filter.Local(), false);
            _clyde.RequestWindowAttention();
        }

        EnsureUIHelper();

        if (!UIHelper!.IsOpen)
        {
            UnreadTicketReceived();
        }

        UIHelper!.TicketUpdated(message.Ticket);
    }

    private void OnTicketsListReceived(object? sender, MentorHelpTicketsListMessage message)
    {
        EnsureUIHelper();
        UIHelper!.TicketsListReceived(message.Tickets);
    }

    private void OnTicketMessagesReceived(object? sender, MentorHelpTicketMessagesMessage message)
    {
        EnsureUIHelper();
        UIHelper!.TicketMessagesReceived(message.TicketId, message.Messages);
    }

    public void EnsureUIHelper()
    {
        var hasMentorPerms = _adminManager.HasFlag(AdminFlags.Mentor);

        if (UIHelper != null && UIHelper.HasMentorPermissions == hasMentorPerms)
            return;

        UIHelper?.Dispose();
        var ownerUserId = _playerManager.LocalUser!.Value;

        UIHelper = hasMentorPerms
            ? new MentorMentorHelpUIHandler(ownerUserId, _mentorHelpSystem)
            : new PlayerMentorHelpUIHandler(ownerUserId, _mentorHelpSystem);

        UIHelper.OnClose += () => { SetMentorHelpPressed(false); };
    }

    /// <summary>
    /// Open the mentor help window
    /// </summary>
    public void Open()
    {
        EnsureUIHelper();
        UIHelper!.OpenWindow();
        SetMentorHelpPressed(true);
    }

    /// <summary>
    /// Close the mentor help window
    /// </summary>
    public void Close()
    {
        UIHelper?.CloseWindow();
        SetMentorHelpPressed(false);
    }

    private void SetMentorHelpPressed(bool pressed)
    {
        UIManager.ClickSound();
        UnreadTicketRead();

        if (GameMHelpButton != null)
        {
            GameMHelpButton.Pressed = pressed;
        }

        if (LobbyMHelpButton != null)
        {
            LobbyMHelpButton.Pressed = pressed;
        }
    }

    private void UnreadTicketReceived()
    {
        _hasUnreadTickets = true;
        UpdateButtonStyling();
    }

    private void UnreadTicketRead()
    {
        _hasUnreadTickets = false;
        UpdateButtonStyling();
    }

    private void UpdateButtonStyling()
    {
        if (_hasUnreadTickets)
        {
            GameMHelpButton?.StyleClasses.Add(MenuButton.StyleClassRedTopButton);
            LobbyMHelpButton?.StyleClasses.Add("ButtonColorRed");
        }
        else
        {
            GameMHelpButton?.StyleClasses.Remove(MenuButton.StyleClassRedTopButton);
            LobbyMHelpButton?.StyleClasses.Remove("ButtonColorRed");
        }
    }
}

/// <summary>
/// Interface for mentor help UI handlers
/// </summary>
public interface IMentorHelpUIHandler : IDisposable
{
    bool IsOpen { get; }
    bool HasMentorPermissions { get; }
    event Action? OnClose;

    void OpenWindow();
    void CloseWindow();
    void OpenTicket(int ticketId);
    void TicketUpdated(MentorHelpTicketData ticket);
    void TicketsListReceived(List<MentorHelpTicketData> tickets);
    void TicketMessagesReceived(int ticketId, List<MentorHelpMessageData> messages);
}

public sealed class PlayerMentorHelpUIHandler : IMentorHelpUIHandler
{
    public bool IsOpen { get; private set; }
    public bool HasMentorPermissions => false;
    public event Action? OnClose;

    private readonly NetUserId _ownerUserId;
    private readonly MentorHelpSystem? _mentorHelpSystem;
    private MentorHelpWindow? _window;

    public PlayerMentorHelpUIHandler(NetUserId ownerUserId, MentorHelpSystem? mentorHelpSystem)
    {
        _ownerUserId = ownerUserId;
        _mentorHelpSystem = mentorHelpSystem;
    }

    public void OpenWindow()
    {
        if (_window != null)
        {
            _window.MoveToFront();
            IsOpen = true;
            return;
        }

        _window = new MentorHelpWindow();
        _window.MentorHelp.Initialize(_mentorHelpSystem, _ownerUserId, false);
        _window.OnClose += () =>
        {
            IsOpen = false;
            OnClose?.Invoke();
            _window = null;
        };

        _window.OpenCentered();
        IsOpen = true;

        _mentorHelpSystem?.RequestTickets(onlyMine: true);
    }

    public void OpenTicket(int ticketId)
    {
        // Ensure window is open
        OpenWindow();
        // Ask control to focus the ticket if possible
        _window?.MentorHelp.TryOpenTicket(ticketId);
        // Also request messages from server in case they're not loaded yet
        _mentorHelpSystem?.RequestTicketMessages(ticketId);
    }

    public void CloseWindow()
    {
        _window?.Close();
        IsOpen = false;
        OnClose?.Invoke();
    }

    public void TicketUpdated(MentorHelpTicketData ticket)
    {
        _window?.MentorHelp.UpdateTicket(ticket);
    }

    public void TicketsListReceived(List<MentorHelpTicketData> tickets)
    {
        _window?.MentorHelp.UpdateTicketsList(tickets);
    }

    public void TicketMessagesReceived(int ticketId, List<MentorHelpMessageData> messages)
    {
        _window?.MentorHelp.UpdateTicketMessages(ticketId, messages);
    }

    public void Dispose()
    {
        CloseWindow();
    }
}

/// <summary>
/// UI handler for mentors/admins (can see and manage all tickets)
/// </summary>
public sealed class MentorMentorHelpUIHandler : IMentorHelpUIHandler
{
    public bool IsOpen { get; private set; }
    public bool HasMentorPermissions => true;
    public event Action? OnClose;

    private readonly NetUserId _ownerUserId;
    private readonly MentorHelpSystem? _mentorHelpSystem;
    private MentorHelpWindow? _window;

    public MentorMentorHelpUIHandler(NetUserId ownerUserId, MentorHelpSystem? mentorHelpSystem)
    {
        _ownerUserId = ownerUserId;
        _mentorHelpSystem = mentorHelpSystem;
    }

    public void OpenWindow()
    {
        if (_window != null)
        {
            _window.MoveToFront();
            IsOpen = true;
            return;
        }

        _window = new MentorHelpWindow();
        _window.MentorHelp.Initialize(_mentorHelpSystem, _ownerUserId, true);
        _window.OnClose += () =>
        {
            IsOpen = false;
            OnClose?.Invoke();
            _window = null;
        };

        _window.OpenCentered();
        IsOpen = true;

        _mentorHelpSystem?.RequestTickets(onlyMine: false);
    }

    public void OpenTicket(int ticketId)
    {
        OpenWindow();
        _window?.MentorHelp.TryOpenTicket(ticketId);
        _mentorHelpSystem?.RequestTicketMessages(ticketId);
    }

    public void CloseWindow()
    {
        _window?.Close();
        IsOpen = false;
        OnClose?.Invoke();
    }

    public void TicketUpdated(MentorHelpTicketData ticket)
    {
        _window?.MentorHelp.UpdateTicket(ticket);
    }

    public void TicketsListReceived(List<MentorHelpTicketData> tickets)
    {
        _window?.MentorHelp.UpdateTicketsList(tickets);
    }

    public void TicketMessagesReceived(int ticketId, List<MentorHelpMessageData> messages)
    {
        _window?.MentorHelp.UpdateTicketMessages(ticketId, messages);
    }

    public void Dispose()
    {
        CloseWindow();
    }
}
