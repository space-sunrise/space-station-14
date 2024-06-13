#!/usr/bin/env python3

import sys
import yaml
import re
import os
import io
import itertools
import requests
import datetime
from typing import List, Any, Iterable

MAX_ENTRIES = 5000
HEADER_RE = r"(?::cl:|🆑)\s*(.+)$"
ENTRY_RE = r"^ *[*-] *(add|remove|tweak|fix): *([^\r\n]*)"
COMMENT_RE = r"<!--.*?-->|<!--[\s\S]*?-->"

DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")

class NoDatesSafeLoader(yaml.SafeLoader):
    @classmethod
    def remove_implicit_resolver(cls, tag_to_remove):
        if not 'yaml_implicit_resolvers' in cls.__dict__:
            cls.yaml_implicit_resolvers = cls.yaml_implicit_resolvers.copy()

        for first_letter, mappings in cls.yaml_implicit_resolvers.items():
            cls.yaml_implicit_resolvers[first_letter] = [(tag, regexp)
                                                         for tag, regexp in mappings
                                                         if tag != tag_to_remove]

NoDatesSafeLoader.remove_implicit_resolver('tag:yaml.org,2002:timestamp')

TYPES_TO_EMOJI = {
    "Fix":    "🪛",
    "Add":    "🆕",
    "Remove": "❌",
    "Tweak":  "⚒️"
}

ChangelogEntry = dict[str, Any]

def parse_changelog(pr_body: str) -> List[ChangelogEntry]:
    pr_body = re.sub(COMMENT_RE, '', pr_body, flags=re.MULTILINE)
    header_match = re.search(HEADER_RE, pr_body, re.MULTILINE)
    if not header_match:
        return []

    author = header_match.group(1)

    changes = []
    for match in re.finditer(ENTRY_RE, pr_body, re.MULTILINE):
        changes.append({
            'type': match.group(1).capitalize(),
            'message': match.group(2).strip()
        })

    if not changes:
        return []

    return [{
        'author': author.strip(),
        'changes': changes
    }]

def update_changelog(changelog_file: str, pr_body: str):
    new_entries = parse_changelog(pr_body)
    if not new_entries:
        print("No changelog entries found.")
        return

    with open(changelog_file, "r", encoding="utf-8-sig") as f:
        current_data = yaml.load(f, Loader=NoDatesSafeLoader)

    if current_data is None:
        entries_list = []
    else:
        entries_list = current_data.get("Entries", [])

    max_id = max(map(lambda e: e.get("id", 0), entries_list), default=0)

    for new_entry in new_entries:
        max_id += 1
        new_entry["id"] = max_id
        new_entry["time"] = datetime.datetime.now(datetime.timezone.utc).isoformat()
        entries_list.append(new_entry)

    entries_list.sort(key=lambda e: e["id"])

    overflow = len(entries_list) - MAX_ENTRIES
    if overflow > 0:
        entries_list = entries_list[overflow:]

    new_data = {"Entries": entries_list}
    for key, value in current_data.items():
        if key != "Entries":
            new_data[key] = value

    with open(changelog_file, "w", encoding="utf-8-sig") as f:
        yaml.safe_dump(new_data, f)

    send_to_discord(new_entries)


def send_embed_discord(embed):
    data = {
        "embeds": [embed],
        "allowed_mentions": {
            "parse": []
        }
    }
    response = requests.post(DISCORD_WEBHOOK_URL, json=data)
    response.raise_for_status()


def send_to_discord(entries: Iterable[ChangelogEntry]) -> None:
    if not DISCORD_WEBHOOK_URL:
        print(f"No discord webhook URL found, skipping discord send")
        return

    for name, group in itertools.groupby(entries, lambda x: x["author"]):
        # Need to split text to avoid discord character limit
        group_content = io.StringIO()

        for entry in group:
            for change in entry["changes"]:
                emoji = TYPES_TO_EMOJI.get(change['type'], "❓")
                message = change['message']
                url = entry.get("url")
                if url and url.strip():
                    group_content.write(f"{emoji} [-]({url}) {message}\n")
                else:
                    group_content.write(f"{emoji} {message}\n")

        embed = {
            "title": f"Автор: **{name}**",
            "description": group_content,
            "color": 0x3498db
        }

        if len(group_content) > 0:
            send_embed_discord(embed)


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: update_changelog.py <changelog_file> <pr_body>")
        sys.exit(1)

    changelog_file = sys.argv[1]
    pr_body = sys.argv[2]
    update_changelog(changelog_file, pr_body)
