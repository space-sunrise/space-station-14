### Interaction Messages

# System

## When trying to ingest without the required utensil... but you gotta hold it
ingestion-you-need-to-hold-utensil = Чтобы съесть это, вам нужно держать { $utensil }!

ingestion-try-use-is-empty = { $entity } пусто!
ingestion-try-use-wrong-utensil = Вы не можете { $verb } { $food } с помощью { $utensil }.

ingestion-remove-mask = Сначала вам нужно снять { $entity }.

## Failed Ingestion

ingestion-you-cannot-ingest-any-more = Вы больше не можете { $verb }!
ingestion-other-cannot-ingest-any-more = { SUBJECT($target) } больше не может { $verb }!

ingestion-cant-digest = Вы не можете переварить { $entity }!
ingestion-cant-digest-other = { SUBJECT($target) } не может переварить { $entity }!

## Action Verbs, not to be confused with Verbs

ingestion-verb-food = Есть
ingestion-verb-drink = Пить

# Edible Component

edible-nom = Ням. { $flavors }
edible-nom-other = Ням.
edible-slurp = Хлюп. { $flavors }
edible-slurp-other = Хлюп.
edible-swallow = Вы проглатываете { $food }
edible-gulp = Бульк. { $flavors }
edible-gulp-other = Бульк.

edible-has-used-storage = Вы не можете { $verb } { $food }, если внутри что-то хранится.

## Nouns

edible-noun-edible = съедобное
edible-noun-food = еда
edible-noun-drink = напиток
edible-noun-pill = таблетка

## Verbs

edible-verb-edible = употребить
edible-verb-food = съесть
edible-verb-drink = выпить
edible-verb-pill = проглотить

## Force feeding

edible-force-feed = { $user } пытается заставить вас { $verb } что-то!
edible-force-feed-success = { $user } заставил(а) вас { $verb } что-то! { $flavors }
edible-force-feed-success-user = Вы успешно накормили { $target }
