sunrise-material-silo-ui-title = Grid Material Silo
sunrise-material-silo-ui-label-clients = Machines
sunrise-material-silo-ui-label-mats = Materials
sunrise-material-silo-ui-itemlist-entry = {$linked ->
    [true] {"[Linked] "}
    *[False] {""}
} {$name} ({$beacon}) {$inRange ->
    [true] {""}
    *[false] (No Connection)
}
