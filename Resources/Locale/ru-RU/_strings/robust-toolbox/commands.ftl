### Localization for engine console commands


## generic command errors

cmd-invalid-arg-number-error = Недопустимое число аргументов.
cmd-parse-failure-integer = { $arg } не является допустимым integer.
cmd-parse-failure-float = { $arg } не является допустимым float.
cmd-parse-failure-bool = { $arg } не является допустимым bool.
cmd-parse-failure-uid = { $arg } не является допустимым UID сущности.
cmd-parse-failure-mapid = { $arg } не является допустимым MapId.
cmd-parse-failure-entity-exist = UID { $arg } не соответствует существующей сущности.
cmd-error-file-not-found = Не удалось найти файл: { $file }.
cmd-error-dir-not-found = Не удалось найти директорию: { $dir }.
cmd-failure-no-attached-entity = К этой оболочке не привязана никакая сущность.

## 'help' command

cmd-help-desc = Выводит общую справку или справку по определённой команде
cmd-help-help =
    Использование: help [имя команды]
    Если имя команды не будет указано, будет выведена общая справка. Если имя команды будет указано, будет выведена справка по этой команде.
cmd-help-no-args = Чтобы получить справку по определённой команде, используйте 'help <command>'. Для получения списка всех доступных команд используйте 'list'. Для поиска по командам используйте 'list <filter>'.
cmd-help-unknown = Неизвестная команда: { $command }
cmd-help-top = { $command } - { $description }
cmd-help-invalid-args = Недопустимое количество аргументов.
cmd-help-arg-cmdname = [имя команды]

## 'cvar' command

cmd-cvar-desc = Получает или устанавливает CVar.
cmd-cvar-help =
    Использование: cvar <name | ?> [значение]
    Если значение предоставлено, оно спарсится и сохранится как новое значение CVar.
    Если нет, отобразится текущее значение CVar.
    Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.
cmd-cvar-invalid-args = Должно быть представлено ровно один или два аргумента.
cmd-cvar-not-registered = CVar '{ $cvar }' не зарегистрирован. Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.
cmd-cvar-parse-error = Входное значение имеет неправильный формат для типа { $type }
cmd-cvar-compl-list = Список доступных CVar-ов
cmd-cvar-arg-name = <name | ?>
cmd-cvar-value-hidden = <value hidden>

## 'list' command

cmd-list-desc = Выводит список доступных команд с опциональным поисковым фильтром
cmd-list-help =
    Использование: list [фильтр]
    Выводит список всех доступных команд. Если был предоставлен аргумент, он будет использоваться для фильтрации команд по имени.
cmd-list-heading = SIDE NAME            DESC{ "\u000A" }-------------------------{ "\u000A" }
cmd-list-arg-filter = [фильтр]

## '>' command, aka remote exec

cmd-remoteexec-desc = Выполняет команду на стороне сервера
cmd-remoteexec-help =
    Использование: > <command> [arg] [arg] [arg...]
    Выполняет команду на стороне сервера. Это необходимо, если на клиенте имеется команда с таким же именем, так как при простом выполнении команды сначала будет запущена команда на клиенте.

## 'gc' command

cmd-gc-desc = Запускает GC (Garbage Collector, Сборка мусора)
cmd-gc-help =
    Использование: gc [поколение]
    Использует GC.Collect() для запуска Сборки мусора.
    Если был предоставлен аргумент, то он спарсится как номер поколения GC и используется GC.Collect(int).
    Используйте команду 'gfc' для проведения сборки мусора, со сжатием 'кучи больших объектов' (LOH-compacting).
cmd-gc-failed-parse = Не удалось спарсить аргумент.
cmd-gc-arg-generation = [поколение]

## 'gcf' command

cmd-gcf-desc = Запускает GC, полную, со сжатием 'кучи больших объектов' (LOH-compacting) и всего.
cmd-gcf-help =
    Использование: gcf
    Выполняет полный GC.Collect(2, GCCollectionMode.Forced, true, true) одновременно сжимая 'кучу больших объектов' LOH.
    Скорее всего, это приведёт к зависанию на сотни миллисекунд, имейте в виду.

## 'gc_mode' command

cmd-gc_mode-desc = Изменяет/отображает режим задержки GC
cmd-gc_mode-help =
    Использование: gc_mode [тип]
    Если аргумент не был предоставлен, вернётся текущий режим задержки GC.
    Если аргумент был пропущен, он спарсится как GCLatencyMode и будет установлен как режим задержки GC.
cmd-gc_mode-current = текущий режим задержки gc: { $prevMode }
cmd-gc_mode-possible = возможные режимы:
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-unknown = неизвестный режим задержки gc: { $arg }
cmd-gc_mode-attempt = попытка изменения режима задержки gc: { $prevMode } -> { $mode }
cmd-gc_mode-result = полученный режим задержки gc: { $mode }
cmd-gc_mode-arg-type = [тип]

## 'mem' command

cmd-mem-desc = Выводит информацию об управляемой памяти
cmd-mem-help = Использование: mem
cmd-mem-report =
    Размер кучи: { TOSTRING($heapSize, "N0") }
    Всего распределено: { TOSTRING($totalAllocated, "N0") }

## 'physics' command

cmd-physics-overlay = { $overlay } не является распознанным оверлеем

## 'lsasm' command

cmd-lsasm-desc = Выводит список загруженных сборок по контексту загрузки
cmd-lsasm-help = Использование: lsasm

## 'exec' command

cmd-exec-desc = Исполняет скриптовый файл из записываемых пользовательских данных игры
cmd-exec-help =
    Использование: exec <fileName>
    Каждая строка в файле выполняется как одна команда, если только она не начинается со знака #
cmd-exec-arg-filename = <fileName>

## 'dump_net_comps' command

cmd-dump_net_comps-desc = Выводит таблицу сетевых компонентов.
cmd-dump_net_comps-help = Использование: dump_net-comps
cmd-dump_net_comps-error-writeable = Регистрация всё ещё доступна для записи, сетевые идентификаторы не были сгенерированы.
cmd-dump_net_comps-header = Регистрации сетевых компонентов:

## 'dump_event_tables' command

cmd-dump_event_tables-desc = Выводит таблицы направленных событий для сущности.
cmd-dump_event_tables-help = Использование: dump_event_tables <entityUid>
cmd-dump_event_tables-missing-arg-entity = Отсутствует аргумент сущности
cmd-dump_event_tables-error-entity = Недопустимая сущность
cmd-dump_event_tables-arg-entity = <entityUid>

## 'monitor' command

cmd-monitor-desc = Переключение отладочного монитора в меню F3.
cmd-monitor-help =
    Использование: monitor <name>
    Возможные мониторы: { $monitors }
    Вы также можете использовать специальные значения "-all" и "+all", чтобы соответственно скрыть или показать все мониторы.
cmd-monitor-arg-monitor = <monitor>
cmd-monitor-invalid-name = Недопустимое имя монитора
cmd-monitor-arg-count = Отсутствует аргумент монитора
cmd-monitor-minus-all-hint = Скрывает все мониторы
cmd-monitor-plus-all-hint = Показывает все мониторы

## Mapping commands

cmd-set-ambient-light-desc = Позволяет установить эмбиентое освещение для указанной карты, в формате SRGB.
cmd-set-ambient-light-help = Использование: setambientlight [mapid] [r g b a]
cmd-set-ambient-light-parse = Не удалось спарсить аргументы как байтовые значения цветов.
cmd-savemap-desc = Сериализует карту на диск. Не будет сохранять карту после инициализации, если это не будет сделано принудительно.
cmd-savemap-help = Использование: savemap <MapID> <Path> [force]
cmd-savemap-not-exist = Целевая карта не существует.
cmd-savemap-init-warning = Попытка сохранить карту после инициализации без принудительного сохранения.
cmd-savemap-attempt = Попытка сохранить карту { $mapId } в { $path }.
cmd-savemap-success = Карта успешно сохранена.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Path>
cmd-hint-savemap-force = [bool]
cmd-loadmap-desc = Загружает карту с диска в игру.
cmd-loadmap-help = Использование: loadmap <MapID> <Path> [x] [y] [rotation] [consistentUids]
cmd-loadmap-nullspace = Невозможно загрузить в карту 0.
cmd-loadmap-exists = Карта { $mapId } уже существует.
cmd-loadmap-success = Карта { $mapId } была загружена из { $path }.
cmd-loadmap-error = При загрузке карты из { $path } произошла ошибка.
cmd-hint-loadmap-x-position = [x-position]
cmd-hint-loadmap-y-position = [y-position]
cmd-hint-loadmap-rotation = [rotation]
cmd-hint-loadmap-uids = [float]
cmd-hint-savebp-id = <Grid EntityID>

## 'flushcookies' command


# Примечание: команда flushcookies взята из Robust.Client.WebView, её нет в коде основного движка.

cmd-flushcookies-desc = Сброс хранилища CEF-cookie на диск
cmd-flushcookies-help =
    Это гарантирует правильное сохранение файлов cookie на диске в случае неаккуратного выключения.
    Имейте в виду, что фактическая операция является асинхронной.
cmd-ldrsc-desc = Предварительно кэширует ресурс.
cmd-guidump-desc = Выгружает дерево GUI в /guidump.txt в пользовательских данных.
cmd-guidump-help = Использование: guidump
cmd-uitest-desc = Открывает окно тестирования пользовательского интерфейса.
cmd-uitest-help = Использование: uitest
cmd-uitest2-desc = Открывает окно тестирования управления пользовательским интерфейсом.
cmd-uitest2-help = Использование: uitest2 <tab>
cmd-uitest2-arg-tab = <tab>
cmd-uitest2-error-args = Ожидался максимум один аргумент.
cmd-uitest2-error-tab = Неверная вкладка: '{ $value }'
cmd-uitest2-title = UITest2
cmd-setclipboard-desc = Устанавливает системный буфер обмена.
cmd-setclipboard-help = Использование: setclipboard <text>
cmd-getclipboard-desc = Получает системный буфер обмена.
cmd-getclipboard-help = Использование: Getclipboard
cmd-togglelight-desc = Переключает рендеринг освещения.
cmd-togglelight-help = Использование: togglelight
cmd-togglefov-desc = Переключает FOV для клиента.
cmd-togglefov-help = Использование: togglefov
cmd-togglehardfov-desc = Переключает жесткий FOV для клиента. (для отладки space-station-14#2353)
cmd-togglehardfov-help = Использование: togglehardfov
cmd-toggleshadows-desc = Переключает рендеринг теней.
cmd-toggleshadows-help = Использование: toggleshadows
cmd-togglelightbuf-desc = Переключает рендеринг освещения. Это включает тени, но не FOV.
cmd-togglelightbuf-help = Использование: togglelightbuf
cmd-chunkinfo-desc = Получает информацию о чанке под вашим курсором.
cmd-chunkinfo-help = Использование: chunkinfo
cmd-rldshader-desc = Перезагружает все шейдеры.
cmd-rldshader-help = Использование: rldshader
cmd-cldbglyr-desc = Переключает слои отладки FOV и освещения.
cmd-cldbglyr-help =
    Использование: cldbglyr <layer>: Переключить <layer>
    cldbglyr: Выключить все слои
cmd-key-info-desc = Выводит информацию о клавише.
cmd-key-info-help = Использование: keyinfo <Key>
cmd-bind-desc = Привязывает комбинацию клавиш к команде ввода.
cmd-bind-help =
    Использование: bind { cmd-bind-arg-key } { cmd-bind-arg-mode } { cmd-bind-arg-command }
    Обратите внимание, что это НЕ сохраняет привязки автоматически. Используйте команду 'svbind' для сохранения конфигурации привязок.
cmd-bind-arg-key = <KeyName>
cmd-bind-arg-mode = <BindMode>
cmd-bind-arg-command = <InputCommand>
cmd-net-draw-interp-desc = Переключает отладочное отображение сетевой интерполяции.
cmd-net-draw-interp-help = Использование: net_draw_interp
cmd-net-watch-ent-desc = Выводит все сетевые обновления для EntityId в консоль.
cmd-net-watch-ent-help = Использование: net_watchent <0|EntityUid>
cmd-net-refresh-desc = Запрашивает полное состояние сервера.
cmd-net-refresh-help = Использование: net_refresh
cmd-net-entity-report-desc = Переключает панель отчета сетевых сущностей.
cmd-net-entity-report-help = Использование: net_entityreport
cmd-fill-desc = Заполняет консоль для отладки.
cmd-fill-help = Заполняет консоль некоторым бессмыслицей для отладки.
cmd-cls-desc = Очищает консоль.
cmd-cls-help = Очищает консоль от всех сообщений.
cmd-sendgarbage-desc = Отправляет мусор на сервер.
cmd-sendgarbage-help = Сервер ответит "no u"
cmd-loadgrid-desc = Загружает сетку из файла в существующую карту.
cmd-loadgrid-help = Использование: loadgrid <MapID> <Path> [x y] [rotation] [storeUids]
cmd-loc-desc = Выводит абсолютное местоположение сущности игрока в консоль.
cmd-loc-help = Использование: loc
cmd-tpgrid-desc = Телепортирует сетку в новое место.
cmd-tpgrid-help = Использование: tpgrid <gridId> <X> <Y> [<MapId>]
cmd-rmgrid-desc = Удаляет сетку из карты. Нельзя удалить стандартную сетку.
cmd-rmgrid-help = Использование: rmgrid <gridId>
cmd-mapinit-desc = Запускает инициализацию карты на карте.
cmd-mapinit-help = Использование: mapinit <mapID>
cmd-lsmap-desc = Перечисляет карты.
cmd-lsmap-help = Использование: lsmap
cmd-lsgrid-desc = Перечисляет гриды.
cmd-lsgrid-help = Использование: lsgrid
cmd-addmap-desc = Добавляет новую пустую карту в раунд. Если mapID уже существует, эта команда ничего не делает.
cmd-addmap-help = Использование: addmap <mapID> [initialize]
cmd-rmmap-desc = Удаляет карту из мира. Нельзя удалить nullspace.
cmd-rmmap-help = Использование: rmmap <mapId>
cmd-savegrid-desc = Сохраняет сетку на диск.
cmd-savegrid-help = Использование: savegrid <gridID> <Path>
cmd-testbed-desc = Загружает тестовое поле физики на указанной карте.
cmd-testbed-help = Использование: testbed <mapid> <test>
cmd-saveconfig-desc = Сохраняет конфигурацию клиента в файл конфигурации.
cmd-saveconfig-help = Использование: saveconfig
cmd-addcomp-desc = Добавляет компонент к сущности.
cmd-addcomp-help = Использование: addcomp <uid> <componentName>
cmd-addcompc-desc = Добавляет компонент к сущности на клиенте.
cmd-addcompc-help = Использование: addcompc <uid> <componentName>
cmd-rmcomp-desc = Удаляет компонент из сущности.
cmd-rmcomp-help = Использование: rmcomp <uid> <componentName>
cmd-rmcompc-desc = Удаляет компонент из сущности на клиенте.
cmd-rmcompc-help = Использование: rmcompc <uid> <componentName>
cmd-addview-desc = Позволяет подписаться на отображение сущности для отладки.
cmd-addview-help = Использование: addview <entityUid>
cmd-addviewc-desc = Позволяет подписаться на отображение сущности на клиенте для отладки.
cmd-addviewc-help = Использование: addview <entityUid>
cmd-removeview-desc = Позволяет отписаться от отображения сущности для отладки.
cmd-removeview-help = Использование: removeview <entityUid>
cmd-loglevel-desc = Изменяет уровень логирования для указанного sawmill.
cmd-loglevel-help =
    Использование: loglevel <sawmill> <level>
    sawmill: Метка, предшествующая сообщениям журнала. Для которой вы устанавливаете уровень.
    level: Уровень журнала. Должен соответствовать одному из значений перечисления LogLevel.
cmd-testlog-desc = Записывает тестовый журнал в sawmill.
cmd-testlog-help =
    Использование: testlog <sawmill> <level> <message>
    sawmill: Метка, предшествующая зарегистрированному сообщению.
    level: Уровень журнала. Должен соответствовать одному из значений перечисления LogLevel.
    message: Сообщение, которое будет зарегистрировано. Оберните его в двойные кавычки, если хотите использовать пробелы.
cmd-vv-desc = Открывает переменные представления.
cmd-vv-help = Использование: vv <entity ID|IoC interface name|SIoC interface name>
cmd-showvelocities-desc = Показывает ваши угловые и линейные скорости.
cmd-showvelocities-help = Использование: showvelocities
cmd-setinputcontext-desc = Устанавливает активный контекст ввода.
cmd-setinputcontext-help = Использование: setinputcontext <context>
cmd-forall-desc = Выполняет команду для всех сущностей с заданным компонентом.
cmd-forall-help = Использование: forall <bql query> do <command...>
cmd-delete-desc = Удаляет сущность с указанным ID.
cmd-delete-help = Использование: delete <entity UID>
# System commands
cmd-showtime-desc = Показывает время сервера.
cmd-showtime-help = Использование: showtime
cmd-restart-desc = Аккуратно перезапускает сервер (не только раунд).
cmd-restart-help = Использование: restart
cmd-shutdown-desc = Аккуратно завершает работу сервера.
cmd-shutdown-help = Использование: shutdown
cmd-netaudit-desc = Выводит информацию о безопасности NetMsg.
cmd-netaudit-help = Использование: netaudit
# Player commands
cmd-tp-desc = Телепортирует игрока в любое место на раунде.
cmd-tp-help = Использование: tp <x> <y> [<mapID>]
cmd-tpto-desc = Телепортирует текущего игрока или указанных игроков/сущностей к местоположению первого игрока/сущности.
cmd-tpto-help = Использование: tpto <username|uid> [username|uid]...
cmd-tpto-destination-hint = место назначения (uid или username)
cmd-tpto-victim-hint = сущность для телепортации (uid или username)
cmd-tpto-parse-error = Не удалось найти сущность или игрока: { $str }
cmd-listplayers-desc = Перечисляет всех игроков, которые в данный момент подключены.
cmd-listplayers-help = Использование: listplayers
cmd-kick-desc = Изгоняет подключённого игрока с сервера, отключая его.
cmd-kick-help = Использование: kick <PlayerIndex> [<Reason>]
# Spin command
cmd-spin-desc = Заставляет сущность вращаться. По умолчанию сущность — родитель подключённого игрока.
cmd-spin-help = Использование: spin velocity [drag] [entityUid]
# Localization command
cmd-rldloc-desc = Перезагружает локализацию (клиент и сервер).
cmd-rldloc-help = Использование: rldloc
# Debug entity controls
cmd-spawn-desc = Создает сущность указанного типа.
cmd-spawn-help = Использование: spawn <prototype> ИЛИ spawn <prototype> <relative entity ID> ИЛИ spawn <prototype> <x> <y>
cmd-cspawn-desc = Создает клиентскую сущность указанного типа у ваших ног.
cmd-cspawn-help = Использование: cspawn <entity type>
cmd-scale-desc = Увеличивает или уменьшает размер сущности.
cmd-scale-help = Использование: scale <entityUid> <float>
cmd-dumpentities-desc = Выводит список сущностей.
cmd-dumpentities-help = Выводит список сущностей с их UID и прототипом.
cmd-getcomponentregistration-desc = Получает информацию о регистрации компонента.
cmd-getcomponentregistration-help = Использование: getcomponentregistration <componentName>
cmd-showrays-desc = Включает отладочное отображение физических лучей. Нужно указать целое число для <raylifetime>.
cmd-showrays-help = Использование: showrays <raylifetime>
cmd-disconnect-desc = Немедленно отключается от сервера и возвращается в главное меню.
cmd-disconnect-help = Использование: disconnect
cmd-entfo-desc = Показывает подробную диагностику сущности.
cmd-entfo-help =
    Использование: entfo <entityuid>
    UID объекта может иметь префикс 'c', чтобы преобразовать его в UID объекта клиента.
cmd-fuck-desc = Вызывает исключение
cmd-fuck-help = Вызывает исключение
cmd-showpos-desc = Включает отладочное отображение всех позиций сущностей в игре.
cmd-showpos-help = Использование: showpos
cmd-sggcell-desc = Показывает сущности на ячейке сетки.
cmd-sggcell-help = Использование: sggcell <gridID> <vector2i>\nЭтот параметр vector2i имеет форму x<int>,y<int>.
cmd-overrideplayername-desc = Изменяет имя, используемое при попытке подключения к серверу.
cmd-overrideplayername-help = Использование: overrideplayername <name>
cmd-showanchored-desc = Показывает закрепленные сущности на определенной плитке.
cmd-showanchored-help = Использование: showanchored
cmd-dmetamem-desc = Выводит члены типа в формате, подходящем для файла конфигурации песочницы.
cmd-dmetamem-help = Использование: dmetamem <type>
cmd-launchauth-desc = Загружает токены аутентификации из данных лаунчера для помощи в тестировании живых серверов.
cmd-launchauth-help = Использование: launchauth <account name>
cmd-lightbb-desc = Включает отображение ограничивающих рамок света.
cmd-lightbb-help = Использование: lightbb
cmd-monitorinfo-desc = Мониторинг информации
cmd-monitorinfo-help = Использование: monitorinfo <id>
cmd-setmonitor-desc = Устанавливает монитор
cmd-setmonitor-help = Использование: setmonitor <id>
cmd-physics-desc = Показывает отладочный физический наложение. Аргумент указывает наложение.
cmd-physics-help = Использование: physics <aabbs / com / contactnormals / contactpoints / distance / joints / shapeinfo / shapes>
cmd-hardquit-desc = Немедленно закрывает клиент игры.
cmd-hardquit-help = Немедленно закрывает клиент игры, не оставляя следов. Без прощания с сервером.
cmd-quit-desc = Корректно закрывает клиент игры.
cmd-quit-help = Корректно закрывает клиент игры, уведомляя подключенный сервер и т.д.
cmd-csi-desc = Открывает интерактивную консоль C#.
cmd-csi-help = Использование: csi
cmd-scsi-desc = Открывает интерактивную консоль C# на сервере.
cmd-scsi-help = Использование: scsi
cmd-watch-desc = Открывает окно наблюдения за переменными.
cmd-watch-help = Использование: watch
cmd-showspritebb-desc = Включает или отключает отображение границ спрайтов.
cmd-showspritebb-help = Использование: showspritebb
cmd-togglelookup-desc = Показывает / скрывает границы entitylookup через наложение.
cmd-togglelookup-help = Использование: togglelookup
cmd-net_entityreport-desc = Включает или отключает панель отчетов о сетевых сущностях.
cmd-net_entityreport-help = Использование: net_entityreport
cmd-net_refresh-desc = Запрашивает полное состояние сервера.
cmd-net_refresh-help = Использование: net_refresh
cmd-net_graph-desc = Включает или отключает панель статистики сети.
cmd-net_graph-help = Использование: net_graph
cmd-net_watchent-desc = Выводит все сетевые обновления для EntityId в консоль.
cmd-net_watchent-help = Использование: net_watchent <0|EntityUid>
cmd-net_draw_interp-desc = Включает или отключает отладочное отображение сетевой интерполяции.
cmd-net_draw_interp-help = Использование: net_draw_interp <0|EntityUid>
cmd-vram-desc = Показывает статистику использования видеопамяти игрой.
cmd-vram-help = Использование: vram
cmd-showislands-desc = Показывает текущие физические тела, участвующие в каждом физическом острове.
cmd-showislands-help = Использование: showislands
cmd-showgridnodes-desc = Показывает узлы для разделения сетки.
cmd-showgridnodes-help = Использование: showgridnodes
cmd-profsnap-desc = Создает снимок профилирования.
cmd-profsnap-help = Использование: profsnap
cmd-devwindow-desc = Окно разработки
cmd-devwindow-help = Использование: devwindow
cmd-scene-desc = Немедленно изменяет сцену/состояние UI.
cmd-scene-help = Использование: scene <className>
cmd-szr_stats-desc = Отчет о статистике сериализатора.
cmd-szr_stats-help = Использование: szr_stats
cmd-hwid-desc = Возвращает текущий HWID (идентификатор оборудования).
cmd-hwid-help = Использование: hwid
cmd-vvread-desc = Извлекает значение пути, используя VV (View Variables).
cmd-vvwrite-desc = Изменяет значение пути, используя VV (View Variables).
cmd-vvwrite-help = Использование: vvwrite <path>
cmd-vvinvoke-desc = Вызывает/Вызывает путь с аргументами, используя VV.
cmd-vvinvoke-help = Использование: vvinvoke <path> [аргументы...]
cmd-dump_dependency_injectors-desc = Выводит кэш инжекторов зависимостей IoCManager.
cmd-dump_dependency_injectors-help = Использование: dump_dependency_injectors
cmd-dump_dependency_injectors-total-count = Всего: { $total }
cmd-dump_netserializer_type_map-desc = Выводит карту типов и хеш сериализатора NetSerializer.
cmd-dump_netserializer_type_map-help = Использование: dump_netserializer_type_map
cmd-hub_advertise_now-desc = Немедленно рекламирует на главном сервере хаба.
cmd-hub_advertise_now-help = Использование: hub_advertise_now
cmd-echo-desc = Возвращает аргументы обратно в консоль.
cmd-echo-help = Использование: echo "<message>"
cmd-vfs_ls-desc = Список содержимого директории в VFS.
cmd-vfs_ls-help =
    Использование: vfs_list <path>
    Пример:
    vfs_list /Assemblies
cmd-vfs_ls-err-args = Требуется ровно 1 аргумент.
cmd-vfs_ls-hint-path = <path>
