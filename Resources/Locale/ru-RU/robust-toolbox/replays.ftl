# Playback Commands

cmd-replay-play-desc = Возобновляет воспроизведение.
cmd-replay-play-help = Использование: replay_play
cmd-replay-pause-desc = Приостанавливает воспроизведение.
cmd-replay-pause-help = Использование: replay_pause
cmd-replay-toggle-desc = Возобновляет или приостанавливает воспроизведение.
cmd-replay-toggle-help = Использование: replay_toggle
cmd-replay-stop-desc = Останавливает и выгружает воспроизведение.
cmd-replay-stop-help = Использование: replay_stop
cmd-replay-load-desc = Загружает и запускает воспроизведение.
cmd-replay-load-help = Использование: replay_load <replay folder>
cmd-replay-load-hint = Папка с воспроизведением
cmd-replay-skip-desc = Перематывает вперёд или назад по времени.
cmd-replay-skip-help = Использование: replay_skip <tick or timespan>
cmd-replay-skip-hint = Тики или временной интервал (ЧЧ:ММ:СС).
cmd-replay-set-time-desc = Перематывает вперёд или назад до конкретного времени.
cmd-replay-set-time-help = Использование: replay_set <tick or time>
cmd-replay-set-time-hint = Тик или временной интервал (ЧЧ:ММ:СС), начиная с
cmd-replay-error-time = "{ $time }" не является целым числом или временным интервалом.
cmd-replay-error-args = Неверное количество аргументов.
cmd-replay-error-no-replay = В данный момент воспроизведение не активно.
cmd-replay-error-already-loaded = Воспроизведение уже загружено.
cmd-replay-error-run-level = Вы не можете загрузить воспроизведение, находясь в подключении к серверу.

# Recording commands

cmd-replay-recording-start-desc = Запускает запись воспроизведения, возможно с ограничением по времени.
cmd-replay-recording-start-help = Использование: replay_recording_start [name] [overwrite] [time limit]
cmd-replay-recording-start-success = Запись воспроизведения начата.
cmd-replay-recording-start-already-recording = Запись воспроизведения уже идет.
cmd-replay-recording-start-error = При попытке начать запись произошла ошибка.
cmd-replay-recording-start-hint-time = [time limit (minutes)]
cmd-replay-recording-start-hint-name = [name]
cmd-replay-recording-start-hint-overwrite = [overwrite (bool)]
cmd-replay-recording-stop-desc = Останавливает запись воспроизведения.
cmd-replay-recording-stop-help = Использование: replay_recording_stop
cmd-replay-recording-stop-success = Запись воспроизведения остановлена.
cmd-replay-recording-stop-not-recording = В данный момент запись воспроизведения не идет.
cmd-replay-recording-stats-desc = Отображает информацию о текущей записи воспроизведения.
cmd-replay-recording-stats-help = Использование: replay_recording_stats
cmd-replay-recording-stats-result = Длительность: { $time } мин, Тики: { $ticks }, Размер: { $size } МБ, скорость: { $rate } МБ/мин.
# Time Control UI
replay-time-box-scrubbing-label = Динамическое перематывание
replay-time-box-replay-time-label = Время записи: { $current } / { $end } ({ $percentage }%)
replay-time-box-server-time-label = Время сервера: { $current } / { $end }
replay-time-box-index-label = Индекс: { $current } / { $total }
replay-time-box-tick-label = Тик: { $current } / { $total }
