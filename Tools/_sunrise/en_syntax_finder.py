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

def update_or_insert_translation(db, table, key, value, locale, rel_path):
    existing_value = db.get_translation(table, key, locale)
    
    if existing_value:
        if existing_value != value:
            # Сравниваем временные метки
            db_last_updated = db.get_last_updated(table, key, locale)
            file_last_updated = datetime.fromtimestamp(os.path.getmtime(rel_path))
            
            if db_last_updated and datetime.fromisoformat(db_last_updated) > file_last_updated:
                # Обновляем файл значением из базы данных
                print(f'Обновление строки в файле: {key}')
                return existing_value
            else:
                # Обновляем базу данных значением из файла
                print(f'Обновление строки в базе данных: {key}')
                db.update_translation(table, key, locale, value)
    else:
        # Вставляем новую строку в базу данных
        print(f'Добавление новой строки в базу данных: {key}')
        db.insert_translation(table, key, value, value)
    
    return value

def check_translations(db, root_dir):
    # Список всех ключей в базе данных
    db_keys = {key for key, _ in db.get_all_translations('strings', 'ru-RU')}

    # Проходим по файлам локализации и проверяем
    for dirpath, _, filenames in os.walk(root_dir):
        for filename in filenames:
            if filename.endswith('.ftl'):
                file_path = os.path.join(dirpath, filename)
                rel_path = os.path.relpath(file_path, root_dir)

                with open(file_path, 'r', encoding='utf-8') as ftl_file:
                    lines = ftl_file.readlines()
                    for line_num, line in enumerate(lines, start=1):
                        if '=' in line and not line.strip().startswith('#'):
                            key, value = line.split('=', 1)
                            key = key.strip()
                            value = value.strip()
                            value = remove_braces_content(value)

                            if not has_russian(value):
                                if key not in db_keys:
                                    # Добавляем новые строки в базу данных
                                    print(f'Добавление новой строки в базу данных: {key}')
                                    db.insert_translation('strings', key, value, value)
                                else:
                                    # Обновляем существующие строки в базе данных
                                    existing_value = db.get_translation('strings', key, 'ru-RU')[1]
                                    if existing_value != value:
                                        db.update_translation('strings', key, 'ru-RU', value)


if __name__ == "__main__":
    # Подключение к базе данных
    db = LocalizationDB()

    # Получаем директорию, где находится скрипт
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Устанавливаем корневую директорию для локализационных файлов
    localization_directory = os.path.join(script_dir, '../../Resources/Locale/ru-RU')
    
    # Проверяем локализацию и обновляем базу данных
    check_translations(db, localization_directory)
    
    # Закрываем соединение с базой данных
    db.close()

    print("Проверка завершена.")
