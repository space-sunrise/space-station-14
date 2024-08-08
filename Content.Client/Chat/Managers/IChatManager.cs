using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager
    {
        void Initialize();
        event Action PermissionsUpdated; // Sunrise-Edit
        public void SendMessage(string text, ChatSelectChannel channel);
        public void UpdatePermissions(); // Sunrise-Edit
    }
}
