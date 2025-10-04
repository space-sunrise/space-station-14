## Survivor

roles-antag-survivor-name = Выживший
# It's a Halo reference
roles-antag-survivor-objective = Текущая цель: Выжить
survivor-role-greeting =
    Вы - Выживший.
    Прежде всего вам нужно живым вернуться в Центком.
    Соберите столько огневой мощи, сколько необходимо, чтобы гарантировать своё выживание.
    Не доверяйте никому.
survivor-round-end-dead-count =
    { $deadCount ->
        [one] [color=red]{ $deadCount }[/color] выживший погиб.
        [few] [color=red]{ $deadCount }[/color] выживших погибли.
       *[other] [color=red]{ $deadCount }[/color] выживших погибло.
    }
survivor-round-end-alive-count =
    { $aliveCount ->
        [one] [color=yellow]{ $aliveCount }[/color] выживший остался на станции.
        [few] [color=yellow]{ $aliveCount }[/color] выживших остались на станции.
       *[other] [color=yellow]{ $aliveCount }[/color] выживших осталось на станции.
    }
survivor-round-end-alive-on-shuttle-count =
    { $aliveCount ->
        [one] [color=green]{ $aliveCount }[/color] выживший спасся.
        [few] [color=green]{ $aliveCount }[/color] выживших спаслись.
       *[other] [color=green]{ $aliveCount }[/color] выживших спаслось.
    }

## Wizard

objective-issuer-swf = [color=turquoise]Федерация Космических Магов[/color]
wizard-title = Маг
wizard-description = На станции Маг! Неизвестно, что он может сделать.
roles-antag-wizard-name = Маг
roles-antag-wizard-objective = Преподайте им урок, который они никогда не забудут.
wizard-role-greeting =
    ТЫ МАГ!
    Между Федерацией Космических Магов и NanoTrasen возникла напряжённость.
    Поэтому ты был выбран Федерацией Космических Магов для визита на станцию.
    Продемонстрируй им свои способности.
    Что делать - решать тебе, только помни, что Космические Маги хотят, чтобы ты остался в живых.
wizard-round-end-name = маг

## TODO: Wizard Apprentice (Coming sometime post-wizard release)

