using System.Linq;
using System.Numerics;
using Content.Client._Sunrise.StatsBoard;
using Content.Client.Message;
using Content.Shared._Sunrise.StatsBoard;
using Content.Shared.GameTicking;
using Content.Shared._Sunrise.Storyteller; // Sunrise-Edit
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.RoundEnd
{
    public sealed class RoundEndSummaryWindow : DefaultWindow
    {
        private readonly IEntityManager _entityManager;
        private readonly ISharedPlayerManager _playerManager;
        public int RoundId;

        public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, int roundId,
            RoundEndMessageEvent.RoundEndPlayerInfo[] info, string roundEndStats, SharedStatisticEntry[] statisticEntries, IEntityManager entityManager, ISharedPlayerManager playerManager,
            string? storytellerName, StorytellerHistoryEntry[] storytellerHistory) // Sunrise-Edit
        {
            _entityManager = entityManager;
            _playerManager = playerManager; // Sunrise-Edit

            MinSize = SetSize = new Vector2(750, 650); // Sunrise-Edit

            Title = Loc.GetString("round-end-summary-window-title");

            RoundId = roundId;
            var roundEndTabs = new TabContainer();
            roundEndTabs.AddChild(MakeRoundEndStatsTab(roundEndStats)); // Sunrise-End
            roundEndTabs.AddChild(MakeRoundEndMyStatsTab(statisticEntries)); // Sunrise-End
            roundEndTabs.AddChild(MakeRoundEndSummaryTab(gm, roundEnd, roundTimeSpan, roundId, storytellerName)); // Sunrise-Edit
            roundEndTabs.AddChild(MakeStorytellerHistoryTab(storytellerHistory)); // Sunrise-Edit
            roundEndTabs.AddChild(MakePlayerManifestTab(info));

            ContentsContainer.AddChild(roundEndTabs);

            OpenCenteredRight();
            MoveToFront();
        }

        private BoxContainer MakeRoundEndSummaryTab(string gamemode, string roundEnd, TimeSpan roundDuration, int roundId, string? storytellerName) // Sunrise-Edit
        {
            var roundEndSummaryTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-round-end-summary-tab-title")
            };

            var roundEndSummaryContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var roundEndSummaryContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Gamemode Name
            var gamemodeLabel = new RichRichRichRichTextLabelHackForMarkup();
            var gamemodeMessage = new FormattedMessage();
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-round-id-label", ("roundId", roundId)));
            gamemodeMessage.AddText(" ");
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", gamemode)));
            gamemodeLabel.SetMessage(gamemodeMessage);
            roundEndSummaryContainer.AddChild(gamemodeLabel);

            // Active Storyteller
            if (!string.IsNullOrEmpty(storytellerName))
            {
                var storytellerLabel = new RichTextLabel();
                storytellerLabel.SetMarkup(Loc.GetString("round-end-summary-window-storyteller-name-label", ("storyteller", storytellerName)));
                roundEndSummaryContainer.AddChild(storytellerLabel);
            }

            //Duration
            var roundTimeLabel = new RichTextLabel();
            roundTimeLabel.SetMarkup(Loc.GetString("round-end-summary-window-duration-label",
                                                   ("hours", roundDuration.Hours),
                                                   ("minutes", roundDuration.Minutes),
                                                   ("seconds", roundDuration.Seconds)));
            roundEndSummaryContainer.AddChild(roundTimeLabel);

            //Round end text
            if (!string.IsNullOrEmpty(roundEnd))
            {
                var roundEndLabel = new RichTextLabel();
                roundEndLabel.SetMarkup(roundEnd);
                roundEndSummaryContainer.AddChild(roundEndLabel);
            }

            roundEndSummaryContainerScrollbox.AddChild(roundEndSummaryContainer);
            roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

            return roundEndSummaryTab;
        }

        private BoxContainer MakePlayerManifestTab(RoundEndMessageEvent.RoundEndPlayerInfo[] playersInfo)
        {
            var playerManifestTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-player-manifest-tab-title")
            };

            var playerInfoContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var playerInfoContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Put observers at the bottom of the list. Put antags on top.
            var sortedPlayersInfo = playersInfo.OrderBy(p => p.Observer).ThenBy(p => !p.Antag);

            //Create labels for each player info.
            foreach (var playerInfo in sortedPlayersInfo)
            {
                var hBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                };

                var playerInfoText = new RichTextLabel
                {
                    VerticalAlignment = VAlignment.Center,
                    VerticalExpand = true,
                };

                if (playerInfo.PlayerNetEntity != null)
                {
                    hBox.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, _entityManager)
                        {
                            OverrideDirection = Direction.South,
                            VerticalAlignment = VAlignment.Center,
                            SetSize = new Vector2(32, 32),
                            VerticalExpand = true,
                        });
                }

                if (playerInfo.PlayerICName != null)
                {
                    if (playerInfo.Observer)
                    {
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-observer-text",
                                          ("playerOOCName", playerInfo.PlayerOOCName),
                                          ("playerICName", playerInfo.PlayerICName)));
                    }
                    else
                    {
                        //TODO: On Hover display a popup detailing more play info.
                        //For example: their antag goals and if they completed them sucessfully.
                        var icNameColor = playerInfo.Antag ? "red" : "white";
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-not-observer-text",
                                ("playerOOCName", playerInfo.PlayerOOCName),
                                ("icNameColor", icNameColor),
                                ("playerICName", playerInfo.PlayerICName),
                                ("playerRole", Loc.GetString(playerInfo.Role))));
                    }
                }
                hBox.AddChild(playerInfoText);
                playerInfoContainer.AddChild(hBox);
            }

            playerInfoContainerScrollbox.AddChild(playerInfoContainer);
            playerManifestTab.AddChild(playerInfoContainerScrollbox);

            return playerManifestTab;
        }

        // Sunrise-Start
        private BoxContainer MakeRoundEndStatsTab(string stats)
        {
            var roundEndSummaryTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-stats-tab-title")
            };

            var roundEndSummaryContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var roundEndSummaryContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Round end text
            if (!string.IsNullOrEmpty(stats))
            {
                var statsLabel = new RichTextLabel();
                statsLabel.SetMarkup(stats);
                roundEndSummaryContainer.AddChild(statsLabel);
            }

            roundEndSummaryContainerScrollbox.AddChild(roundEndSummaryContainer);
            roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

            return roundEndSummaryTab;
        }

        private BoxContainer MakeRoundEndMyStatsTab(SharedStatisticEntry[] statisticEntries)
        {
            var roundEndSummaryTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-my-stats-tab-title")
            };

            var roundEndSummaryContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10),
            };

            var statsEntries = new StatsEntries();
            foreach (var statisticEntry in statisticEntries)
            {
                if (statisticEntry.FirstActor != _playerManager.LocalSession!.UserId)
                    continue;

                var statsEntry = new StatsEntry(statisticEntry.Name, statisticEntry.TotalTakeDamage,
                    statisticEntry.TotalTakeHeal, statisticEntry.TotalInflictedDamage,
                    statisticEntry.TotalInflictedHeal, statisticEntry.SlippedCount,
                    statisticEntry.CreamedCount, statisticEntry.DoorEmagedCount, statisticEntry.ElectrocutedCount,
                    statisticEntry.CuffedCount, statisticEntry.AbsorbedPuddleCount, statisticEntry.SpentTk ?? 0,
                    statisticEntry.DeadCount, statisticEntry.HumanoidKillCount, statisticEntry.KilledMouseCount,
                    statisticEntry.CuffedTime, statisticEntry.SpaceTime, statisticEntry.SleepTime,
                    statisticEntry.IsInteractedCaptainCard ? Loc.GetString("accept-cloning-window-accept-button") : Loc.GetString("accept-cloning-window-deny-button"));
                statsEntries.AddEntry(statsEntry);
            }

            roundEndSummaryContainerScrollbox.AddChild(statsEntries);
            roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

            return roundEndSummaryTab;
        }
        // Sunrise-End

        private BoxContainer MakeStorytellerHistoryTab(StorytellerHistoryEntry[] history)
        {
            var tab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-storyteller-history-tab-title")
            };

            var scroll = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 10
            };

            if (history == null || history.Length == 0)
            {
                var emptyLabel = new RichTextLabel();
                emptyLabel.SetMarkup(Loc.GetString("round-end-summary-window-storyteller-history-empty"));
                container.AddChild(emptyLabel);
            }
            else
            {
                var titleLabel = new RichTextLabel();
                titleLabel.SetMarkup("[bold][size=14]Хронология событий раунда:[/size][/bold]\n");
                container.AddChild(titleLabel);

                foreach (var entry in history)
                {
                    var hBox = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        SeparationOverride = 12
                    };

                    var timeLabel = new RichTextLabel
                    {
                        SetWidth = 80,
                    };
                    var formattedTime = $"[color=#A9A9A9]({entry.RoundTime.ToString(@"hh\:mm\:ss")})[/color]";
                    timeLabel.SetMarkup(formattedTime);

                    var textLabel = new RichTextLabel
                    {
                        HorizontalExpand = true
                    };
                    textLabel.SetMarkupPermissive(entry.Description);

                    hBox.AddChild(timeLabel);
                    hBox.AddChild(textLabel);
                    container.AddChild(hBox);
                }
            }

            scroll.AddChild(container);
            tab.AddChild(scroll);

            return tab;
        }
    }

    // Sunrise-Start
    // Simple helper class to allow compiling gamemodeLabel in the modified MakeRoundEndSummaryTab
    internal sealed class RichRichRichRichTextLabelHackForMarkup : RichTextLabel
    {
    }
    // Sunrise-End

}
