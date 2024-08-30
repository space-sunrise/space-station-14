import os
import re
import sys

def is_english(text):
    return bool(re.search('[a-zA-Z]', text))

def has_russian(text):
    return bool(re.search('[а-яА-Я]', text))

def remove_braces_content(text):
    text1 = re.sub(r'\{.*?\}', '', text)
    text2 = re.sub(r'\[.*?\]', '', text1)
    text3 = re.sub(r'\<.*?\>', '', text2)
    return text3

def contains_ignored_word(text, ignore_list):
    return any(ignored_word in text for ignored_word in ignore_list)

def check_translations(root_dir, report_file, ignore_list, ignore_files):
    root_dir_abs = os.path.abspath(root_dir)
    found_issues = False
    localization_issues = []
    
    for dirpath, _, filenames in os.walk(root_dir):
        for filename in filenames:
            if filename.endswith('.ftl'):
                file_path = os.path.join(dirpath, filename)
                rel_path = os.path.relpath(file_path, root_dir_abs)

                # Проверка, должен ли файл быть игнорирован
                if filename in ignore_files:
                    print(f'Игнорирование файла: {filename}')
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
                                        localization_issues.append(f'Не переведённая строка "{key}" в "{rel_path}", строка {line_num}: {line.strip()}\n')
                                elif is_english(value):
                                    localization_issues.append(f'Не переведённая строка "{key}" в "{rel_path}", строка {line_num}: {line.strip()}\n')
                                    
    if localization_issues:
        found_issues = True
        with open(report_file, 'a', encoding='utf-8') as report:
            report.write("\nПроверка локализации:\n")
            report.writelines(localization_issues)

    return found_issues

def check_prototypes_for_russian(root_dir, report_file):
    root_dir_abs = os.path.abspath(root_dir)
    found_issues = False
    prototype_issues = []

    for dirpath, _, filenames in os.walk(root_dir):
        for filename in filenames:
            if filename.endswith('.yml') or filename.endswith('.yaml'):
                file_path = os.path.join(dirpath, filename)
                rel_path = os.path.relpath(file_path, root_dir_abs)
                with open(file_path, 'r', encoding='utf-8') as prototype_file:
                    lines = prototype_file.readlines()
                    for line_num, line in enumerate(lines, start=1):
                        if has_russian(line):
                            prototype_issues.append(f'Строка с русскими символами в "{rel_path}", строка {line_num}: {line.strip()}\n')

    if prototype_issues:
        found_issues = True
        with open(report_file, 'a', encoding='utf-8') as report:
            report.write("\nПроверка прототипов:\n")
            report.writelines(prototype_issues)

    return found_issues

if __name__ == "__main__":
    # Список слов или фраз, которые нужно игнорировать
    ignore_list = [
    'EntityUid', 
    'Zzz...', 
    'playglobalsound', 
    'variantize', 
    'ID', 
    'IP', 
    'TileY', 
    'TileX', 
    'X', 'Y', 
    'EI NATH', 
    '>MFW', 
    'Hello world!', 
    'green', 'red', 
    'purple', 
    'yellow', 
    'orange', 
    '$count', 
    'v1', 
    'Nanotrasen', 
    'float', 
    '$open', 
    '$rate', 
    'LV-426', 
    'LOOC', 
    'OOC', 
    'white', 
    'blue', 
    'grey', 
    '#1b487e', 
    '#B50F1D', 
    'Never gonna let you down!', 
    'Never gonna give you up!', 
    'power_net_query', 
    'zzz...', 
    'GENDER', 
    '$initialCount', 
    'Github', 
    'GitHub', 
    'Space Station 14', 
    'AHelp', 
    'TARCOL MINTI ZHERI', 
    "ONI'SOMA!", 
    'AIE KHUSE EU', 
    'Gray', 
    '$enabled', 
    'FOO-BAR-BAZ', 
    '$state', 
    'Float', 
    'Integer', 
    'left-4-zed', 
    'plant-B-gone', 
    'Pwr Game', 
    'sol dry', 
    'gray', 
    'UITest2',
    'EVA',
    'MK58',
    'WT550',
    'N1984',
    'DEUS VULT',
    'AMS-42',
    'SAM-300',
    'AJ-100',
    'Deus vult! Ave maria!',
    'TR-263',
    'G-Man',
    'RGB',
    'Changelog',
    'M1 Garand',
    'BR-64',
    'MG-42',
    'MG-60',
    'RPD',
    "Monkin' Donuts",
    'P-90',
    'MP-38',
    'MP5',
    'MP7',
    'PPSH-41',
    'AKMS',
    'AK74-U',
    'G-36',
    'M-28',
    'AR-18',
    'M16A4',
    'STG 44',
    'ACP-14',
    'Glock-22',
    'GL-79',
    'M-41',
    'DEAD-BEEF',
    'UwU',
    'C-4',
    'Who ya gonna call?',
    'Ue No',
    'Carpe diem!',
    'Space-Up!',
    'Sun-kist',
    'Robust Softdrinks',
    'C-20r',
    'Donut Corp.',
    'LV426',
    'Kept ya waiting, huh?',
    'Getmore Chocolate Corp',
    'Discount Dan',
    'L6 SAW',
    'L6C ROW',
    'China Lake',
    'Plant-B-Gone',
    'Bon appétit!',
    'Bon ap-petite!',
    'Sol dry',
    'hover entity', # Спорная хуйня, проверить в игре
    'drag shadow', # Спорная хуйня, проверить в игре
    'dbg_rotation1',
    'dbg_rotation4',
    'dbg_rotationTex',
    'plague inc 2.0',
    'False',
    'True',
    'chèvre',
    'Bon appétit!',
    'N/A',
    'GMan 2.0',
    'Susnet'
    ]  # Добавьте сюда слова, которые нужно игнорировать
    
    # Список файлов, которые нужно игнорировать
    ignore_files = ['italian.ftl', 'popup.ftl', 'controls.ftl', 'input.ftl', 'speech-chatsan.ftl', 'speech-liar.ftl', 'russian.ftl', 'german.ftl', 'southern.ftl']  # Добавьте сюда имена файлов, которые нужно игнорировать
    
    # Получаем директорию, где находится скрипт
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Устанавливаем корневую директорию и файл отчета
    localization_directory = os.path.join(script_dir, '../../Resources/Locale/ru-RU')
    prototypes_directory = os.path.join(script_dir, '../../Resources/Prototypes')
    report_path = os.path.join(script_dir, 'Report.txt')
    
    # Создаём/очищаем файл отчёта
    open(report_path, 'w').close()

    # Проверяем локализацию
    localization_issues_found = check_translations(localization_directory, report_path, ignore_list, ignore_files)
    
    # Проверяем прототипы на наличие русских символов
    #prototypes_issues_found = check_prototypes_for_russian(prototypes_directory, report_path)
    
    print(f"Отчет создан в {report_path}")