#!/usr/bin/env python3

import argparse
import os
import re
from glob import iglob
from typing import Any, List

errors: List["LocaleError"] = []

def main() -> int:
    parser = argparse.ArgumentParser("validate_yml.py", description="Validates YML files for Russian characters in specific fields.")
    parser.add_argument("directories", nargs="+", help="Directories to look for YML files in")

    args = parser.parse_args()

    for dir in args.directories:
        check_dir(dir)

    for error in errors:
        print(f"{error.path}: {error.message}")

    return 1 if errors else 0

def check_dir(dir: str):
    for yml_rel in iglob("**/*.yml", root_dir=dir, recursive=True):
        yml_path = os.path.join(dir, yml_rel)
        check_yml(yml_path)

def check_yml(yml_path: str):
    try:
        with open(yml_path, "r", encoding="utf-8") as file:
            content = file.read()

            # Оставляем только строки с ключами 'name:', 'description:', 'suffix:'
            filtered_content = filter_specific_keys(content)

            # Проверка нужных полей на русские символы
            for key, value in filtered_content.items():
                if has_russian_chars(value):
                    add_error(yml_path, f"Поле '{key}' содержит русские символы.")

    except Exception as e:
        add_error(yml_path, f"Ошибка чтения файла: {e}")

def filter_specific_keys(content: str) -> dict:
    result = {}
    lines = content.splitlines()
    key_pattern = re.compile(r'^(name|description|suffix):\s*(.+)')

    for line in lines:
        match = key_pattern.match(line.strip())
        if match:
            key, value = match.groups()
            result[key] = value

    return result

def has_russian_chars(text: str) -> bool:
    return bool(re.search(r'[а-яА-Я]', text))

def add_error(yml: str, message: str):
    errors.append(LocaleError(yml, message))

class LocaleError:
    def __init__(self, path: str, message: str):
        self.path = path
        self.message = message

exit(main())
