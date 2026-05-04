bounty-console-menu-title = Консоль запросов
bounty-console-label-button-text = Распечатать этикетку
bounty-console-skip-button-text = Пропустить
bounty-console-time-label = Время: [color=orange]{ $time }[/color]
bounty-console-reward-label = Награда: [color=limegreen]${ $reward }[/color]
bounty-console-manifest-label = Манифест: [color=orange]{ $item }[/color]
bounty-console-manifest-entry =
    { $amount ->
        [1] { $item }
       *[other] { $item } x{ $amount }
    }
bounty-console-manifest-reward = Награда: ${ $reward }
bounty-console-description-label = [color=gray]{ $description }[/color]
bounty-console-id-label = ID#{ $id }
bounty-console-flavor-left = Запросы, полученные от местных недобросовестных торговцев.
bounty-console-flavor-right = v1.4
bounty-manifest-header = [font size=14][bold]Официальный манифест запроса[/bold] (ID#{ $id })[/font]
bounty-manifest-list-start = Манифест:
bounty-console-tab-available-label = Доступные
bounty-console-tab-history-label = История
bounty-console-history-empty-label = История запросов не найдена
bounty-console-history-notice-completed-label = [color=limegreen]Выполнено[/color]
bounty-console-history-notice-skipped-label = [color=red]Пропущено[/color] пользователем { $id }
# Sunrise edit start
cargo-console-menu-tab-title-orders = Заказы
cargo-console-menu-tab-title-funds = Переводы
cargo-console-menu-account-action-transfer-limit = [bold]Лимит перевода:[/bold] ${$limit}
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=gold](Лимит снят!)[/color]
cargo-console-menu-account-action-select = [bold]Действие со счётом:[/bold]
cargo-console-menu-account-action-amount = [bold]Сумма:[/bold] $
cargo-console-menu-account-action-button = Перевести
cargo-console-menu-toggle-account-lock-button = Переключить лимит перевода
cargo-console-menu-account-action-option-withdraw = Снять наличные
cargo-console-menu-account-action-option-transfer = Перевести средства на {$code}
cargo-console-unlock-approved-order-broadcast = [bold]{$productName} x{$orderAmount}[/bold], стоимостью [bold]{$cost}[/bold], одобрен сотрудником [bold]{$approver}[/bold]
cargo-console-fund-withdraw-broadcast = [bold]{$name} снял(а) {$amount} $ со счёта {$name1} \[{$code1}\][/bold]
cargo-console-fund-transfer-broadcast = [bold]{$name} перевёл(а) {$amount} $ со счёта {$name1} \[{$code1}\] на счёт {$name2} \[{$code2}\][/bold]
cargo-console-fund-transfer-user-unknown = Неизвестный
# Sunrise edit end
