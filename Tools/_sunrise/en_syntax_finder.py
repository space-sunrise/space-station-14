import os
import re
import sys
from datetime import datetime
from database_connect import LocalizationDB

def is_english(text):
    return bool(re.search('[a-zA-Z]', text))

def has_russian(text):
    return bool(re.search('[а-яА-Я]', text))

def remove_braces_content(text):
    text1 = re.sub(r'\{.*?\}', '', text)
    text2 = re.sub(r'\[.*?\]', '', text1)
    text3 = re.sub(r'\<.*?\>', '', text2)
    return text3

def extract_value_from_lines(lines):
    value_lines = []
    inside_multiline = False
    for line in lines:
        line = line.strip()
        if line.endswith('='):
            inside_multiline = True
            continue
        if inside_multiline:
            if line:
                value_lines.append(line)
            else:
                inside_multiline = False
        else:
            if line:
                if line.startswith('='):
                    value_lines.append(line[1:].strip())
                else:
                    value_lines.append(line)
    return '\n'.join(value_lines)

def update_or_insert_translation(db, table, key, value, locale, rel_path):
    existing_value = db.get_translation(table, key, locale)
    
    if existing_value:
        if existing_value != value:
            db_last_updated = db.get_last_updated(table, key, locale)
            file_last_updated = datetime.fromtimestamp(os.path.getmtime(rel_path))
            
            if db_last_updated and datetime.fromisoformat(db_last_updated) > file_last_updated:
                print(f'Обновление строки в файле: {key}')
                return existing_value
            else:
                print(f'Обновление строки в базе данных: {key}')
                db.update_translation(table, key, locale, value)
    else:
        print(f'Добавление новой строки в базу данных: {key}')
        db.insert_translation(table, key, value, value)
    
    return value

def check_translations(db, root_dir):
    db_translations = db.get_all_translations('strings', 'ru-RU')
    db_keys = {key for key, _, _ in db_translations}
    
    for dirpath, _, filenames in os.walk(root_dir):
        for filename in filenames:
            if filename.endswith('.ftl'):
                file_path = os.path.join(dirpath, filename)
                rel_path = os.path.relpath(file_path, root_dir)

                with open(file_path, 'r', encoding='utf-8') as ftl_file:
                    lines = ftl_file.readlines()
                    key = None
                    value_lines = []
                    
                    for line_num, line in enumerate(lines, start=1):
                        line = line.strip()
                        if '=' in line and not line.startswith('#'):
                            if key:
                                value = extract_value_from_lines(value_lines)
                                value = remove_braces_content(value)
                                
                                if 'ru-RU' in file_path:
                                    if not has_russian(value):
                                        if key not in db_keys:
                                            print(f'Добавление новой строки в базу данных: {key}')
                                            db.insert_translation('strings', key, value, value)
                                        else:
                                            existing_value = db.get_translation('strings', key, 'ru-RU')[1]
                                            if existing_value != value:
                                                db.update_translation('strings', key, 'ru-RU', value)
                                elif 'en-US' in file_path:
                                    if not is_english(value):
                                        if key not in db_keys:
                                            print(f'Добавление новой строки в базу данных: {key}')
                                            db.insert_translation('strings', key, value, value)
                                        else:
                                            existing_value = db.get_translation('strings', key, 'en-US')[1]
                                            if existing_value != value:
                                                db.update_translation('strings', key, 'en-US', value)
                            
                            key, _ = line.split('=', 1)
                            key = key.strip()
                            value_lines = []
                        elif key:
                            value_lines.append(line)
                    
                    if key:
                        value = extract_value_from_lines(value_lines)
                        value = remove_braces_content(value)
                        
                        if 'ru-RU' in file_path:
                            if not has_russian(value):
                                if key not in db_keys:
                                    print(f'Добавление новой строки в базу данных: {key}')
                                    db.insert_translation('strings', key, value, value)
                                else:
                                    existing_value = db.get_translation('strings', key, 'ru-RU')[1]
                                    if existing_value != value:
                                        db.update_translation('strings', key, 'ru-RU', value)
                        elif 'en-US' in file_path:
                            if not is_english(value):
                                if key not in db_keys:
                                    print(f'Добавление новой строки в базу данных: {key}')
                                    db.insert_translation('strings', key, value, value)
                                else:
                                    existing_value = db.get_translation('strings', key, 'en-US')[1]
                                    if existing_value != value:
                                        db.update_translation('strings', key, 'en-US', value)

if __name__ == "__main__":
    db = LocalizationDB()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    localization_directory = os.path.join(script_dir, '../../Resources/Locale')
    
    check_translations(db, localization_directory)
    
    db.close()

    print("Проверка завершена.")
