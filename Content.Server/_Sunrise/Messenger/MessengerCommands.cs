namespace Content.Server._Sunrise.Messenger;

/// <summary>
/// Константы команд для мессенджера
/// </summary>
public static class MessengerCommands
{
    // Входящие команды
    public const string CmdRegisterUser = "messenger_register_user";
    public const string CmdSendMessage = "messenger_send_message";
    public const string CmdCreateGroup = "messenger_create_group";
    public const string CmdAddToGroup = "messenger_add_to_group";
    public const string CmdRemoveFromGroup = "messenger_remove_from_group";
    public const string CmdGetUsers = "messenger_get_users";
    public const string CmdGetGroups = "messenger_get_groups";
    public const string CmdGetMessages = "messenger_get_messages";

    // Исходящие команды
    public const string CmdUserRegistered = "messenger_user_registered";
    public const string CmdUsersList = "messenger_users_list";
    public const string CmdGroupsList = "messenger_groups_list";
    public const string CmdMessagesList = "messenger_messages_list";
    public const string CmdMessageReceived = "messenger_message_received";
    public const string CmdGroupCreated = "messenger_group_created";
    public const string CmdUserAddedToGroup = "messenger_user_added_to_group";
    public const string CmdInviteReceived = "messenger_invite_received";
    public const string CmdInviteAccepted = "messenger_invite_accepted";
    public const string CmdInviteDeclined = "messenger_invite_declined";
    public const string CmdInvitesList = "messenger_invites_list";

    // Входящие команды для инвайтов
    public const string CmdAcceptInvite = "messenger_accept_invite";
    public const string CmdDeclineInvite = "messenger_decline_invite";

    // Входящие команды для выхода из группы
    public const string CmdLeaveGroup = "messenger_leave_group";

    // Исходящие команды для выхода из группы
    public const string CmdUserLeftGroup = "messenger_user_left_group";
    public const string CmdGroupDeleted = "messenger_group_deleted";

    // Входящие команды для удаления сообщений
    public const string CmdDeleteMessage = "messenger_delete_message";

    // Исходящие команды для удаления сообщений
    public const string CmdMessageDeleted = "messenger_message_deleted";
}
