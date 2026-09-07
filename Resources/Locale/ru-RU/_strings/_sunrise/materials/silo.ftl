sunrise-material-silo-ui-title = Станционное хранилище материалов
sunrise-material-silo-ui-label-clients = Машины
sunrise-material-silo-ui-label-mats = Материалы
sunrise-material-silo-ui-itemlist-entry =
    { $linked ->
        [true] { "[Соединено] " }
       *[False] { "" }
    } { $name } ({ $beacon }) { $inRange ->
        [true] { "" }
       *[false] (Нет связи)
    }
