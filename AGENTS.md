Перед каждым действием пользователя ты обязан прочитать skills и rules, связанные с запросом пользователя.
Из skills ты должен выбрать все, что хоть немного связаны с темой пользователя. Примеры, что и когда использовать

Папка rules - ЧИТАТЬ ВСЕГДА. Применять всегда, все что написано использовать 100% раз.

**Если требуется работать с C#/YAML/FTL/SWSL**
ss14-naming-conventions
ss14-ecs-prototypes
ss14-upstream-maintenance

**Если требуется писать C# код**
ss14-ecs-components
ss14-ecs-entities
ss14-ecs-prototypes
ss14-ecs-systems
ss14-events
ss14-prediction

**Если большой С# код(>300 строчек кода)**
ss14-documentation-writing

**Если C#, который использует часто вызываемые ивенты/Update() или нужна оптимизация**
ss14-standard-optimizations

**Если требуется делать перевод**
ss14-localization-strings

**Остальное - если связано с темой**

Пример рассуждения и выбора скилла
Пример поставленной задачи: Нужно добавить систему для проигрывания звука при спавне сущности X

**Если требуется работать с C#/YAML/FTL/SWSL** - ДА, берем блок
ss14-naming-conventions
ss14-ecs-prototypes
ss14-upstream-maintenance

**Если требуется писать C# код** - ДА, Так как нужен C# код
ss14-ecs-components
ss14-ecs-entities
ss14-ecs-prototypes
ss14-ecs-systems
ss14-events
ss14-prediction

Система маленькая - не берем оптимизации
Нет требуется перевод - не берем перевод

Требуется работа со звуком - находим скилл содержащий слово audio
ss14-audio-system-api - берем
ss14-audio-system-core - слишком продвинуто, не нужно в таком задаче - не берем.

---

Если задача составить план - включи в план скилы, которые требуются для выполнении задачи используя этот гайд
Если выполняется автоматическая чистка/компрессия контекста - перечитай все скилы и правила после нее каждый раз.

Если делаешь большую исследовательскую работу/большой код по плану - заведи временный файл для записывания всех важных мыслей или деталей.
Записывай их туда, чтобы не терять во время чисток/компрессия контекста. Включай в этот файл абсолютно все хоть сколько-то важное для задачи.
Удаляй после работы

## ТЕСТИРОВАНИЕ

В конце работы над кодом провести тестирование

Если код затрагивает прототипы(YAML/FTL) -> запускать проект `Content.YAMLLinter` командой `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build`; если линтер ещё не собран, сначала `dotnet build Content.YAMLLinter/Content.YAMLLinter.csproj --configuration Release --no-restore /m`
Если код затрагивает C# -> билдить измененный проект
Если код затрагивает клиент - запускать клиент командой `dotnet run --project Content.Client/Content.Client.csproj` или `dotnet run --project Content.Client/Content.Client.csproj --configuration Tools`, чтобы проверить runtime ошибки и IL verification.

Использовать `dotnet` по конкретному сценарию:
1. Компиляция изменённого проекта: `dotnet build <relative/path/to/project.csproj> --configuration Debug`; для CI/жёсткой проверки использовать `dotnet build <relative/path/to/project.csproj> --configuration Release --no-restore /m`
2. Запуск всех тестов решения: `dotnet test SpaceStation14.slnx --configuration DebugOpt --no-build`
3. Запуск конкретного test project: `dotnet test Content.Tests/Content.Tests.csproj --configuration DebugOpt --no-build` или `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --configuration DebugOpt --no-build`
4. Запуск конкретного теста: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --configuration DebugOpt --no-build --filter "FullyQualifiedName~GravityGridTest"`
5. Локальный запуск клиента: `dotnet run --project Content.Client/Content.Client.csproj` или `dotnet run --project Content.Client/Content.Client.csproj --configuration Tools`
6. Сборка publish-артефактов при необходимости: `dotnet publish Content.Packaging/Content.Packaging.csproj --configuration Release -r win-x64` или `dotnet publish Content.Packaging/Content.Packaging.csproj --configuration Release -p:PublishProfile=<ProfileName>`

Никогда не запускай больше двух тестов одновременно, чтобы не привести к лагам на компьютере пользователя. Идеально - по одному тесту за раз.

После обязательно завершить начатый процесс в системе!
