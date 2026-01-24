using System.Linq;
using System.Threading.Tasks;
using Content.Client.UserInterface.Systems.Ghost.Controls.PlanetPrison;
using Content.Shared._Sunrise.PlanetPrison;
using Content.Shared.Maps;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using Robust.Shared;

namespace Content.Client._Sunrise.PlanetPrison;

public sealed class PlanetPrisonUISystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    private PlanetPrisonWindow? _window;
    private PlanetPrisonMapEntry? _metusMapEntry;
    private PlanetPrisonMapEntry? _noxMapEntry;
    private System.Threading.CancellationTokenSource? _timerCancellation;

    // Отдельное состояние для каждой карты
    private readonly Dictionary<string, bool> _hasVoted = new();
    private readonly Dictionary<string, PlanetPrisonMapEntry.PriorityLevel> _localPriority = new();
    private readonly Dictionary<string, bool> _mapLaunched = new();
    private readonly Dictionary<string, int> _lastVoteCount = new();
    private readonly Dictionary<int, string> _mapIdToProto = new(); // Соответствие MapId -> protoId
    private int _totalPriorityPlayers = 0; // Общее количество игроков с приоритетами
    private bool _initialStateRequested = false; // Флаг, запрашивалось ли начальное состояние

    public event Action<bool>? PrisonButtonHighlightChanged;

    public override void Initialize()
    {
        base.Initialize();

        // Создаем окно сразу при инициализации системы
        _window = new PlanetPrisonWindow();
        _window.MapsTabPressed += OnMapsTabPressed;
        _window.RolesTabPressed += OnRolesTabPressed;

        SubscribeNetworkEvent<PlanetPrisonVoteUpdateEvent>(OnPrisonVoteUpdate);
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);


        PopulateMaps();
        PopulateRoles();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _timerCancellation?.Cancel();

        if (_window != null)
        {
            _window.MapsTabPressed -= OnMapsTabPressed;
            _window.RolesTabPressed -= OnRolesTabPressed;
            _window.Dispose();
            _window = null;
        }

    }

    public void OpenWindow()
    {
        Logger.Info("PlanetPrisonUISystem: OpenWindow called");
        if (_window == null)
        {
            Logger.Error("PlanetPrisonUISystem: Window is null!");
            return;
        }

        // Обновляем данные перед открытием окна
        PopulateMaps();
        PopulateRoles();

        _window.OpenCentered();
        Logger.Info("PlanetPrisonUISystem: Window opened");

        // Всегда запрашиваем актуальный статус при открытии окна
        // Это гарантирует корректное отображение после любых изменений
        RequestPrisonVoteStatus();
    }

    private void OnMapsTabPressed()
    {
        // Обработка переключения на вкладку карт
        // PopulateMaps() не вызываем, чтобы не сбросить состояние
        // PopulateMaps() вызывается только при первом открытии окна
    }

    private void OnRolesTabPressed()
    {
        // Обработка переключения на вкладку ролей
        // PopulateRoles() не вызываем, чтобы не сбросить состояние
        // PopulateRoles() вызывается только при первом открытии окна
    }

    private void PopulateMaps()
    {
        if (_window == null)
            return;

        _window.ClearMaps();

        // Создаем карту Metus
        _metusMapEntry = new PlanetPrisonMapEntry(
            Loc.GetString("planet-prison-map-metus-title"),
            Loc.GetString("planet-prison-map-metus-description")
        );

        _metusMapEntry.PrioritySelected += (priority) => OnMapPrioritySelected("PlanetPrison", priority);
        _window.AddMapEntry(_metusMapEntry);

        // Создаем карту Nox
        _noxMapEntry = new PlanetPrisonMapEntry(
            Loc.GetString("planet-prison-map-nox-title"),
            Loc.GetString("planet-prison-map-nox-description")
        );

        _noxMapEntry.PrioritySelected += (priority) => OnMapPrioritySelected("PlanetPrisonOld", priority);
        _window.AddMapEntry(_noxMapEntry);

        // Настраиваем каждую карту отдельно
        SetupMapEntry("PlanetPrison", _metusMapEntry);
        SetupMapEntry("PlanetPrisonOld", _noxMapEntry);

        Logger.Info("Added Metus and Nox map entries");
    }

    private void UpdatePriorityCounter()
    {
        if (_window == null) return;

        // Счетчик всегда виден под вкладкой карт
        _window.GetPriorityCounterLabel().Text = Loc.GetString("planet-prison-priority-required",
            ("current", _totalPriorityPlayers), ("required", 2));
        _window.GetPriorityCounterPanel().Visible = true;

        // Устанавливаем цвет фона
        var panel = _window.GetPriorityCounterPanel();
        if (panel.PanelOverride == null)
        {
            // Создаем новый StyleBoxFlat с нужным цветом
            var styleBox = new StyleBoxFlat { BackgroundColor = Color.FromHex("#202023") };
            panel.PanelOverride = styleBox;
        }
    }

    private int GetRequiredVotes(string mapId)
    {
        return 2; // Все карты требуют 2 голоса
    }

    private void SetupMapEntry(string mapId, PlanetPrisonMapEntry? entry)
    {
        if (entry == null) return;

        // Инициализируем состояния если они не существуют
        if (!_localPriority.ContainsKey(mapId))
            _localPriority[mapId] = PlanetPrisonMapEntry.PriorityLevel.Never;
        if (!_hasVoted.ContainsKey(mapId))
            _hasVoted[mapId] = false;
        if (!_mapLaunched.ContainsKey(mapId))
            _mapLaunched[mapId] = false;
        if (!_lastVoteCount.ContainsKey(mapId))
            _lastVoteCount[mapId] = 0;

        // Устанавливаем сохраненный приоритет
        entry.SetSelectedPriority(_localPriority[mapId]);

        // Восстанавливаем состояние
        if (_mapLaunched.ContainsKey(mapId) && _mapLaunched[mapId])
        {
            // Карта запущена - показываем статус
            entry.ShowLaunchedStatus();
            entry.DisableButtons();
        }
        else
        {
            // Карта не запущена - делаем её активной
            entry.HideStatus();
            entry.HideVoteCount();
            entry.EnableButtons();
        }
    }

    private void OnMapPrioritySelected(string mapId, PlanetPrisonMapEntry.PriorityLevel priority)
    {
        Logger.Info($"{mapId} priority selected: {priority}");

        _localPriority[mapId] = priority;
        // Не устанавливаем _hasVoted[mapId] = true здесь - кнопки блокируются только при запуске голосования

        // Отправляем голос на сервер (не блокируем кнопки локально)
        var message = new PlanetPrisonVoteMessage(mapId, (int)priority);
        RaiseNetworkEvent(message);
    }

    private void OnPrisonVoteUpdate(PlanetPrisonVoteUpdateEvent msg)
    {
        Logger.Info($"Prison vote update: {msg.MapId}, votes: {msg.VoteCount}, voting: {msg.IsVoting}, time: {msg.RemainingSeconds}, total players: {msg.TotalPriorityPlayers}");

        // Сохраняем количество голосов для конкретной карты
        _lastVoteCount[msg.MapId] = msg.VoteCount;
        _totalPriorityPlayers = msg.TotalPriorityPlayers;

        // Обновляем счетчик приоритетов
        UpdatePriorityCounter();

        // Специальная обработка для только что запущенной карты (RemainingSeconds = 0)
        if (msg.RemainingSeconds.HasValue && msg.RemainingSeconds.Value == 0 && !msg.IsVoting)
        {
            Logger.Info($"Map {msg.MapId} just launched (RemainingSeconds=0)");
            _mapLaunched[msg.MapId] = true;

            // Сбрасываем состояния голосования (но не статусы запущенных карт)
            ResetVotingStatesOnly();
            // Принудительно обновляем UI для всех карт
            SetupMapEntry("PlanetPrison", _metusMapEntry);
            SetupMapEntry("PlanetPrisonOld", _noxMapEntry);
        }

        // Обновляем UI для карты
        var entry = msg.MapId == "PlanetPrison" ? _metusMapEntry : _noxMapEntry;
        if (entry != null)
        {
            UpdateMapEntryUI(msg, entry);
        }

        // Определяем какую карту обновлять
        PlanetPrisonMapEntry? targetEntry = null;
        if (msg.MapId == "PlanetPrison" && _metusMapEntry != null)
            targetEntry = _metusMapEntry;
        else if (msg.MapId == "PlanetPrisonOld" && _noxMapEntry != null)
            targetEntry = _noxMapEntry;

        if (targetEntry != null)
        {
            if (msg.IsVoting && msg.RemainingSeconds.HasValue)
            {
                // Во время голосования: блокируем кнопки (счетчик не показываем)
                targetEntry.HideVoteCount();
                targetEntry.DisableButtons(); // Блокируем все кнопки во время голосования

                if (msg.RemainingSeconds.Value > 0)
                {
                    // Показываем таймер
                    targetEntry.ShowTimer(msg.RemainingSeconds.Value);
                    targetEntry.HideStatus();
                    targetEntry.HideVoteCount();
                }
                else // RemainingSeconds == 0
                {
                    // Таймер закончился, показываем "(запускается)"
                    targetEntry.HideTimer();
                    targetEntry.HideVoteCount();
                    targetEntry.ShowLaunchingStatus();
                }

                UpdatePrisonButtonHighlight(true);

                // Проигрываем звук, если это начало голосования
                if (msg.RemainingSeconds.Value == 5)
                {
                    // _entManager.System<AudioSystem>().PlayGlobal("/Audio/Effects/beep.ogg", Filter.Local(), false, AudioParams.Default);
                }
            }
            else if (!msg.IsVoting && msg.VoteCount >= GetRequiredVotes(msg.MapId))
            {
                // Голосование завершено успешно - блокируем кнопки навсегда и показываем статус
                targetEntry.HideVoteCount();
                targetEntry.HideTimer();
                targetEntry.DisableButtons(); // Полная блокировка всех кнопок
                UpdatePrisonButtonHighlight(false);
                _hasVoted[msg.MapId] = true;

                // Показываем "(запускается)" желтым, затем "(запущен)" красным
                targetEntry.ShowLaunchingStatus();
                Robust.Shared.Timing.Timer.Spawn(2000, () => {
                    if (targetEntry != null)
                    {
                        targetEntry.ShowLaunchedStatus();
                        _mapLaunched[msg.MapId] = true; // Помечаем, что эта конкретная карта запущена

                        // При запуске карты сбрасываем ВСЕ приоритеты и состояния
                        ResetAllClientStates(msg.MapId); // Передаем ID запущенной карты
                        Logger.Info($"Map {msg.MapId} launched successfully, all states reset");
                    }
                });
            }
            else
            {
                // Голосование не активно
                targetEntry.HideTimer();

                if (_mapLaunched.ContainsKey(msg.MapId) && _mapLaunched[msg.MapId])
                {
                    // Карта запущена - показываем статус "(запущен)"
                    targetEntry.ShowLaunchedStatus();
                    targetEntry.HideVoteCount();
                    targetEntry.DisableButtons(); // Кнопки остаются заблокированными
                }
                else
                {
                    // Карта не запущена
                    targetEntry.HideStatus();

                    // Не показываем счетчики для отдельных карт
                    targetEntry.HideVoteCount();
                    if (!_hasVoted.ContainsKey(msg.MapId) || !_hasVoted[msg.MapId])
                    {
                        targetEntry.EnableButtons();
                    }
                    else
                    {
                        targetEntry.DisableButtons();
                    }
                }

                UpdatePrisonButtonHighlight(false);
            }
        }
    }


    private void UpdatePrisonButtonHighlight(bool highlight)
    {
        PrisonButtonHighlightChanged?.Invoke(highlight);
    }

    private void OnMapRemoved(MapRemovedEvent ev)
    {
        // При удалении карты тюрьмы сбрасываем статус ТОЛЬКО удаленной карты
        // Карты тюрьмы имеют ID >= 100
        var mapIdValue = (int)ev.MapId;
        if (mapIdValue >= 100) // Карта тюрьмы
        {
            // Определяем, какая карта была удалена по ID
            // Поскольку порядок запуска фиксированный (Metus первая, Nox вторая),
            // и они получают последовательные ID, используем эту логику
            string? removedProtoId = null;
            if (_mapLaunched["PlanetPrison"])
            {
                _mapLaunched["PlanetPrison"] = false;
                removedProtoId = "PlanetPrison";
                Logger.Info($"Metus map (ID: {mapIdValue}) removed, status reset");
            }
            else if (_mapLaunched["PlanetPrisonOld"])
            {
                _mapLaunched["PlanetPrisonOld"] = false;
                removedProtoId = "PlanetPrisonOld";
                Logger.Info($"Nox map (ID: {mapIdValue}) removed, status reset");
            }

            // Обновляем UI только для удаленной карты, другая остается запущенной
            if (removedProtoId == "PlanetPrison")
            {
                SetupMapEntry("PlanetPrison", _metusMapEntry);
            }
            else if (removedProtoId == "PlanetPrisonOld")
            {
                SetupMapEntry("PlanetPrisonOld", _noxMapEntry);
            }
        }
    }

    private void RequestPrisonVoteStatus()
    {
        // Отправляем запросы на получение текущего статуса голосования для всех карт
        foreach (var mapId in new[] { "PlanetPrison", "PlanetPrisonOld" })
        {
            var message = new PlanetPrisonStatusRequestMessage(mapId);
            RaiseNetworkEvent(message);
        }
    }

    // Метод для сброса состояния карты (вызывается при удалении карты)
    public void ResetMapState()
    {
        _initialStateRequested = false; // Позволяем заново запросить состояние
        ResetAllClientStates(null); // null означает сбросить все состояния
    }

    // Метод для сброса всех состояний при запуске карты
    private void ResetAllClientStates(string? launchedMapId)
    {
        _totalPriorityPlayers = 0; // Сбрасываем счетчик игроков

        // Сбрасываем состояние для всех карт, но НЕ трогаем статус _mapLaunched (запущенные карты остаются запущенными)
        foreach (var mapId in new[] { "PlanetPrison", "PlanetPrisonOld" })
        {
            Logger.Info($"Resetting client state for {mapId}: hasVoted={_hasVoted[mapId]} -> false, localPriority={_localPriority[mapId]} -> Never");

            _hasVoted[mapId] = false;
            // НЕ сбрасываем _mapLaunched - запущенные карты остаются запущенными до их удаления
            _lastVoteCount[mapId] = 0;
            _localPriority[mapId] = PlanetPrisonMapEntry.PriorityLevel.Never;
        }

        // Обновляем UI для каждой карты отдельно
        SetupMapEntry("PlanetPrison", _metusMapEntry);
        SetupMapEntry("PlanetPrisonOld", _noxMapEntry);

        UpdatePriorityCounter();
        Logger.Info($"Client states reset after launching {launchedMapId ?? "none (full reset)"}");
    }

    // Сбрасывает только состояния голосования, но сохраняет статусы запущенных карт
    private void ResetVotingStatesOnly()
    {
        _totalPriorityPlayers = 0;

        foreach (var mapId in new[] { "PlanetPrison", "PlanetPrisonOld" })
        {
            _hasVoted[mapId] = false;
            _lastVoteCount[mapId] = 0;
            _localPriority[mapId] = PlanetPrisonMapEntry.PriorityLevel.Never;
        }

        UpdatePriorityCounter();
        Logger.Info("Voting states reset, launched statuses preserved");
    }

    private void UpdateMapEntryUI(PlanetPrisonVoteUpdateEvent msg, PlanetPrisonMapEntry entry)
    {
        // Обработка только что запущенной карты - показываем финальный статус сразу
        if (!msg.IsVoting && msg.RemainingSeconds.HasValue && msg.RemainingSeconds.Value == 0)
        {
            entry.HideTimer();
            entry.HideVoteCount();
            entry.ShowLaunchingStatus();
            entry.DisableButtons();
            return;
        }

        if (msg.IsVoting && msg.RemainingSeconds.HasValue)
        {
            HandleVotingUI(msg, entry);
        }
        else
        {
            HandleNonVotingUI(msg, entry);
        }
    }

    private void HandleVotingUI(PlanetPrisonVoteUpdateEvent msg, PlanetPrisonMapEntry entry)
    {
        entry.DisableButtons();
        entry.HideVoteCount();
        entry.HideStatus();

        // При 1 секунде показываем "(запускается)" вместо цифры для плавного перехода
        if (msg.RemainingSeconds!.Value > 1)
        {
            entry.ShowTimer(msg.RemainingSeconds!.Value);
        }
        else
        {
            entry.HideTimer();
            entry.ShowLaunchingStatus();
        }
    }

    private void HandleNonVotingUI(PlanetPrisonVoteUpdateEvent msg, PlanetPrisonMapEntry entry)
    {
        entry.HideTimer();

        if (_mapLaunched.ContainsKey(msg.MapId) && _mapLaunched[msg.MapId])
        {
            // Карта уже запущена - блокируем интерфейс
            entry.ShowLaunchedStatus();
            entry.DisableButtons();
            entry.HideVoteCount();
        }
        else
        {
            // Карта доступна для голосования
            entry.HideStatus();
            entry.EnableButtons();

            if (msg.VoteCount > 0)
                entry.ShowVoteCount(msg.VoteCount, GetRequiredVotes(msg.MapId));
            else
                entry.HideVoteCount();
        }
    }

    private void PopulateRoles()
    {
        if (_window == null)
            return;

        _window.ClearRoles();

        // Пока что добавим заглушку - роли будут заполняться из PlanetPrisonStationComponent или отдельной системы
        var entry = new PlanetPrisonRoleEntry(
            Loc.GetString("planet-prison-role-placeholder-title"),
            Loc.GetString("planet-prison-role-placeholder-description")
        );
        _window.AddRoleEntry(entry);
    }
}
