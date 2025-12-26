ore-silo-ui-title = Материальное хранилище
ore-silo-ui-label-clients = Машины
ore-silo-ui-label-mats = Материалы
ore-silo-ui-itemlist-entry =
    { $linked ->
        [true] { "[Соединено] " }
       *[False] { "" }
    } { $name } ({ $beacon }) { $inRange ->
        [true] { "" }
       *[false] (Вне зоны действия)
    }
