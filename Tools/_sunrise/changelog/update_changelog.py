#!/usr/bin/env python3

import argparse
import datetime
import os
from typing import Any, List

import yaml

MAX_ENTRIES = 5000
CATEGORY_MAIN = "Main"


class NoDatesSafeLoader(yaml.SafeLoader):
    @classmethod
    def remove_implicit_resolver(cls, tag_to_remove):
        if "yaml_implicit_resolvers" not in cls.__dict__:
            cls.yaml_implicit_resolvers = cls.yaml_implicit_resolvers.copy()

        for first_letter, mappings in cls.yaml_implicit_resolvers.items():
            cls.yaml_implicit_resolvers[first_letter] = [
                (tag, regexp)
                for tag, regexp in mappings
                if tag != tag_to_remove
            ]


NoDatesSafeLoader.remove_implicit_resolver("tag:yaml.org,2002:timestamp")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("changelog_file")
    parser.add_argument("parts_dir")
    parser.add_argument("--category", default=CATEGORY_MAIN)
    args = parser.parse_args()

    with open(args.changelog_file, "r", encoding="utf-8-sig") as file:
        current_data = yaml.load(file, Loader=NoDatesSafeLoader)

    entries_list: List[Any] = [] if current_data is None else current_data.get("Entries") or []
    max_id = max(
        (entry["id"] for entry in entries_list if isinstance(entry, dict) and type(entry.get("id")) is int),
        default=0,
    )

    for part_name in sorted(os.listdir(args.parts_dir)):
        if not part_name.endswith(".yml"):
            continue

        part_path = os.path.join(args.parts_dir, part_name)
        print(part_path)
        with open(part_path, "r", encoding="utf-8-sig") as file:
            part = yaml.load(file, Loader=NoDatesSafeLoader)

        part_category = part.get("category", CATEGORY_MAIN)
        if part_category != args.category:
            print(f"Skipping: wrong category ({part_category} vs {args.category})")
            continue

        changes = part["changes"]
        if not isinstance(changes, list):
            changes = [changes]

        if changes:
            max_id += 1
            entry = {
                "author": part["author"],
                "time": part.get("time", datetime.datetime.now(datetime.timezone.utc).isoformat()),
                "changes": changes,
                "id": max_id,
                "url": part.get("url"),
            }
            if media := part.get("media"):
                entry["media"] = media
            entries_list.append(entry)

        os.remove(part_path)

    print(f"Have {len(entries_list)} changelog entries")
    entries_list.sort(
        key=lambda entry: entry["id"]
        if isinstance(entry, dict) and type(entry.get("id")) is int
        else 0,
    )
    overflow = len(entries_list) - MAX_ENTRIES
    if overflow > 0:
        print(f"Removing {overflow} old entries.")
        entries_list = entries_list[overflow:]

    new_data = {"Entries": entries_list}
    if current_data is not None:
        new_data.update((key, value) for key, value in current_data.items() if key != "Entries")

    with open(args.changelog_file, "w", encoding="utf-8-sig") as file:
        yaml.safe_dump(new_data, file, allow_unicode=True, sort_keys=False)


main()
