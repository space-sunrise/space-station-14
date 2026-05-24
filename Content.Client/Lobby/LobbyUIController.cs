using System.Linq;
using Content.Client._Sunrise.Lobby.UI;
using Content.Client._Sunrise.Pets;
using Content.Client.Body;
using Content.Client.Guidebook;
using Content.Shared._Sunrise.Pets;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby.UI;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Station;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Sunrise.Interfaces.Shared;
using Content.Client._Sunrise.PlayerCache; // Sunrise-Sponsors

namespace Content.Client.Lobby;

public sealed partial class LobbyUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IFileDialogManager _dialogManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly JobRequirementsManager _requirements = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly PlayerCacheManager _playerCache = default!;
    [UISystemDependency] private readonly GuidebookSystem _guide = default!;
    [UISystemDependency] private readonly PetSelectionSystem _petSelectionSystem = default!;

    private CharacterSetupGui? _characterSetup;
    private HumanoidProfileEditor? _profileEditor;
    private CharacterSetupGuiSavePanel? _savePanel;
    private PetSelectionMenu? _petSelectionMenu;
    private ISharedSponsorsManager? _sponsorsManager; // Sunrise-Sponsors

    /// <summary>
    /// This is the characher preview panel in the chat. This should only update if their character updates.
    /// </summary>
    private LobbyCharacterPreviewPanel? PreviewPanel => GetLobbyPreview();

    private LobbyPetPreviewPanel? PetPreviewPanel => GetLobbyPetPreview();

    /// <summary>
    /// This is the modified profile currently being edited.
    /// </summary>
    private HumanoidCharacterProfile? EditedProfile => _profileEditor?.Profile;

    private int? EditedSlot => _profileEditor?.CharacterSlot;

    public override void Initialize()
    {
        base.Initialize();
        IoCManager.Instance!.TryResolveType(out _sponsorsManager); // Sunrise-Sponsors
        _prototypeManager.PrototypesReloaded += OnProtoReload;
        _preferencesManager.OnServerDataLoaded += PreferencesDataLoaded;
        _requirements.Updated += OnRequirementsUpdated;

        _configurationManager.OnValueChanged(SunriseCCVars.SponsorPet, _ => RefreshPetPreview());

        _configurationManager.OnValueChanged(CCVars.FlavorText, args =>
        {
            _profileEditor?.RefreshFlavorText();
        });
        // Sunrise-Start
        _configurationManager.OnValueChanged(SunriseCCVars.FlavorTextSponsorOnly, args =>
        {
            _profileEditor?.RefreshFlavorText();
        });
        // Sunrise-End

        _configurationManager.OnValueChanged(CCVars.GameRoleTimers, _ => RefreshProfileEditor());
        _configurationManager.OnValueChanged(CCVars.GameRoleLoadoutTimers, _ => RefreshProfileEditor());

        _configurationManager.OnValueChanged(CCVars.GameRoleWhitelist, _ => RefreshProfileEditor());
    }

    private LobbyCharacterPreviewPanel? GetLobbyPreview()
    {
        if (_stateManager.CurrentState is LobbyState lobby)
        {
            return lobby.Lobby?.CharacterPreview;
        }

        return null;
    }

    private LobbyPetPreviewPanel? GetLobbyPetPreview()
    {
        // Sunrise-Edit
        // if (_stateManager.CurrentState is LobbyState lobby)
        // {
        //     return lobby.Lobby?.PetPreview;
        // }
        return null;
    }

    private void OnRequirementsUpdated()
    {
        if (_profileEditor != null)
        {
            _profileEditor.RefreshAntags();
            _profileEditor.RefreshJobs();
        }
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (_profileEditor != null)
        {
            if (obj.WasModified<AntagPrototype>())
            {
                _profileEditor.RefreshAntags();
            }

            if (obj.WasModified<JobPrototype>() ||
                obj.WasModified<DepartmentPrototype>())
            {
                _profileEditor.RefreshJobs();
            }

            if (obj.WasModified<LoadoutPrototype>() ||
                obj.WasModified<LoadoutGroupPrototype>() ||
                obj.WasModified<RoleLoadoutPrototype>())
            {
                _profileEditor.RefreshLoadouts();
            }

            if (obj.WasModified<SpeciesPrototype>())
            {
                _profileEditor.RefreshSpecies();
            }

            if (obj.WasModified<TraitPrototype>())
            {
                _profileEditor.RefreshTraits();
            }
        }
    }

    private void PreferencesDataLoaded()
    {
        PreviewPanel?.SetLoaded(true);

        if (_stateManager.CurrentState is not LobbyState)
            return;

        RefreshLobbyPreview();

        if (_characterSetup?.Visible == true)
            ReloadCharacterSetup();

        RefreshPetPreview();
    }

    public void OnStateEntered(LobbyState state)
    {
        PreviewPanel?.SetLoaded(_preferencesManager.ServerDataLoaded);
        RefreshLobbyPreview();

        if (_characterSetup?.Visible == true)
            ReloadCharacterSetup();

        RefreshPetPreview();
    }

    public void OnStateExited(LobbyState state)
    {
        PreviewPanel?.SetLoaded(false);
        _profileEditor?.Dispose();
        _characterSetup?.Dispose();

        _characterSetup = null;
        _profileEditor = null;
    }

    /// <summary>
    /// Reloads every single character setup control.
    /// </summary>
    public void ReloadCharacterSetup()
    {
        RefreshLobbyPreview();
        var (characterGui, profileEditor) = EnsureGui();
        characterGui.ReloadCharacterPickers();
        profileEditor.SetProfile(
            (HumanoidCharacterProfile?) _preferencesManager.Preferences?.SelectedCharacter,
            _preferencesManager.Preferences?.SelectedCharacterIndex);
    }

    /// <summary>
    /// Refreshes the character preview in the lobby chat.
    /// </summary>
    private void RefreshLobbyPreview()
    {
        if (PreviewPanel == null)
            return;

        // Get selected character, load it, then set it
        var character = _preferencesManager.Preferences?.SelectedCharacter;

        if (character is not HumanoidCharacterProfile humanoid)
        {
            PreviewPanel.ProfilePreviewSpriteView.ClearPreview();
            PreviewPanel.SetSummaryText(string.Empty);
            return;
        }

        PreviewPanel.ProfilePreviewSpriteView.LoadPreview(humanoid);
        PreviewPanel.SetSummaryText(humanoid.Summary);
    }

    private void RefreshPetPreview()
    {
        if (PreviewPanel == null)
            return;

        var currentPetSelection = GetCurrentPetSelection();

        PreviewPanel.OnChangePetRequested -= OpenPetPanel;
        PreviewPanel.OnChangePetRequested += OpenPetPanel;

        EntityUid? petDummy = null;
        if (!string.IsNullOrEmpty(currentPetSelection) &&
            _prototypeManager.TryIndex<PetSelectionPrototype>(currentPetSelection, out var petSelectionPrototype))
        {
            petDummy = EntityManager.SpawnEntity(petSelectionPrototype.PetEntity, MapCoordinates.Nullspace);
        }

        PreviewPanel.SetPetSprite(petDummy);
    }

    private void OpenPetPanel()
    {
        if (_petSelectionMenu is { IsOpen: true })
            return;

        _petSelectionMenu = new PetSelectionMenu();

        _petSelectionMenu.UpdateState();
        _petSelectionMenu.OnIdSelected += OnPetSelectionChanged;

        _petSelectionMenu.OpenCentered();
    }

    private void OnPetSelectionChanged(string selectedPet)
    {
        RefreshPetPreview();
        _petSelectionSystem.PetSelectionSelected(selectedPet);
    }

    private string? GetCurrentPetSelection()
    {
        return _playerCache.TryGetCachedPet(out var pet) ? pet : null;
    }

    private void RefreshProfileEditor()
    {
        _profileEditor?.RefreshAntags();
        _profileEditor?.RefreshJobs();
        _profileEditor?.RefreshLoadouts();
    }

    private void SaveProfile()
    {
        DebugTools.Assert(EditedProfile != null);

        if (EditedProfile == null || EditedSlot == null)
            return;

        var selected = _preferencesManager.Preferences?.SelectedCharacterIndex;

        if (selected == null)
            return;

        _preferencesManager.UpdateCharacter(EditedProfile, EditedSlot.Value);
        ReloadCharacterSetup();
    }

    private void CloseProfileEditor()
    {
        if (_profileEditor == null)
            return;

        _profileEditor.SetProfile(null, null);
        _profileEditor.Visible = false;

        if (_stateManager.CurrentState is LobbyState lobbyGui)
        {
            lobbyGui.SwitchState(SunriseLobbyGui.LobbyGuiState.Default);
        }
    }

    private void OpenSavePanel()
    {
        if (_savePanel is { IsOpen: true })
            return;

        _savePanel = new CharacterSetupGuiSavePanel();

        _savePanel.SaveButton.OnPressed += _ =>
        {
            SaveProfile();

            _savePanel.Close();

            CloseProfileEditor();
        };

        _savePanel.NoSaveButton.OnPressed += _ =>
        {
            _savePanel.Close();

            CloseProfileEditor();
        };

        _savePanel.OpenCentered();
    }

    private (CharacterSetupGui, HumanoidProfileEditor) EnsureGui()
    {
        if (_characterSetup != null && _profileEditor != null)
        {
            _characterSetup.Visible = true;
            _profileEditor.Visible = true;
            return (_characterSetup, _profileEditor);
        }

        _profileEditor = new HumanoidProfileEditor(
            _preferencesManager,
            _configurationManager,
            EntityManager,
            _dialogManager,
            LogManager,
            _playerManager,
            _prototypeManager,
            _resourceCache,
            _requirements,
            _markings);

        _profileEditor.OnOpenGuidebook += _guide.OpenHelp;

        _characterSetup = new CharacterSetupGui(_profileEditor);

        _characterSetup.CloseButton.OnPressed += _ =>
        {
            // Open the save panel if we have unsaved changes.
            if (_profileEditor.Profile != null && _profileEditor.IsDirty)
            {
                OpenSavePanel();

                return;
            }

            // Reset sliders etc.
            CloseProfileEditor();
        };

        _profileEditor.Save += SaveProfile;

        _characterSetup.SelectCharacter += args =>
        {
            _preferencesManager.SelectCharacter(args);
            ReloadCharacterSetup();
        };

        _characterSetup.DeleteCharacter += args =>
        {
            _preferencesManager.DeleteCharacter(args);

            // Reload everything
            if (EditedSlot == args)
            {
                ReloadCharacterSetup();
            }
            else
            {
                // Only need to reload character pickers
                _characterSetup?.ReloadCharacterPickers();
            }
        };

        if (_stateManager.CurrentState is LobbyState lobby)
        {
            lobby.Lobby?.CharacterSetupState.AddChild(_characterSetup);
        }

        return (_characterSetup, _profileEditor);
    }

    public EntityUid LoadProfileEntity(HumanoidCharacterProfile? humanoid, JobPrototype? job, bool jobClothes)
    {
        var sponsorPrototypes = _sponsorsManager?.GetClientPrototypes().ToArray() ?? [];

        EntProtoId? previewEntity = null;
        if (humanoid != null && jobClothes)
        {
            job ??= GetPreferredJob(humanoid);
            previewEntity = job.JobPreviewEntity ?? (EntProtoId?) job.JobEntity;
        }

        EntityUid dummyEnt;
        if (previewEntity != null)
        {
            dummyEnt = EntityManager.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
        }
        else if (humanoid is not null)
        {
            var dummy = _prototypeManager.Index(humanoid.Species).DollPrototype;
            dummyEnt = EntityManager.SpawnEntity(dummy, MapCoordinates.Nullspace);
            EntityManager.System<SharedVisualBodySystem>().ApplyProfileTo(dummyEnt, humanoid);
            EntityManager.System<HumanoidAppearanceSystem>().LoadProfile(dummyEnt, humanoid);
        }
        else
        {
            dummyEnt = EntityManager.SpawnEntity(_prototypeManager.Index(HumanoidCharacterProfile.DefaultSpecies).DollPrototype, MapCoordinates.Nullspace);
        }

        if (humanoid != null && jobClothes)
        {
            DebugTools.Assert(job != null);
            GiveDummyJobClothes(dummyEnt, humanoid, job);

            var jobLoadoutId = LoadoutSystem.GetJobPrototype(job.ID);
            var effectiveJobLoadoutId = LoadoutSystem.GetEffectiveRolePrototype(jobLoadoutId, _prototypeManager);
            if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(effectiveJobLoadoutId))
            {
                var loadout = humanoid.GetLoadoutOrDefault(jobLoadoutId, _playerManager.LocalSession, humanoid.Species, EntityManager, _prototypeManager, sponsorPrototypes);
                GiveDummyLoadout(dummyEnt, loadout, jobClothes);
            }
        }

        return dummyEnt;
    }

    public void GiveDummyLoadout(EntityUid dummyEnt, RoleLoadout? roleLoadout, bool outerwear)
    {
        if (roleLoadout == null)
            return;

        var spawnSys = EntityManager.System<StationSpawningSystem>();
        var underwearSlots = new HashSet<string> { "bra", "pants", "socks" };

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                    continue;

                var wear = true;
                foreach (var (slot, _) in loadoutProto.Equipment)
                {
                    if (!underwearSlots.Contains(slot) && !outerwear)
                        wear = false;
                }

                if (wear)
                    spawnSys.EquipStartingGear(dummyEnt, loadoutProto);
            }
        }
    }

    public void GiveDummyJobClothes(EntityUid dummyEnt, HumanoidCharacterProfile profile, JobPrototype job)
    {
        var inventorySys = EntityManager.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(dummyEnt, out var slots))
            return;

        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                        continue;

                    foreach (var slot in slots)
                    {
                        if (_prototypeManager.Resolve(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(dummyEnt, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                                EntityManager.DeleteEntity(unequippedItem.Value);

                            if (itemType != string.Empty)
                            {
                                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(dummyEnt, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(dummyEnt, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                                EntityManager.DeleteEntity(unequippedItem.Value);

                            if (itemType != string.Empty)
                            {
                                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(dummyEnt, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.Resolve(job.StartingGear, out var gear))
            return;

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);

            if (inventorySys.TryUnequip(dummyEnt, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                EntityManager.DeleteEntity(unequippedItem.Value);

            if (itemType != string.Empty)
            {
                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                inventorySys.TryEquip(dummyEnt, item, slot.Name, true, true);
            }
        }
    }

    private JobPrototype GetPreferredJob(HumanoidCharacterProfile profile)
    {
        var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;
        return _prototypeManager.Index<JobPrototype>(highPriorityJob.Id ?? SharedGameTicker.FallbackOverflowJob);
    }
}
