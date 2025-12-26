import os
import json
import subprocess


"""
Программа для обновления файла meta.json в директориях с изображениями PNG.

Функциональность:
1. Получает список изменений, добавленных в stage в Git, и обрабатывает только те файлы, которые имеют формат .png.
2. В каждой директории, где находится PNG-файл, ищет файл meta.json и обновляет его содержимое: заменяет license на "CLA" и copyright на "SUNRISE".
3. Если файл meta.json не найден, выводит соответствующее сообщение.

Требования:
- Файл meta.json должен находиться в той же директории, что и PNG-файлы.
- Скрипт работает в контексте репозитория Git и проверяет staged изменения.
"""


def update_meta_json(path_to_meta):
    """Обновляет meta.json, заменяя license и copyright.

    Аргументы:
    path_to_meta -- путь к файлу meta.json.
    """
    try:
        with open(path_to_meta, 'r', encoding='utf-8') as file:
            data = json.load(file)

        # Обновляем license и copyright
        data['license'] = 'CLA'
        data['copyright'] = 'SUNRISE'

        # Перезаписываем файл meta.json с обновленными данными
        with open(path_to_meta, 'w', encoding='utf-8') as file:
            json.dump(data, file, ensure_ascii=False, indent=4)
        print(f"meta.json обновлен в {path_to_meta}")
    except Exception as e:
        print(f"Ошибка при обновлении {path_to_meta}: {e}")


def process_directories(dirs: list[str]):
    """Обрабатывает список директорий, обновляет meta.json для каждого .rsi с PNG.

    Аргументы:
    dirs -- список путей к файлам, полученных из git.
    """
    for i in dirs:
        if i.endswith('.png') and '.rsi' in i:
            directory = os.path.join(i)
            meta_json_path = '/'.join(directory.split('/')[:-1]) + '/meta.json'
            if os.path.exists(meta_json_path):
                update_meta_json(meta_json_path)
            else:
                print(f"meta.json не найден в {directory}, meta json {meta_json_path}")


# Получаем список staged изменений из git
staged_changes = subprocess.run(
    ['git', 'diff', '--cached', '--name-only'],
    cwd=os.getcwd(),
    stdout=subprocess.PIPE,
    stderr=subprocess.PIPE,
    text=True
)

# Преобразуем вывод Git в список файлов и передаем на обработку
process_directories(list(staged_changes.stdout.split('\n')))
