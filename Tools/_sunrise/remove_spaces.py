import os

audio_dir_path = 'Resources/Audio/_Sunrise/TapePlayer/Tracks'

files = os.listdir(audio_dir_path)

for filename in files:
    new_filename = filename.replace(' ', '_')
    if new_filename != filename:
        old_file_path = os.path.join(audio_dir_path, filename)
        new_file_path = os.path.join(audio_dir_path, new_filename)
        try:
            os.rename(old_file_path, new_file_path)
            print(f'Переименован: {filename} -> {new_filename}')
        except FileExistsError:
            print(f'Файл {new_filename} уже существует')

print('Все пробелы в названиях файлов заменены на подчеркивания.')
