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
}
