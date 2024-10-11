#!/usr/bin/env python3

import argparse
import os
import re
import yaml
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
            data = yaml.safe_load(file)  # Замените на safe_load для большей безопасности

            # Проверка нужных полей на русские символы
            for key in ['name', 'description', 'suffix']:
                if key in data and has_russian_chars(data[key]):
                    add_error(yml_path, f"Поле '{key}' содержит русские символы.")

    except yaml.YAMLError as e:
        add_error(yml_path, f"Ошибка чтения файла YAML: {e}")
    except Exception as e:
        add_error(yml_path, f"Ошибка чтения файла: {e}")

def has_russian_chars(text: str) -> bool:
    """Проверяет, содержит ли текст русские символы."""
    return bool(re.search(r'[а-яА-Я]', text))

def add_error(yml: str, message: str):
    errors.append(LocaleError(yml, message))

class LocaleError:
    def __init__(self, path: str, message: str):
        self.path = path
        self.message = message

exit(main())
