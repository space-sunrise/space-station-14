import os
import re
from datetime import datetime
from database_connect import LocalizationDB
from fluent.syntax import FluentParser, FluentSerializer

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

def parse_ftl_file(file_path):
    parser = FluentParser()
    serializer = FluentSerializer()
    
    with open(file_path, 'r', encoding='utf-8') as ftl_file:
        content = ftl_file.read()
    
    resource = parser.parse(content)
    
    translations = {}
    
    for entry in resource.body:
        if hasattr(entry, 'id'):
            key = entry.id.name
            value = serializer.serialize_entry(entry).strip().split('=', 1)[1].strip()
            translations[key] = value
    
    return translations

def check_translations(db, root_dir):
    db_translations = db.get_all_translations('strings', 'ru-RU')
    db_keys = {key for key, _, _ in db_translations}
    
    for dirpath, _, filenames in os.walk(root_dir):
        for filename in filenames:
            if filename.endswith('.ftl'):
                file_path = os.path.join(dirpath, filename)
                rel_path = os.path.relpath(file_path, root_dir)
                
                translations = parse_ftl_file(file_path)
                
                for key, value in translations.items():
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
