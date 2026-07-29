### Connecting dialog when you start up the game

connecting-title = Space Station 14
connecting-exit = Exit
connecting-retry = Retry
connecting-reconnect = Reconnect
connecting-copy = Copy Message
connecting-redial = Relaunch
connecting-redial-wait = Please wait: { TOSTRING($time, "G3") }
connecting-in-progress = Connecting to server...
connecting-disconnected = Disconnected from server:
connecting-tip = Don't die!
connecting-window-tip = Tip { $numberTip }
connecting-version = ver 0.1
connecting-fail-reason = Failed to connect to server:
                         { $reason }
connecting-state-NotConnecting = Not connecting
connecting-state-ResolvingHost = Resolving host
connecting-state-EstablishingConnection = Establishing connection
connecting-state-Handshake = Handshake
connecting-state-Connected = Connected

# Sunrise added start - показываем прогресс runtime-контента во время подключения.
connecting-uploaded-content-checking = Checking additional content…
connecting-uploaded-content-downloading = Downloading additional content
connecting-uploaded-content-current-calculating = Current file: calculating speed…
connecting-uploaded-content-current-estimated = Current file: ~{ $percent }% · ~{ $speed }/s
connecting-uploaded-content-total =
    Total: { $completedBytes } / { $totalBytes } · { $completedFiles } of { $totalFiles } { $totalFiles ->
        [one] file
       *[other] files
    }
# Sunrise added end
