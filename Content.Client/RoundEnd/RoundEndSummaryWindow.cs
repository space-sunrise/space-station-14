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
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
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
            roundEndTabs.AddChild(MakeStorytellerHistoryTab(storytellerHistory)); // Sunrise-Edit
            roundEndTabs.AddChild(MakeRoundEndStatsTab(roundEndStats)); // Sunrise-End
            roundEndTabs.AddChild(MakeRoundEndMyStatsTab(statisticEntries)); // Sunrise-End
            roundEndTabs.AddChild(MakeRoundEndSummaryTab(gm, roundEnd, roundTimeSpan, roundId, storytellerName)); // Sunrise-Edit
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

        private Color GetEventTypeColor(StorytellerHistoryType type)
        {
            return type switch
            {
                StorytellerHistoryType.HelpfulEvent => Color.FromHex("#2ecc71").WithAlpha(0.2f),
                StorytellerHistoryType.NeutralEvent => Color.FromHex("#7f8c8d").WithAlpha(0.2f),
                StorytellerHistoryType.MinorCalmEvent => Color.FromHex("#3498db").WithAlpha(0.2f),
                StorytellerHistoryType.MajorCalmEvent => Color.FromHex("#9b59b6").WithAlpha(0.2f),
                StorytellerHistoryType.MinorAntagEvent => Color.FromHex("#e67e22").WithAlpha(0.2f),
                StorytellerHistoryType.MajorAntagEvent => Color.FromHex("#e74c3c").WithAlpha(0.2f),
                StorytellerHistoryType.Death => Color.FromHex("#c0392b").WithAlpha(0.2f),
                StorytellerHistoryType.AnomalyEngine => Color.FromHex("#e67e22").WithAlpha(0.2f),
                StorytellerHistoryType.StationEvent => Color.FromHex("#34495e").WithAlpha(0.2f),
                StorytellerHistoryType.Explosion => Color.FromHex("#d35400").WithAlpha(0.2f),
                StorytellerHistoryType.Research => Color.FromHex("#9b59b6").WithAlpha(0.2f),
                StorytellerHistoryType.Arrival => Color.FromHex("#2ecc71").WithAlpha(0.2f),
                StorytellerHistoryType.Departure => Color.FromHex("#3498db").WithAlpha(0.2f),
                _ => Color.FromHex("#95a5a6").WithAlpha(0.2f),
            };
        }

        private string GetEventTypeName(StorytellerHistoryType type)
        {
            return type switch
            {
                StorytellerHistoryType.HelpfulEvent => "Положительные",
                StorytellerHistoryType.NeutralEvent => "Нейтральные события",
                StorytellerHistoryType.MinorCalmEvent => "Мелкие проишествия",
                StorytellerHistoryType.MajorCalmEvent => "Крупные проишествия",
                StorytellerHistoryType.MinorAntagEvent => "Антагонисты",
                StorytellerHistoryType.MajorAntagEvent => "Крупные антагонисты",
                StorytellerHistoryType.Death => "Смерти",
                StorytellerHistoryType.AnomalyEngine => "Аномальные двигатели",
                StorytellerHistoryType.Explosion => "Взрывы",
                StorytellerHistoryType.Research => "Исследования",
                StorytellerHistoryType.Arrival => "Прибытия",
                StorytellerHistoryType.Departure => "Крио",
                _ => type.ToString(),
            };
        }

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
                Margin = new Thickness(10),
                HScrollEnabled = false
            };
            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 10
            };

            var eventPanels = new List<(StorytellerHistoryType Type, Control Panel)>();

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
                    var panelColor = GetEventTypeColor(entry.EventType);
                    var panel = new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = panelColor,
                            BorderColor = panelColor.WithAlpha(1f),
                            BorderThickness = new Thickness(1),
                            ContentMarginBottomOverride = 6,
                            ContentMarginLeftOverride = 6,
                            ContentMarginRightOverride = 6,
                            ContentMarginTopOverride = 6
                        }
                    };

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
                    panel.AddChild(hBox);
                    container.AddChild(panel);

                    eventPanels.Add((entry.EventType, panel));
                }
            }

            scroll.AddChild(container);
            tab.AddChild(scroll);

            if (history != null && history.Length > 0)
            {
                var filtersGrid = new GridContainer
                {
                    Columns = 4,
                    Margin = new Thickness(10, 0, 10, 10)
                };

                var categories = new Dictionary<string, List<StorytellerHistoryType>>
                {
                    { Loc.GetString("storyteller-history-filter-events"), new List<StorytellerHistoryType> { StorytellerHistoryType.HelpfulEvent, StorytellerHistoryType.NeutralEvent, StorytellerHistoryType.MinorCalmEvent, StorytellerHistoryType.MajorCalmEvent } },
                    { Loc.GetString("storyteller-history-filter-antagonists"), new List<StorytellerHistoryType> { StorytellerHistoryType.MinorAntagEvent, StorytellerHistoryType.MajorAntagEvent } },
                    { Loc.GetString("storyteller-history-filter-station"), new List<StorytellerHistoryType> { StorytellerHistoryType.StationEvent } },
                    { Loc.GetString("storyteller-history-filter-deaths"), new List<StorytellerHistoryType> { StorytellerHistoryType.Death } },
                    { Loc.GetString("storyteller-history-filter-anomalies"), new List<StorytellerHistoryType> { StorytellerHistoryType.AnomalyEngine } },
                    { Loc.GetString("storyteller-history-filter-explosions"), new List<StorytellerHistoryType> { StorytellerHistoryType.Explosion } },
                    { Loc.GetString("storyteller-history-filter-research"), new List<StorytellerHistoryType> { StorytellerHistoryType.Research } },
                    { Loc.GetString("storyteller-history-filter-arrivals"), new List<StorytellerHistoryType> { StorytellerHistoryType.Arrival } },
                    { Loc.GetString("storyteller-history-filter-cryo"), new List<StorytellerHistoryType> { StorytellerHistoryType.Departure } }
                };

                foreach (var kvp in categories)
                {
                    var isHiddenDefault = kvp.Value.Contains(StorytellerHistoryType.Arrival) || kvp.Value.Contains(StorytellerHistoryType.Departure);
                    var cb = new CheckBox
                    {
                        Text = kvp.Key,
                        Pressed = !isHiddenDefault
                    };

                    cb.OnToggled += args =>
                    {
                        foreach (var (pType, pControl) in eventPanels)
                        {
                            if (kvp.Value.Contains(pType))
                                pControl.Visible = args.Pressed;
                        }
                    };

                    // Initialize visibility
                    foreach (var (pType, pControl) in eventPanels)
                    {
                        if (kvp.Value.Contains(pType))
                            pControl.Visible = !isHiddenDefault;
                    }

                    filtersGrid.AddChild(cb);
                }
                tab.AddChild(filtersGrid);
            }

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
