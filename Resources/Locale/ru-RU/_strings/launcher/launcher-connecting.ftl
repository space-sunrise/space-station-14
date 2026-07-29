### Connecting dialog when you start up the game

connecting-title = Space Station 14
connecting-exit = Выйти
connecting-retry = Повторить
connecting-reconnect = Переподключиться
connecting-copy = Скопировать сообщение
connecting-redial = Перезапустить
connecting-redial-wait = Пожалуйста подождите: { TOSTRING($time, "G3") }
connecting-in-progress = Подключение к серверу...
connecting-disconnected = Отключён от сервера:
connecting-tip = Мяу!
connecting-window-tip = Совет { $numberTip }
connecting-version = версия 0.1
connecting-fail-reason =
    Не удалось подключиться к серверу:
    { $reason }
connecting-state-NotConnecting = Не подключён
connecting-state-ResolvingHost = Определение хоста
connecting-state-EstablishingConnection = Установка соединения
connecting-state-Handshake = Подключение
connecting-state-Connected = Подключён

# Sunrise added start - показываем прогресс runtime-контента во время подключения.
connecting-uploaded-content-checking = Проверка дополнительного контента…
connecting-uploaded-content-downloading = Скачивается дополнительный контент
connecting-uploaded-content-current-calculating = Текущий файл: расчёт скорости…
connecting-uploaded-content-current-estimated = Текущий файл: ~{ $percent }% · ~{ $speed }/с
connecting-uploaded-content-total =
    Всего: { $completedBytes } / { $totalBytes } · { $completedFiles } из { $totalFiles } { $totalFiles ->
        [one] файла
       *[other] файлов
    }
# Sunrise added end
