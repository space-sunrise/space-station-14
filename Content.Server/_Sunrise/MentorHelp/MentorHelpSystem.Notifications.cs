using System.Linq;
using Content.Shared._Sunrise.MentorHelp;

namespace Content.Server._Sunrise.MentorHelp;

public sealed partial class MentorHelpSystem
{
    private static List<MentorHelpMessageData> GetPlayerVisibleMessages(IEnumerable<MentorHelpMessageData> messages)
    {
        return [.. messages.Where(message => !message.IsStaffOnly)];
    }
}
