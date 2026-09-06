# Автодрафт

Скрипт `review_threads.js` синхронизирует состояние открытых ПР с ревью GitHub.
Используются последние решения ревьюверов с правом записи в репозиторий, включая
CodeRabbit. Автоматический черновик помечается меткой `auto-draft: unresolved review`.
Ручные черновики без этой метки скрипт не публикует.

Текущая политика:

- Новое `Request changes` переводит готовый ПР в черновик.
- `Approve` заменяет требование исправлений только того же ревьювера.
  ПР возвращается к ревью, когда блокирующих решений не осталось либо закрыты все
  обсуждения, связанные с текущими блокирующими ревью.
- `Request changes` без обсуждений остаётся блокирующим до нового решения,
  отмены ревью или ручного перевода ПР в готовое состояние.
- Ручной перевод в готовое состояние имеет приоритет над старыми замечаниями.
- Проверки сборки, ожидание первого ревью и повторного ревью нового коммита
  пока не входят в условия готовности. Обсуждения прежних ревью того же автора
  также не учитываются, если их ревью заменено новым решением.

CodeRabbit должен проверять черновики (`reviews.auto_review.drafts: true`),
иначе он перестанет автоматически проверять исправления после автодрафта.

## Установка GitHub App

Приложение принадлежит организации `makura-games`. Его нужно установить только на
`sunrise-station`, `project-fire`, `lust-station`, `fish-station`, `invicta`.
Наличие приложения и секретов само по себе не обновляет workflow других репозиториев:
для перехода с прежнего `AUTO_DRAFT_TOKEN` туда также требуется перенести реализацию.

1. Владелец организации открывает [заполненную форму регистрации](https://github.com/organizations/makura-games/settings/apps/new?name=Makura%20Auto%20Draft&url=https%3A%2F%2Fgithub.com%2Fmakura-games%2Fsunrise-station&public=false&webhook_active=false&contents=write&issues=write&pull_requests=write).
2. Проверяет права репозитория: `Contents`, `Issues`, `Pull requests` — чтение и
   запись, `Metadata` — чтение. Права организации и пользователя не нужны.
   Webhook выключен, установка — `Only on this account`.
3. Создаёт приложение, затем `Install App` → `makura-games` → `Only select repositories`
   и отмечает пять репозиториев выше.
4. Копирует `Client ID`, затем в `Private keys` нажимает `Generate a private key`.
   Скачанный `.pem` хранится вне репозитория.

Создание приложения требует подтверждения в браузере. `gh` используется для
последующей настройки и проверки через API. Манифест приложения также требует
веб-подтверждения и не устраняет этот шаг.

## Переменная и секрет

Нужны `AUTO_DRAFT_APP_CLIENT_ID` и `AUTO_DRAFT_APP_PRIVATE_KEY`. Можно создать их на
уровне организации с доступом только к пяти репозиториям. Для токена `gh` это требует
области доступа `admin:org`; при необходимости владелец выполняет
`gh auth refresh --hostname github.com --scopes admin:org`.

Если общие секреты недоступны, администратор репозиториев может настроить каждый
репозиторий отдельно, используя имеющийся доступ `repo`:

```powershell
$appClientId = Read-Host 'Client ID приложения'
$appKeyPath = Read-Host 'Полный путь к скачанному .pem'
$appPrivateKey = Get-Content -LiteralPath $appKeyPath -Raw -ErrorAction Stop
$repositories = 'sunrise-station', 'project-fire', 'lust-station', 'fish-station', 'invicta'
foreach ($repository in $repositories) {
    gh variable set AUTO_DRAFT_APP_CLIENT_ID --repo "makura-games/$repository" --body $appClientId
    if ($LASTEXITCODE -ne 0) { throw "Не удалось настроить переменную: $repository" }
    $appPrivateKey | gh secret set AUTO_DRAFT_APP_PRIVATE_KEY --repo "makura-games/$repository"
    if ($LASTEXITCODE -ne 0) { throw "Не удалось настроить секрет: $repository" }
}
```

## Проверка

Локальные проверки без доступа к GitHub и без изменения ПР:

```powershell
python Tools/_sunrise/ci/test_auto_draft_review_threads.py
node --check Tools/_sunrise/auto_draft/review_threads.js
```

Ручная проверка установленного приложения на выбранном ПР:

Для ручного запуска файл workflow с `workflow_dispatch` должен уже существовать
в основной ветке. В `sunrise-station` это условие выполнено прежней реализацией.
При первом переносе в другой репозиторий сначала нужно зарегистрировать workflow
в его основной ветке.

```powershell
gh workflow run auto-draft-review-threads.yml --repo makura-games/sunrise-station --ref fix/auto-draft -f pr-number=4644
```

При ручном запуске код берётся из коммита выбранного workflow. При автоматических
событиях — из доверенной версии workflow, а не из ветки контрибутора. Пустой
`pr-number` запускает синхронизацию всех открытых ПР и может изменить их статусы.

Проверить успешное создание токена приложения, затем на тестовом ПР проверить
`Request changes` → черновик, `Approve` → готовность к ревью и закрытие обсуждений
→ готовность к ревью. События ревью передаются через отдельный непривилегированный
workflow. Автоматическая цепочка `workflow_run` заработает после попадания файлов
в основную ветку. Закрытие обсуждений проверяется по расписанию примерно раз в пять
минут; GitHub может задержать или пропустить запуск.
Если событие не содержит номер ПР, скрипт ищет открытые ПР по коммиту сигнального
запуска. Полный обход остаётся резервом, если GitHub не вернул связанных ПР.

Старый `AUTO_DRAFT_TOKEN` удаляется отдельно в каждом репозитории только после
перехода его workflow на приложение и успешной проверки. Отзыв личного токена
выполняет выпустившая его учётная запись после проверки всех его потребителей.

Документация: [регистрация приложения](https://docs.github.com/en/apps/creating-github-apps/registering-a-github-app/registering-a-github-app),
[параметры формы](https://docs.github.com/en/apps/sharing-github-apps/registering-a-github-app-using-url-parameters),
[приложения в Actions](https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/making-authenticated-api-requests-with-a-github-app-in-a-github-actions-workflow),
[создание токена](https://github.com/actions/create-github-app-token),
[события workflow](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows),
[очередь запусков](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency).
