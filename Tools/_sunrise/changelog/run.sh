#!/usr/bin/env bash

set -euo pipefail

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
        echo "Чейнджлог уже актуален."
        exit 0
    fi

    git commit -m "Automatic changelog update"
    if git push origin HEAD:master; then
        exit 0
    fi

    echo "Отправка не удалась, повторяем поверх свежего master (${attempt}/5)."
    sleep $((attempt * 2))
done

echo "Не удалось отправить чейнджлог после пяти попыток." >&2
exit 1
