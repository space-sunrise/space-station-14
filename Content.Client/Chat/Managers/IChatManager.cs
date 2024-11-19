using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        event Action PermissionsUpdated; // Sunrise-Edit
        public void UpdatePermissions(); // Sunrise-Edit
        public void SendMessage(string text, ChatSelectChannel channel);
    }
}
