discord-watchlist-connection-header =
    { $players ->
        [one] { $players } игрок из списка наблюдения подключился к
       *[other] { $players } игроков из списка наблюдения подключились к
    } { $serverName }
discord-watchlist-connection-entry =
    - { $playerName } с сообщением «{ $message }»{ $expiry ->
        [0] { "" }
       *[other] { " " }(истекает <t:{ $expiry }:R>)
    }{ $otherWatchlists ->
        [0] { "" }
        [one] { " " }и { $otherWatchlists } другой список наблюдения
       *[other] { " " }и { $otherWatchlists } других списков наблюдения
    }
