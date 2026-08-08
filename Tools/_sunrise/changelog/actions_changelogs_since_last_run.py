#!/usr/bin/env python3
# Sunrise added start - публикация чейнджлога Sunrise в Discord
#
# Отправляет новые записи чейнджлога в вебхук Discord после последнего запуска публикации GitHub Actions.
# Автоматически определяет последний запуск и получает чейнджлог через GitHub API.
#
import io
import os
import time
import textwrap
from pathlib import Path

import requests
import yaml
from typing import Any, Iterable

DEBUG = False
DEBUG_CHANGELOG_FILE_OLD = Path("Resources/Changelog/Old.yml")
GITHUB_API_URL    = os.environ.get("GITHUB_API_URL", "https://api.github.com")
HTTP_REQUEST_TIMEOUT = 30
DISCORD_RETRY_LIMIT = 5

# https://discord.com/developers/docs/resources/webhook
DISCORD_SPLIT_LIMIT = 2000
DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")

CHANGELOG_FILE = os.environ.get("CHANGELOG_FILE", "Resources/Changelog/ChangelogSunrise.yml")

TYPES_TO_EMOJI = {
    "Fix":    "🪛",
    "Add":    "🆕",
    "Remove": "❌",
    "Tweak":  "⚒️"
}

ChangelogEntry = dict[str, Any]

def main():
    if not DISCORD_WEBHOOK_URL:
        return

    if DEBUG:
        # Для локальной отладки можно использовать отдельный файл
        # в качестве предыдущего чейнджлога.
        last_changelog_stream = DEBUG_CHANGELOG_FILE_OLD.read_text()
    else:
        # При обычном запуске через GitHub Actions предыдущий
        # чейнджлог загружается через GitHub API.
        last_changelog_stream = get_last_changelog()

    last_changelog = yaml.safe_load(last_changelog_stream)
    with open(CHANGELOG_FILE, "r") as f:
        cur_changelog = yaml.safe_load(f)

    diff = diff_changelog(last_changelog, cur_changelog)
    send_to_discord(diff)


def get_most_recent_workflow(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    workflow_run = get_current_run(sess, github_repository, github_run)
    past_runs = get_past_runs(sess, workflow_run)
    for run in past_runs["workflow_runs"]:
        # Первый предыдущий успешный запуск, отличный от текущего.
        if run["id"] == workflow_run["id"]:
            continue

        return run


def get_current_run(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/actions/runs/{github_run}",
        timeout=HTTP_REQUEST_TIMEOUT,
    )
    resp.raise_for_status()
    return resp.json()


def get_past_runs(sess: requests.Session, current_run: Any) -> Any:
    """
    Возвращает все успешные запуски рабочего процесса до текущего.
    """
    params = {"status": "success", "created": f"<={current_run['created_at']}"}
    resp = sess.get(
        f"{current_run['workflow_url']}/runs",
        params=params,
        timeout=HTTP_REQUEST_TIMEOUT,
    )
    resp.raise_for_status()
    return resp.json()


def get_last_changelog() -> str:
    github_repository = os.environ["GITHUB_REPOSITORY"]
    github_run = os.environ["GITHUB_RUN_ID"]
    github_token = os.environ["GITHUB_TOKEN"]

    session = requests.Session()
    session.headers["Authorization"] = f"Bearer {github_token}"
    session.headers["Accept"] = "application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"

    most_recent = get_most_recent_workflow(session, github_repository, github_run)
    last_sha = most_recent["head_commit"]["id"]
    print(f"Last successful publish job was {most_recent['id']}: {last_sha}")
    last_changelog_stream = get_last_changelog_by_sha(
        session, last_sha, github_repository
    )

    return last_changelog_stream

def get_last_changelog_by_sha(
    sess: requests.Session, sha: str, github_repository: str
) -> str:
    """
    Получает предыдущую версию YAML-чейнджлога через GitHub API, поскольку Actions использует неглубокий клон.
    """
    params = {
        "ref": sha,
    }
    headers = {"Accept": "application/vnd.github.raw"}

    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/contents/{CHANGELOG_FILE}",
        headers=headers,
        params=params,
        timeout=HTTP_REQUEST_TIMEOUT,
    )
    resp.raise_for_status()
    return resp.text


def diff_changelog(
    old: dict[str, Any], cur: dict[str, Any]
) -> Iterable[ChangelogEntry]:
    """
    Находит новые записи, которых не было в предыдущей публикации.
    """
    old_entry_ids = {e["id"] for e in old["Entries"]}
    return (e for e in cur["Entries"] if e["id"] not in old_entry_ids)


def get_discord_body(content: str):
    return {
        "content": content,
        # Запрещаем любые упоминания.
        "allowed_mentions": {"parse": []},
        # Флаг SUPPRESS_EMBEDS.
        "flags": 1 << 2,
    }


def send_discord(content: str):
    body = get_discord_body(content)

    response = requests.post(DISCORD_WEBHOOK_URL, json=body, timeout=HTTP_REQUEST_TIMEOUT)
    response.raise_for_status()

def send_embed_discord(embed: dict) -> None:
    headers = {
        "Content-Type": "application/json"
    }

    payload = {
        "embeds": [embed]
    }

    for retry_count in range(DISCORD_RETRY_LIMIT + 1):
        response = requests.post(
            DISCORD_WEBHOOK_URL,
            json=payload,
            headers=headers,
            timeout=HTTP_REQUEST_TIMEOUT,
        )

        if response.status_code == 204:
            return
        if response.status_code == 429 and retry_count < DISCORD_RETRY_LIMIT:
            retry_after = response.json().get("retry_after", 1)
            print(f"Rate limited: sleeep {retry_after} seconds")
            time.sleep(retry_after)
            continue

        response.raise_for_status()
        raise RuntimeError(f"Discord webhook вернул неожиданный статус {response.status_code}")


def split_message(message: str, limit: int = DISCORD_SPLIT_LIMIT) -> list[str]:
    return textwrap.wrap(message, width=limit, replace_whitespace=False)

def send_to_discord(entries: Iterable[ChangelogEntry]) -> None:
    if not DISCORD_WEBHOOK_URL:
        print("No discord webhook URL found, skipping discord send")
        return

    for entry in entries:
        content_string = io.StringIO()
        for change in entry["changes"]:
            emoji = TYPES_TO_EMOJI.get(change['type'], "❓")
            message = change['message']
            content_string.write(f"{emoji} {message}\n")
        url = entry.get("url")
        if url and url.strip():
            content_string.write(f"[GitHub Pull Request]({url})\n")

        full_content = content_string.getvalue()
        parts = split_message(full_content, DISCORD_SPLIT_LIMIT)

        for part in parts:
            embed = {
                "title": f"Автор: **{entry['author']}**",
                "description": part,
                "color": 0x3498db
            }
            if len(part) > 0:
                send_embed_discord(embed)


if __name__ == "__main__":
    main()
# Sunrise added end
