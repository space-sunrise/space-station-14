#!/usr/bin/env bash
# Sunrise added start - автоматическое обновление чейнджлога через GitHub Actions

set -euo pipefail

report_status() {
    local command="$1"
    local icon="$2"
    local message="$3"

    echo "::${command}::${message}"
    if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
        printf -- "- %s %s\n" "$icon" "$message" >> "$GITHUB_STEP_SUMMARY"
    fi
}

git config user.name "Sunrise-Bot"
git config user.email "sunrise.project.top@gmail.com"

for attempt in {1..5}; do
    git fetch --no-tags origin master
    git reset --hard origin/master
    git clean -fd -- Resources/Changelog/Parts .github/changelog-state.json

    arguments=(--event-path "$GITHUB_EVENT_PATH" --target-branch master)
    if [[ -n "${PR_NUMBER:-}" ]]; then
        arguments+=(--pr-number "$PR_NUMBER")
    fi

    python Tools/_sunrise/changelog/changelog_actions.py "${arguments[@]}"
    git add -- Resources/Changelog .github/changelog-state.json

    if git diff --cached --quiet; then
        report_status notice "✅" "Чейнджлог уже актуален: публикация не требуется."
        exit 0
    fi

    git commit -m "Automatic changelog update"
    if git push origin HEAD:master; then
        report_status notice "✅" "Чейнджлог успешно опубликован в master."
        exit 0
    fi

    report_status warning "⚠️" "Отправка не удалась, повторяем поверх свежего master (${attempt}/5)."
    sleep $((attempt * 2))
done

report_status error "❌" "Не удалось отправить чейнджлог после пяти попыток."
exit 1
# Sunrise added end
