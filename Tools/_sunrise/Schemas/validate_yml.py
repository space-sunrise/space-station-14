#!/usr/bin/env python3

import argparse
import os
import re
from glob import iglob
from typing import List

errors: List["LocaleError"] = []

def main() -> int:
    parser = argparse.ArgumentParser("validate_yml.py", description="Validates YML files for Russian characters in specific fields.")
    parser.add_argument("directories", nargs="+", help="Directories to look for YML files in")

    args = parser.parse_args()

    for dir in args.directories:
        check_dir(dir)

    for error in errors:
        print(f"{error.path} (строка {error.line}): {error.message}")

    return 1 if errors else 0

def check_dir(dir: str):
    for yml_rel in iglob("**/*.yml", root_dir=dir, recursive=True):
        yml_path = os.path.join(dir, yml_rel)
        check_yml(yml_path)

def check_yml(yml_path: str):
    try:
        with open(yml_path, "r", encoding="utf-8") as file:
            content = file.readlines()

            # Оставляем только строки с ключами 'name:', 'description:', 'suffix:', 'rules:'
            filter_and_check(content, yml_path)

    except Exception as e:
        add_error(yml_path, -1, f"Ошибка чтения файла: {e}")

def filter_and_check(content: List[str], yml_path: str):
    key_pattern = re.compile(r'^(name|description|suffix|rules):\s*(.+)')

    for i, line in enumerate(content, start=1):
        match = key_pattern.match(line.strip())
        if match:
            key, value = match.groups()
            if has_russian_chars(value):
                add_error(yml_path, i, f"Поле '{key}' содержит русские символы.")

def has_russian_chars(text: str) -> bool:
    return bool(re.search(r'[а-яА-Я]', text))

def add_error(yml: str, line: int, message: str):
    errors.append(LocaleError(yml, line, message))

class LocaleError:
    def __init__(self, path: str, line: int, message: str):
        self.path = path
        self.line = line
        self.message = message

exit(main())
