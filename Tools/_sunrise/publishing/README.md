# Публикация Sunrise

Рабочие скрипты публикации и их проверки находятся в этом каталоге.
Их вызывает `.github/workflows/sunrise-publish.yml`; Stable и Test используют тот же сценарий.
GitHub требует хранить сценарии непосредственно в `.github/workflows`, поэтому для них используется префикс `sunrise-`.

Исходные `Tools/publish_multi_request.py`, `Tools/publish_github_artifact.py` и `.github/workflows/publish.yml`
сохранены без изменений из Wizden, коммит `eec3013cdda037c2d2cccd7ac5179e3fe0d08dd2`.
Они не используются сценариями Sunrise. Для ручной публикации выбирайте **Publish Sunrise**, **Publish Stable** или **Publish Test**.

Проверка скриптов: `python -m unittest discover -s Tools/_sunrise/publishing -p test_validate_publish.py`.
