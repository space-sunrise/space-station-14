id-card-access-level-virology = Вирусология
disease-resistance-coefficient-value = - Шанс [color=violet]заражения вирусом[/color] снижен на [color=purple]{ $value }%[/color].

disease-data-server-get-disk-verb-text = Вытащить диск с данными { $value }.

# Этикетки
disease-data-vial-label = вирусный образец

# При получении урона от некроза
disease-necrosis-popup-1 = Ты чувствуешь, как [color=darkred]ткань под кожей[/color] медленно умирает...
disease-necrosis-popup-2 = [color=darkred]Боль пронзает[/color] тело, кожа будто [color=crimson]гниёт изнутри[/color].
disease-necrosis-popup-3 = Твоё тело откликается на инфекцию — [color=purple]клетки разрушаются[/color] одна за другой.
disease-necrosis-popup-4 = Из-под кожи выступает [color=darkred]чёрная жидкость[/color], сопровождаемая жжением.
disease-necrosis-popup-5 = Ты чувствуешь [color=purple]тяжесть и разложение[/color] в собственных мышцах.

# Диагност вирусов
disease-diagnoser-dna-material-attached = Днк материал внутри машины.
disease-diagnoser-flask-attached = Колба внутри машины.

disease-collector-no-mouth = У цели нет ротового отверстия. Введение вируса невозможно.
disease-collector-is-used = Предмет уже был использован.
disease-collector-warn-target = Вам лезут в рот.
drug-collector-dna-not-found = Неизвестно.

reagent-name-viral-solution = раствор с заболеваниями
reagent-desc-viral-solution = Стерильный физиологический раствор с суспензией активного квантового вируса, способного выдерживать FTL-транспортировку.
reagent-physical-desc-clear = прозрачная жидкость

reagent-effect-guidebook-cause-disease = Заражает заболеванием
reagent-effect-guidebook-antiseptic = Убивает болезни

reagent-effect-guidebook-damage-disease =
    { $chance ->
        [1] Убивает
       *[other] убивает
    } болезни или вирусы в организме

entity-effect-guidebook-damage-disease =
    Наносит болезням { $baseDamage } урона и повышает их устойчивость к лекарствам на { $resistanceIncrease }

## -----------------------
##   Вирусный отчёт
## -----------------------

disease-report-no-disease = Вирусных данных не найдено. Образец чист.

disease-report-title = АНАЛИЗ ВИРУСНОГО ОБРАЗЦА

disease-report-strain = Идентификатор штамма: {$id}
disease-report-threshold = Состояние вируса (живучесть): {$value}
disease-report-infectivity = Инфективность: {$value}%

disease-report-damage-when-dead = Показатель уязвимости, если организм носителя мёртв: {$value}
disease-report-mutation-points = Очки мутации: {$value}
disease-report-regen-threshold = Регенерация вируса: {$value}
disease-report-regen-mutation = Скорость мутации: {$value}
disease-report-milty-price-delete-symptom = Сложность удаления симптома {$value}

disease-report-default-medicine-resistance = Базовое сопротивление медикаментам: {$value}

disease-report-medicine-header = Устойчивость к препаратам:
disease-report-medicine-entry = - {$name}: {$value}

disease-report-medicine-none = Не обнаружена

disease-report-symptoms-header = Активные симптомы:
disease-report-symptoms-none = Не выявлены

disease-report-bodyes-header = Допустимые к заражению организмы:
disease-report-body-any = Не выявлены

disease-report-footer = Отчёт сгенерирован вирусным диагностическим модулем.

## UI

### Заголовок окна
disease-diagnoser-window-title = Диагност заболеваний

### Вкладка сервера
disease-diagnoser-server-strains-label = Штаммы вируса на сервере
disease-diagnoser-delete-strain-button = Удалить штамм

disease-diagnoser-server-missing = Нет соединения с сервером данных
disease-diagnoser-server-far = Сервер данных находится слишком далеко

### Вкладка диагноста
disease-diagnoser-actions-label = Доступные действия

disease-diagnoser-scan-button = Сканировать вирус
disease-diagnoser-print-button = Печать отчёта
disease-diagnoser-generate-button = Сгенерировать вирус

disease-diagnoser-missing = Нет соединения с диагностом
disease-diagnoser-far = Диагност находится слишком далеко

# Solution аналайзер
disease-solution-analyser-start-analys-button = Запустить анализ
disease-solution-analyser-missing = Нет соединения с анализатором веществ
disease-solution-analyser-far = Анализатор веществ находится слишком далеко

# Ports

signal-port-name-disease-diagnoser-sender = Диагност заболеваний
signal-port-description-disease-diagnoser-sender = Передатчик сигнала диагносту заболеваний

signal-port-name-disease-data-server-sender = Сервер данных
signal-port-description-disease-data-server-sender = Передатчик сигнала серверу данных

signal-port-name-disease-solution-analyzer-sender = Диагност веществ
signal-port-description-disease-solution-analyzer-sender = Передатчик сигнала диагносту веществ

signal-port-name-disease-diagnoser-receiver = Диагност заболеваний
signal-port-description-disease-diagnoser-receiver = Принимающий сигнал диагност заболеваний

signal-port-name-disease-data-server-receiver = Сервер данных
signal-port-description-disease-data-server-receiver = Принимающий сигнал сервер данных

signal-port-name-disease-solution-analyzer-receiver = Диагност веществ
signal-port-description-disease-solution-analyzer-receiver = Принимающий сигнал диагност веществ
# Другое

research-technology-virology = Вирусология

disease-mutation-verb = Очистить от вируса


# Консоль эволюции

### WINDOW ###

disease-evolution-window-title = Эволюция вируса


### TABS ###

disease-evolution-tab-evolution = Эволюция
disease-evolution-tab-whitelist = Белый список


### EVOLUTION TAB ###

disease-evolution-available-symptoms = Доступные симптомы
disease-evolution-active-symptoms = Активные симптомы
disease-evolution-description-header = Описание
disease-evolution-buy-button = Купить симптом
disease-evolution-delete-button = Удалить симптом

disease-evolution-mutation-points =
    Очки мутации: { $points }

disease-evolution-health =
    Максимум здоровья: { $max }

disease-evolution-infectivity =
    Заразность: { $percent }%

disease-evolution-infected-count =
    Заражённых: { $count }

disease-evolution-points-per-second =
    Очков/сек: { $points }

disease-evolution-cost-label =
    Стоимость: { $cost }


### WHITELIST TAB ###

disease-evolution-available-bodies = Доступные тела
disease-evolution-active-bodies = Активные тела
disease-evolution-buy-body = Добавить тело
disease-evolution-delete-body = Удалить тело

### DATASERVER STATES ###

disease-evolution-diseasedata-missing =
    Данные об вирусе не найдены

disease-evolution-dataserver-missing =
    Сервер данных или анализатор веществ не подключён

disease-evolution-dataserver-far =
    Сервер данных или анализатор веществ слишком далеко


### SOLUTION ANALYZER STATES (на будущее) ###

disease-evolution-solution-analyzer-missing =
    Анализатор растворов не подключён

disease-evolution-solution-analyzer-far =
    Анализатор растворов слишком далеко


### BUTTON / ACTION ERRORS (если понадобятся) ###

disease-evolution-not-enough-points =
    Недостаточно очков мутации

disease-evolution-no-selection =
    Ничего не выбрано


### TOOLTIP / INFO ###

disease-evolution-symptom-price-tooltip =
    Базовая цена: { $price }

disease-evolution-body-price-tooltip =
    Стоимость тела: { $price }


### DEBUG / FALLBACK ###

disease-evolution-unknown-symptom =
    Неизвестный симптом

disease-evolution-unknown-body =
    Неизвестное тело


# РАЗУМНЫЙ ВИРУС

sentient-disease-infect-impossible-target = цель невозможно заразить
sentient-disease-teleport-no-primary-infected = нулевых пациентов не найдено
sentient-disease-infect-failed-source = вы больше не можете создать нулевого пациента
sentient-disease-infect-no-points = Не хватает { $price } очков мутации.
sentient-disease-infect-compensation = Ваш первичный пациент ушёл в крио, вам компенсировали { $price } очков мутации.

# ПРЕПАРАТЫ

reagent-name-infectizine = инфектизин
reagent-desc-infectizine = Простейший препарат, эффективный против слабых вирусов.

reagent-name-mycocline = микоклин
reagent-desc-mycocline = Препарат широкого спектра действия.

reagent-name-virucidine = вируцид
reagent-desc-virucidine = Агрессивный препарат, подавляющий вирусные структуры.

reagent-name-panacemycin = панацемицин
reagent-desc-panacemycin = Экспериментальный препарат экстремального действия.

ent-ChemistryBottleInfectizine = { ent-BaseChemistryBottleFilled }
    .suffix = бактеризин
    .desc = { ent-BaseChemistryBottleFilled.desc }

ent-ChemistryBottleMycocline = { ent-BaseChemistryBottleFilled }
    .suffix = микоклин
    .desc = { ent-BaseChemistryBottleFilled.desc }

ent-ChemistryBottleVirucidine = { ent-BaseChemistryBottleFilled }
    .suffix = вируцид
    .desc = { ent-BaseChemistryBottleFilled.desc }

ent-ChemistryBottlePanacemycin = { ent-BaseChemistryBottleFilled }
    .suffix = панацемицин
    .desc = { ent-BaseChemistryBottleFilled.desc }

reagent-name-septomycin = септомицин
reagent-desc-septomycin = Сильный антисептический препарат, подавляющий устойчивые штаммы инфекций.

ent-ChemistryBottleSeptomycin = { ent-BaseChemistryBottleFilled }
    .suffix = септомицин
    .desc = { ent-BaseChemistryBottleFilled.desc }

reagent-name-necrovir = некровир
reagent-desc-necrovir = Крайне токсичный противовирусный препарат, разрушающий инфекцию вместе с тканями носителя.

reagent-name-antiseptic = антисептик
reagent-desc-antiseptic = Антисептический раствор, подавляющий инфекцию и обеззараживающий поверхность.
reagent-physical-desc-antiseptic = резко пахнущая жидкость

ent-ChemistryBottleNecrovir = { ent-BaseChemistryBottleFilled }
    .suffix = некровир
    .desc = { ent-BaseChemistryBottleFilled.desc }


# Virus-infected human accent
accent-words-disease-1 = Хрр… хрип…
accent-words-disease-2 = б-б-б… чт?
accent-words-disease-3 = Ггг… уенке
accent-words-disease-4 = ххх… пффф…
accent-words-disease-5 = бульк… ффф…
accent-words-disease-6 = хрип… хрип…
accent-words-disease-7 = м-м-м… эээ…

# ANTAG
roles-antag-sentient-disease-name = Разумный вирус
roles-antag-sentient-disease-objective = Заразите как можно больше организмов на станции.
role-subtype-sentient-disease = Разумный вирус

ghost-role-information-sentient-disease-name = Разумный вирус
ghost-role-information-sentient-disease-description = Заразите как можно больше организмов на станции.
ghost-role-information-sentient-disease-rules = Вы [color={ role-type-team-antagonist-color }][bold]{ role-type-solo-antagonist-name }[/bold][/color], распространите вирус по станции.
sentient-disease-role-greeting =
    Вы — разумный вирус.
    У вас нет тела, но есть цель.

    Проникайте в живые организмы, приспосабливайтесь к условиям станции
    и распространяйте себя любыми доступными способами.

    Используйте мутации, симптомы и носителей, чтобы выжить и усилиться.
    Чем больше заражённых, тем сильнее вы становитесь.

    Не действуйте открыто без необходимости.
    Вы, эпидемия, а не солдат.

sentient-disease-round-end-agent-name = разумный вирус

sentient-disease-title = Разумный вирус
sentient-disease-description = На станции появился разумный вирус. Он стремится заразить как можно больше организмов, мутировать и распространиться по всей станции. Будьте бдительны и не

# DataCollector
disease-collector-has-data = Образец взят у пациента.
disease-collector-not-has-data = Биологический материал не обнаружен.

disease-infection-cloud-examine-strain = Штамм: { $strain }
disease-infection-cloud-examine-infectivity = Заразность: { $infectivity }%


health-analyzer-window-entity-infected-text =
    Заражён вирусом.
    Состояние излечения организма: { $progress }%

# Песочница
sandbox-window-toggle-disease-button = Отображение заболеваний
