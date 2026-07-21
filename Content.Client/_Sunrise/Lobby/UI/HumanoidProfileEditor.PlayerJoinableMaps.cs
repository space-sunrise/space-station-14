using System.Numerics;
using System.Linq;
using Content.Client.Lobby.UI.Loadouts;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Sprite;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

#pragma warning disable IDE0130
namespace Content.Client.Lobby.UI;

/// <summary>
/// Integrates Player Joinable Maps with the character profile job editor.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    private readonly Dictionary<CVarDef<bool>, Action<bool>> _playerJoinableMapBoolCVarHandlers = new();
    private readonly Dictionary<CVarDef<int>, Action<int>> _playerJoinableMapIntCVarHandlers = new();
    private readonly PlayerJoinableMapIndex _playerJoinableMapIndex = new();
    private readonly List<PlayerJoinableMapPrototype> _availablePlayerJoinableMaps = new();
    private readonly HashSet<ProtoId<JobPrototype>> _availablePlayerJoinableMapJobs = new();

    private int _lastPlayerCount;

    partial void InitializePlayerJoinableMapsPortal();
    partial void ShutdownPlayerJoinableMapsPortal();
    partial void FilterPlayerJoinableMapDepartmentsPortal(List<DepartmentPrototype> departments);
    partial void FilterPlayerJoinableMapJobsPortal(DepartmentPrototype department, ref JobPrototype[] jobs);
    partial void AddPlayerJoinableMapSectionsPortal(
        (string LocKey, int Priority)[] priorityItems,
        string[] sponsorPrototypes,
        ref bool firstCategory);

    partial void InitializePlayerJoinableMapsPortal()
    {
        _playerJoinableMapIndex.Rebuild(_prototypeManager);
        _lastPlayerCount = _playerManager.PlayerCount;
        SubscribePlayerJoinableMapCVars();
        _playerManager.PlayerStatusChanged += OnPlayerJoinableMapPlayerStatusChanged;
        _prototypeManager.PrototypesReloaded += OnPlayerJoinableMapPrototypesReloaded;
    }

    partial void ShutdownPlayerJoinableMapsPortal()
    {
        UnsubscribePlayerJoinableMapCVars();
        _playerManager.PlayerStatusChanged -= OnPlayerJoinableMapPlayerStatusChanged;
        _prototypeManager.PrototypesReloaded -= OnPlayerJoinableMapPrototypesReloaded;
    }

    partial void FilterPlayerJoinableMapDepartmentsPortal(List<DepartmentPrototype> departments)
    {
        departments.RemoveAll(department =>
        {
            var hasPlayerJoinableMapJob = false;
            foreach (var jobId in department.Roles)
            {
                if (!_prototypeManager.TryIndex(jobId, out var job) || !job.SetPreference)
                    continue;

                if (!_playerJoinableMapIndex.Jobs.Contains(job.ID))
                    return false;

                hasPlayerJoinableMapJob = true;
            }

            return hasPlayerJoinableMapJob;
        });
    }

    partial void FilterPlayerJoinableMapJobsPortal(DepartmentPrototype department, ref JobPrototype[] jobs)
    {
        if (_jobPriorities.Count == 0)
            RefreshPlayerJoinableMapAccess();

        jobs = jobs
            .Where(job => !_playerJoinableMapIndex.Jobs.Contains(job.ID))
            .ToArray();
    }

    partial void AddPlayerJoinableMapSectionsPortal(
        (string LocKey, int Priority)[] priorityItems,
        string[] sponsorPrototypes,
        ref bool firstCategory)
    {
        RefreshPlayerJoinableMapAccess();

        var departments = GetPlayerJoinableMapDepartments();
        var displayedJobs = new HashSet<ProtoId<JobPrototype>>();

        foreach (var map in _availablePlayerJoinableMaps)
        {
            var sections = new List<(DepartmentPrototype Department, JobPrototype[] Jobs)>();

            foreach (var department in departments)
            {
                var jobs = new List<JobPrototype>();
                foreach (var jobId in department.Roles)
                {
                    if (!map.Jobs.Contains(jobId) ||
                        displayedJobs.Contains(jobId) ||
                        !_prototypeManager.TryIndex(jobId, out var job) ||
                        !CanShowPlayerJoinableMapJob(job))
                    {
                        continue;
                    }

                    displayedJobs.Add(jobId);
                    jobs.Add(job);
                }

                if (jobs.Count > 0)
                    sections.Add((department, jobs.ToArray()));
            }

            if (sections.Count == 0)
                continue;

            AddPlayerJoinableMapSectionTitle(
                Loc.GetString("player-joinable-map-additional-title", ("map", Loc.GetString(map.DisplayName))),
                ref firstCategory);

            var firstMapDepartment = true;
            foreach (var (department, jobs) in sections)
            {
                AddPlayerJoinableMapDepartmentJobs(
                    department,
                    jobs,
                    $"{map.ID}-{department.ID}",
                    priorityItems,
                    sponsorPrototypes,
                    ref firstCategory,
                    firstMapDepartment);
                firstMapDepartment = false;
            }
        }
    }

    private void AddPlayerJoinableMapSectionTitle(string title, ref bool firstCategory)
    {
        if (!firstCategory)
        {
            JobList.AddChild(new Control
            {
                MinSize = new Vector2(0, 23),
            });
        }

        firstCategory = false;
        JobList.AddChild(new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#3f6658") },
            Children =
            {
                new Label
                {
                    Text = title,
                    Margin = new Thickness(5f, 0, 0, 0),
                },
            },
        });
    }

    private void AddPlayerJoinableMapDepartmentJobs(
        DepartmentPrototype department,
        JobPrototype[] jobs,
        string categoryId,
        (string LocKey, int Priority)[] priorityItems,
        string[] sponsorPrototypes,
        ref bool firstCategory,
        bool firstMapDepartment)
    {
        if (jobs.Length == 0)
            return;

        var departmentName = Loc.GetString(department.Name);
        if (!_jobCategories.TryGetValue(categoryId, out var category))
        {
            if (firstCategory)
            {
                firstCategory = false;
            }
            else if (!firstMapDepartment)
            {
                JobList.AddChild(new Control
                {
                    MinSize = new Vector2(0, 23),
                });
            }

            category = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = categoryId,
                ToolTip = Loc.GetString(
                    "humanoid-profile-editor-jobs-amount-in-department-tooltip",
                    ("departmentName", departmentName)),
            };

            category.AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#464966") },
                Children =
                {
                    new Label
                    {
                        Text = Loc.GetString(
                            "humanoid-profile-editor-department-jobs-label",
                            ("departmentName", departmentName)),
                        Margin = new Thickness(5f, 0, 0, 0),
                    },
                },
            });

            _jobCategories[categoryId] = category;
            JobList.AddChild(category);
        }

        Array.Sort(jobs, JobUIComparer.Instance);

        foreach (var job in jobs)
        {
            var jobContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
            };

            var selector = new RequirementsSelector
            {
                Margin = new Thickness(3f, 3f, 3f, 0f),
            };
            selector.OnOpenGuidebook += OnOpenGuidebook;

            var icon = new TextureRect
            {
                TextureScale = new Vector2(2, 2),
                VerticalAlignment = VAlignment.Center,
            };
            var jobIcon = _prototypeManager.Index(job.Icon);
            icon.Texture = _sprite.Frame0(jobIcon.Icon);
            selector.Setup(
                priorityItems,
                job.LocalizedName,
                200,
                job.LocalizedDescription,
                icon,
                job.Guides);

            if (!_requirements.IsAllowed(
                    job,
                    (HumanoidCharacterProfile?) _preferencesManager.Preferences?.SelectedCharacter,
                    out var reason))
            {
                selector.LockRequirements(reason);
            }
            else
            {
                selector.UnlockRequirements();
            }

            selector.OnSelected += selectedPriority =>
            {
                var selectedJobPriority = (JobPriority) selectedPriority;
                Profile = Profile?.WithJobPriority(job.ID, selectedJobPriority);

                foreach (var (jobId, other) in _jobPriorities)
                {
                    if (jobId == job.ID)
                    {
                        other.Select(selectedPriority);
                        continue;
                    }

                    if (selectedJobPriority != JobPriority.High ||
                        (JobPriority) other.Selected != JobPriority.High)
                    {
                        continue;
                    }

                    other.Select((int) JobPriority.Medium);
                    Profile = Profile?.WithJobPriority(jobId, JobPriority.Medium);
                }

                ReloadPreview();
                UpdateJobPriorities();
                SetDirty();
            };

            var loadoutWindowButton = new Button
            {
                Text = Loc.GetString("loadout-window"),
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Center,
                Margin = new Thickness(3f, 3f, 0f, 0f),
            };

            var collection = IoCManager.Instance!;
            var prototypeManager = collection.Resolve<IPrototypeManager>();
            var jobLoadoutId = LoadoutSystem.GetJobPrototype(job.ID);
            var effectiveJobLoadoutId = LoadoutSystem.GetEffectiveRolePrototype(jobLoadoutId, prototypeManager);

            if (!prototypeManager.TryIndex<RoleLoadoutPrototype>(effectiveJobLoadoutId, out var roleLoadoutPrototype))
            {
                loadoutWindowButton.Disabled = true;
            }
            else
            {
                loadoutWindowButton.OnPressed += _ =>
                {
                    RoleLoadout? loadout = null;
                    Profile?.Loadouts.TryGetValue(jobLoadoutId, out loadout);
                    loadout = loadout?.Clone();

                    if (loadout == null)
                    {
                        loadout = new RoleLoadout(jobLoadoutId);
                        loadout.SetDefault(
                            Profile,
                            _playerManager.LocalSession,
                            _prototypeManager,
                            sponsorPrototypes);
                    }

                    OpenLoadout(job, loadout, roleLoadoutPrototype);
                };
            }

            _jobPriorities.Add((job.ID, selector));
            jobContainer.AddChild(selector);

            if (job.AlternativeTitles.Count > 0)
            {
                var alternativeTitleButton = new OptionButton
                {
                    Margin = new Thickness(5f, 0f),
                };

                alternativeTitleButton.AddItem(job.LocalizedName, 0);
                for (var i = 0; i < job.AlternativeTitles.Count; i++)
                {
                    alternativeTitleButton.AddItem(
                        Loc.GetString(job.AlternativeTitles[i]),
                        i + 1);
                }

                if (Profile != null &&
                    Profile.JobAlternativeTitles.TryGetValue(job.ID, out var savedAlternativeTitle))
                {
                    var index = job.AlternativeTitles.IndexOf(savedAlternativeTitle);
                    if (index >= 0)
                        alternativeTitleButton.SelectId(index + 1);
                }

                alternativeTitleButton.OnItemSelected += args =>
                {
                    alternativeTitleButton.SelectId(args.Id);
                    if (args.Id == 0)
                        Profile = Profile?.WithJobAlternativeTitle(job.ID, null);
                    else
                        Profile = Profile?.WithJobAlternativeTitle(job.ID, job.AlternativeTitles[args.Id - 1]);

                    SetDirty();
                };

                selector.ReplaceTitleWith(alternativeTitleButton);
            }

            jobContainer.AddChild(loadoutWindowButton);
            category.AddChild(jobContainer);
        }
    }

    private List<DepartmentPrototype> GetPlayerJoinableMapDepartments()
    {
        var departments = new List<DepartmentPrototype>();
        foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (department.EditorHidden)
                continue;

            if (department.Roles.Any(jobId =>
                    _prototypeManager.TryIndex(jobId, out var job) &&
                    CanShowPlayerJoinableMapJob(job)))
            {
                departments.Add(department);
            }
        }

        departments.Sort(DepartmentUIComparer.Instance);
        return departments;
    }

    private void RefreshPlayerJoinableMapAccess()
    {
        _lastPlayerCount = _playerManager.PlayerCount;
        _availablePlayerJoinableMaps.Clear();
        _availablePlayerJoinableMapJobs.Clear();

        foreach (var map in _playerJoinableMapIndex.Maps)
        {
            if (!PlayerJoinableMapAccess.IsEnabled(map, _cfgManager, _lastPlayerCount))
                continue;

            var hasVisibleJob = false;
            foreach (var jobId in map.Jobs)
            {
                _availablePlayerJoinableMapJobs.Add(jobId);
                if (_prototypeManager.TryIndex(jobId, out JobPrototype? job) && job.SetPreference)
                    hasVisibleJob = true;
            }

            if (hasVisibleJob)
                _availablePlayerJoinableMaps.Add(map);
        }
    }

    private bool CanShowPlayerJoinableMapJob(JobPrototype job)
    {
        return job.SetPreference &&
            _availablePlayerJoinableMapJobs.Contains(job.ID);
    }

    private void SubscribePlayerJoinableMapCVars()
    {
        foreach (var map in _playerJoinableMapIndex.Maps)
        {
            SubscribePlayerJoinableMapBoolCVar(PlayerJoinableMapAccess.GetEnabledCVar(map));
            SubscribePlayerJoinableMapIntCVar(PlayerJoinableMapAccess.GetMinPlayersCVar(map));
        }
    }

    private void UnsubscribePlayerJoinableMapCVars()
    {
        foreach (var (cvar, handler) in _playerJoinableMapBoolCVarHandlers)
            _cfgManager.UnsubValueChanged(cvar, handler);

        foreach (var (cvar, handler) in _playerJoinableMapIntCVarHandlers)
            _cfgManager.UnsubValueChanged(cvar, handler);

        _playerJoinableMapBoolCVarHandlers.Clear();
        _playerJoinableMapIntCVarHandlers.Clear();
    }

    private void SubscribePlayerJoinableMapBoolCVar(CVarDef<bool>? cvar)
    {
        if (cvar == null || _playerJoinableMapBoolCVarHandlers.ContainsKey(cvar))
            return;

        Action<bool> handler = _ => RefreshJobs();
        _cfgManager.OnValueChanged(cvar, handler);
        _playerJoinableMapBoolCVarHandlers.Add(cvar, handler);
    }

    private void SubscribePlayerJoinableMapIntCVar(CVarDef<int>? cvar)
    {
        if (cvar == null || _playerJoinableMapIntCVarHandlers.ContainsKey(cvar))
            return;

        Action<int> handler = _ => RefreshJobs();
        _cfgManager.OnValueChanged(cvar, handler);
        _playerJoinableMapIntCVarHandlers.Add(cvar, handler);
    }

    private void OnPlayerJoinableMapPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (_lastPlayerCount == _playerManager.PlayerCount)
            return;

        _lastPlayerCount = _playerManager.PlayerCount;
        RefreshJobs();
    }

    private void OnPlayerJoinableMapPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        UnsubscribePlayerJoinableMapCVars();
        _playerJoinableMapIndex.Rebuild(_prototypeManager);
        SubscribePlayerJoinableMapCVars();
        RefreshJobs();
    }
}
