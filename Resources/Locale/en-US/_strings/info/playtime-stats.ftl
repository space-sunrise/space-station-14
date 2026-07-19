# Playtime Stats

ui-playtime-stats-title = User Playtime Stats
ui-playtime-overall-base = Overall Playtime:
ui-playtime-overall = Overall Playtime: {PLAYTIME($time)}
ui-playtime-first-time = First Time Playing
ui-playtime-roles = Playtime per Role
ui-playtime-header-role-type = Role
ui-playtime-header-role-time = Time
ui-playtime-time-format-short =
    { $hours ->
        [0] { $minutes }m.
       *[other] { $hours }h { $minutes }m.
    }
ui-playtime-time-format-verbose =
    { $hours ->
        [0] { $minutes } minutes
       *[other] { $hours } hours { $minutes } minutes
    }
