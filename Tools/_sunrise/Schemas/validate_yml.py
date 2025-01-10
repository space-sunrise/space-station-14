#!/usr/bin/env python3

import os
import re
import sys
import yaml
import argparse
from glob import iglob
from typing import List

# Общий список ошибок
errors: List["LocaleError"] = []

class LocaleError:
    def __init__(self, path: str, line: int, message: str):
        self.path = path
        self.line = line
        self.message = message

def add_error(file_path: str, line: int, message: str):
    errors.append(LocaleError(file_path, line, message))

def is_english(text):
    return bool(re.search(r'[a-zA-Z]', text))

def has_russian(text):
    return bool(re.search(r'[а-яА-Я]', text))

def remove_braces_content(text):
    text1 = re.sub(r'\{.*?\}', '', text)
    text2 = re.sub(r'\[.*?\]', '', text1)
    text3 = re.sub(r'\<.*?\>', '', text2)
    return text3

def contains_ignored_word(text, ignore_list):
    return any(ignored_word in text for ignored_word in ignore_list)

def check_translations(root_dir, ignore_list, ignore_files):
    ru_locale_dir = f'{root_dir}ru-RU/'
    en_locale_dir = f'{root_dir}en-US/'
    root_dir_abs = os.path.abspath(ru_locale_dir)

    for dirpath, _, filenames in os.walk(ru_locale_dir):
        for filename in filenames:
            if filename.endswith('.ftl'):
                file_path = os.path.join(dirpath, filename)
                rel_path = os.path.relpath(file_path, root_dir_abs)

                if filename in ignore_files:
                    #print(f'Игнорирование файла: {filename}') Не нужно, если много файлов игнорирует
                    continue

                with open(file_path, 'r', encoding='utf-8') as ftl_file:
                    lines = ftl_file.readlines()
                    for line_num, line in enumerate(lines, start=1):
                        if '=' in line and not line.strip().startswith('#'):
                            key, value = line.split('=', 1)
                            key = key.strip()
                            value = value.strip()
                            value = remove_braces_content(value)

                            if not has_russian(value) and not contains_ignored_word(value, ignore_list):
                                if key.endswith('.desc') or key.endswith('.suffix'):
                                    if is_english(value):
                                        add_error(rel_path, line_num, f'Не переведённая строка "{key}": {line.strip()}')
                                elif is_english(value):
                                    add_error(rel_path, line_num, f'Не переведённая строка "{key}": {line.strip()}')
                                    
    #for dirpath, _, filenames in os.walk(en_locale_dir):
    #    for filename in filenames:
    #        if filename.endswith('.ftl'):
    #            file_path = os.path.join(dirpath, filename)
    #            rel_path = os.path.relpath(file_path, root_dir_abs)

    #            if filename in ignore_files:
    #                #print(f'Игнорирование файла: {filename}') Не нужно, если много файлов игнорирует
    #                continue

    #            with open(file_path, 'r', encoding='utf-8') as ftl_file:
    #                lines = ftl_file.readlines()
    #                for line_num, line in enumerate(lines, start=1):
    #                    if '=' in line and not line.strip().startswith('#'):
    #                        key, value = line.split('=', 1)
    #                        key = key.strip()
    #                        value = value.strip()
    #                        value = remove_braces_content(value)

    #                        if not is_english(value) and not contains_ignored_word(value, ignore_list):
    #                            if key.endswith('.desc') or key.endswith('.suffix'):
    #                                if has_russian(value):
    #                                    add_error(rel_path, line_num, f'Русская строка "{key}": {line.strip()}')
    #                            elif has_russian(value):
    #                                add_error(rel_path, line_num, f'Русская строка "{key}": {line.strip()}')

def check_yml_files(dir: str, ignore_list: List[str]):
    key_pattern = re.compile(r'^(name|description|suffix|rules|desc):\s*(.+)')
    
    for yml_rel in iglob("**/*.yml", root_dir=dir, recursive=True):
        yml_path = os.path.join(dir, yml_rel)
        with open(yml_path, 'r', encoding='utf-8') as file:
            content = file.readlines()
            
            for i, line in enumerate(content, start=1):
                match = key_pattern.match(line.strip())
                if match:
                    key, value = match.groups()
                    if has_russian(value):
                        add_error(yml_path, i, f'Поле "{key}" содержит русские символы.')

def load_ignore_list(ignore_file):
    with open(ignore_file, 'r', encoding='utf-8') as f:
        ignore_data = yaml.safe_load(f)
    return ignore_data.get('ignore_list', []), ignore_data.get('ignore_files', [])

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Проверка локализационных файлов и YML файлов на русские символы.")
    parser.add_argument("localization_dir", help="Каталог с локализационными файлами")
    parser.add_argument("yml_dir", help="Каталог с YML файлами")
    parser.add_argument("--ignore", help="YAML-файл со списком слов и файлов для игнорирования", required=True)

    args = parser.parse_args()

    ignore_list, ignore_files = load_ignore_list(args.ignore)

    check_translations(args.localization_dir, ignore_list, ignore_files)

    check_yml_files(args.yml_dir, ignore_list)

    if errors:
        for error in errors:
            print(f"{error.path} (строка {error.line}): {error.message}")
        sys.exit(1)
    else:
        print("Ошибок не найдено.")
        sys.exit(0)