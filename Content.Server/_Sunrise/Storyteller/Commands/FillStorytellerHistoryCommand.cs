using Content.Server.Administration;
using Content.Server._Sunrise.Storyteller.Systems;
using Content.Shared._Sunrise.Storyteller;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Sunrise.Storyteller.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class FillStorytellerHistoryCommand : IConsoleCommand
{
    public string Command => "fill_storyteller_history";
    public string Description => "Adds a mock entry for every type of storyteller history event for debugging purposes.";
    public string Help => "Usage: fill_storyteller_history";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var historySystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<StorytellerHistorySystem>();

        foreach (var type in Enum.GetValues<StorytellerHistoryType>())
        {
            var locKey = type switch
            {
                StorytellerHistoryType.HelpfulEvent => "storyteller-history-event-started",
                StorytellerHistoryType.NeutralEvent => "storyteller-history-event-started",
                StorytellerHistoryType.MinorCalmEvent => "storyteller-history-event-started",
                StorytellerHistoryType.MajorCalmEvent => "storyteller-history-event-started",
                StorytellerHistoryType.MinorAntagEvent => "storyteller-history-threat-started",
                StorytellerHistoryType.MajorAntagEvent => "storyteller-history-threat-started",
                StorytellerHistoryType.Death => "storyteller-history-crew-death-1",
                StorytellerHistoryType.AnomalyEngine => "storyteller-history-singularity-escaped",
                StorytellerHistoryType.Explosion => "storyteller-history-large-explosion",
                StorytellerHistoryType.Research => "storyteller-history-research-complete",
                StorytellerHistoryType.Arrival => "storyteller-history-arrival",
                StorytellerHistoryType.Departure => "storyteller-history-cryo-departure",
                StorytellerHistoryType.StationEvent => "storyteller-history-alert-level-changed",
                _ => "storyteller-history-event-started"
            };

            historySystem.LogHistoryEntry(type, locKey, 
                ("name", "DEBUG_EVENT"), 
                ("job", "DEBUG_JOB"),
                ("location", "DEBUG_LOCATION"),
                ("cause", "DEBUG_CAUSE"),
                ("severity", "DEBUG_SEVERITY"),
                ("discipline", "DEBUG_DISCIPLINE"),
                ("level", "Красный"),
                ("color", "#ff0000"));
        }

        historySystem.LogHistoryEntry(StorytellerHistoryType.StationEvent, "storyteller-history-alert-level-changed-with-prev", 
            ("level", "Красный"),
            ("color", "#ff0000"),
            ("prev", "Синий"),
            ("prevColor", "#0000ff"),
            ("duration", 10));

        historySystem.LogHistoryEntry(StorytellerHistoryType.StationEvent, "storyteller-history-nuke-armed", 
            ("location", "Капитанская"));
            
        historySystem.LogHistoryEntry(StorytellerHistoryType.StationEvent, "storyteller-history-nuke-disarmed");

        shell.WriteLine("Added debug history entries.");
    }
}
