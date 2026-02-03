## Основные действия

alerts-vampire-blood-name = Кровавое опьянение
alerts-vampire-blood-desc = Показывает, сколько крови вы выпили. Вытяните клыки и нажмите ЛКМ по цели, чтобы пить.

alerts-vampire-fed-name = Насыщение кровью
alerts-vampire-fed-desc = Ваше текущее насыщение кровью. Пейте кровь, чтобы оставаться сытым.

roles-antag-vamire-name = Вампир
roles-antag-vampire-description = Питайтесь членами экипажа. Вытяните клыки и пейте их кровь.

vampire-roundend-name = вампир

vampire-drink-start = Вы вонзаете клыки в {CAPITALIZE(THE($target))}.

vampire-not-enough-blood = Недостаточно крови.

vampire-mouth-covered = Ваш рот закрыт!
vampire-drink-invalid-target = Вы не можете пить кровь вампиров или их рабов.
vampire-target-protected-by-faith = Этот человек защищён своей верой!
vampire-drink-target-maxed = Вы уже выпили { $amount } единиц крови у этой цели.
vampire-drink-target-hard-max = Вы выпили максимум крови у этой цели ({ $amount } единиц).
vampire-full-power-achieved = Ваша вампирическая сущность достигает полной силы!
vampire-umbrae-full-power-fov = Тени подчиняются вашей воле. Теперь вы можете видеть сквозь стены!

vampire-role-greeting = Вы — вампир!
    Ваша жажда крови толкает вас питаться членами экипажа. Используйте способности, чтобы обращать других.
    Ваши клыки позволяют высасывать кровь из людей. Кровь восстанавливает здоровье и даёт новые способности.
    Найдите, чем заняться во время этой смены!

# Цели
objective-issuer-vampire = [color=crimson]Вампир[/color]

objective-condition-drain-title = Выпить {$count} единиц крови
objective-condition-drain-description = Выпейте {$count} единиц крови у членов экипажа, используя клыки.

objective-vampire-thrall-obey-master-title = Подчиняйся своему господину, {$targetName}.

# Действие выбора класса
action-vampire-class-select = Выбрать класс вампира
action-vampire-class-select-desc = Выберите свой подкласс вампира

# Статистика в конце раунда
roundend-prepend-vampire-drained-low = Вампиры едва кормились в эту смену, выпив всего {$blood} единиц крови.
roundend-prepend-vampire-drained-medium = Вампиры неплохо пообедали, выпив {$blood} единиц крови.
roundend-prepend-vampire-drained-high = Вампиры устроили кровавый пир, выпив {$blood} единиц крови!
roundend-prepend-vampire-drained-critical = Вампиры впали в неистовство, выпив ошеломляющие {$blood} единиц крови!

roundend-prepend-vampire-drained = В этом раунде ни одному вампиру не удалось выпить сколь-нибудь значимое количество крови.
roundend-prepend-vampire-drained-named = {$name} был самым кровожадным вампиром, выпив в сумме {$number} единиц крови.

# Подсказки при выборе класса вампира
vampire-class-hemomancer-tooltip = Гемомант
    Сосредоточен на кровавой магии и манипуляции кровью вокруг себя

vampire-class-umbrae-tooltip = Умбра
    Сосредоточен на тьме, скрытных атаках из засады и мобильности

vampire-class-gargantua-tooltip = Гаргантюа
    Сосредоточен на живучести и уроне в ближнем бою

vampire-class-dantalion-tooltip = Данталион
    Сосредоточен на подчинении и иллюзиях

# Способности Гемоманта
action-vampire-hemomancer-tendrils-wrong-place = Нельзя применить здесь.

action-vampire-blood-barrier-wrong-place = Нельзя разместить барьер здесь.

action-vampire-sanguine-pool-already-in = Вы уже в форме кровавой лужи!
action-vampire-sanguine-pool-invalid-tile = Вы не можете превратиться в кровавую лужу здесь.
action-vampire-sanguine-pool-enter = Вы превращаетесь в лужу крови!
action-vampire-sanguine-pool-exit = Вы восстанавливаете форму из лужи крови!
vampire-space-burn-warning = Суровый свет пустоты обжигает вашу неживую плоть!

action-vampire-blood-eruption-activated = Вы вызываете извержение шипов крови вокруг себя!

action-vampire-blood-bringers-rite-not-enough-power = Вам не хватает полной вампирической силы (нужно более 1000 всей крови и 8 уникальных жертв)
action-vampire-blood-bringers-rite-not-enough-blood = Недостаточно крови для активации Ритуала Кровеносцев
action-vampire-blood-bringers-rite-start = Ритуал Кровеносцев активирован!
action-vampire-blood-bringers-rite-stop = Ритуал Кровеносцев деактивирован
action-vampire-blood-bringers-rite-stop-blood = Ритуал Кровеносцев деактивирован — недостаточно крови

vampire-locate-result = Ваши чувства ведут вас к { $target } в { $location }.
vampire-locate-not-same-sector = Этот человек не в вашем секторе.
vampire-locate-unknown = Неизвестная зона
vampire-locate-no-targets = В этом секторе не чувствуется добычи.

predator-sense-title = Чувство хищника
vampire-locate-search-placeholder = Поиск...

vampiric-claws-remove-popup = Вы убираете когти.

# Способности Умбры
action-vampire-cloak-of-darkness-start = Вы сливаетесь с тенями!
action-vampire-cloak-of-darkness-stop = Вы выходите из теней.

action-vampire-shadow-snare-placed = Вы установили теневую ловушку.
action-vampire-shadow-snare-wrong-place = Вы не можете установить ловушку здесь.
action-vampire-shadow-snare-scatter = Вы развеяли теневую ловушку.
vampire-shadow-snare-oldest-removed = Ваша старая теневая ловушка рассеивается.
ent-shadow-snare-ensnare = теневая ловушка

action-vampire-shadow-anchor-returned = Вы вернулись к теневому якорю
action-vampire-shadow-anchor-installed = Вы закрепились в тенях

action-vampire-shadow-boxing-start = Вы начинаете теневой бой.
action-vampire-shadow-boxing-stop = Теневой бой остановлен.
action-vampire-shadow-boxing-ends = Теневой бой заканчивается.

action-vampire-dark-passage-wrong-place = Тьма здесь непроницаема...
action-vampire-dark-passage-activated = Вы скользнули сквозь тьму...

action-vampire-extinguish-activated = Вы поглотили свет вокруг себя... ({$count})

action-vampire-eternal-darkness-not-enough-blood = У вас закончилась кровь, чтобы поддерживать вечную тьму.
action-vampire-eternal-darkness-start = Вы призвали вечную тьму...
action-vampire-eternal-darkness-stop = Вечная тьма рассеялась...

# Данталион
vampire-enthrall-start = Вы проникаете в разум {CAPITALIZE(THE($target))}...
vampire-enthrall-success = {CAPITALIZE(THE($target))} преклоняет колено и становится вашим рабом.
vampire-enthrall-target = Ваш разум подавлен вампирическим господством!
vampire-enthrall-limit = Вы не можете контролировать больше рабов.
vampire-enthrall-invalid = Эту цель нельзя поработить.
vampire-thrall-released = Вампирическая хватка над вами ослабевает.

vampire-pacify-invalid = Эту цель нельзя умиротворить.
vampire-pacify-success = {CAPITALIZE(THE($target))} поддаётся вашему всепоглощающему спокойствию.
vampire-pacify-target = Сокрушительное безмолвие гасит вашу волю к борьбе!

vampire-subspace-swap-thrall = Вы не можете обмениваться местами со своими рабами.
vampire-subspace-swap-dead = Этот разум вне вашей досягаемости.
vampire-subspace-swap-failed = Подпространственный разрыв бесполезно искрится.
vampire-subspace-swap-success = Пространство искажается, и вы меняетесь местами с {CAPITALIZE(THE($target))}!
vampire-subspace-swap-target = Реальность искажается, и вас вырывает на новое место!

vampire-rally-thralls-success = { $count ->
    [one] Ваш зов возвращает одного раба на вашу сторону!
    [few] Ваш зов возвращает {$count} рабов на вашу сторону!
    [many] Ваш зов возвращает {$count} рабов на вашу сторону!
   *[other] Ваш зов возвращает {$count} рабов на вашу сторону!
}
vampire-rally-thralls-none = Ни один из ваших рабов не может ответить на зов.
vampire-thrall-holy-water-freed = Святая вода очищает разум от вампирической хватки!

vampire-blood-bond-start = Реки крови связывают вас с вашими рабами.
vampire-blood-bond-stop = Вы ослабляете кровавую связь.
vampire-blood-bond-no-thralls = У вас нет порабощённых слуг для связи.
vampire-blood-bond-stop-blood = Связь разрывается — вам не хватает крови, чтобы поддерживать её.

action-vampire-not-enough-power = Ваша сила недостаточна (нужно >1000 всей крови и 8 уникальных жертв).

# Гаргантюа
vampire-blood-swell-start = Ваши мышцы набухают от нечестивой силы
vampire-blood-swell-end = Кровавое неистовство утихает.

vampire-blood-rush-start = Кровь бурлит в ваших конечностях!
vampire-blood-rush-end = Сверхъестественная скорость покидает вас.

vampire-seismic-stomp-activate = Земля содрогается от вашей ярости!

vampire-overwhelming-force-start = Ваше присутствие становится незыблемым.
vampire-overwhelming-force-stop = Вы ослабляете железную хватку.
vampire-overwhelming-force-too-heavy = Этот предмет слишком тяжёл, чтобы сдвинуть его!
vampire-overwhelming-force-door-pried = Вы с силой вырываете дверь.

vampire-demonic-grasp-hit = Демоническая клешня хватает вас!
vampire-demonic-grasp-pull = Клешня тащит вас к вампиру!

vampire-charge-start = Вы несётесь вперёд с неудержимой силой!
vampire-charge-impact = Вы врезаетесь в {CAPITALIZE(THE($target))} с сокрушительной силой!

vampire-blood-swell-cancel-shoot = Ваши пальцы не пролезают в скобу спускового крючка!!

vampire-holy-place-burn = Священная земля обжигает вашу нечестивую плоть!

alerts-vampire-blood-swell-name = Кровяной отёк
alerts-vampire-blood-swell-desc = Ваши мышцы набухают от нечестивой силы.
alerts-vampire-blood-rush-name = Кровяной рывок
alerts-vampire-blood-rush-desc = Сверхъестественная скорость бежит по вашим конечностям.
