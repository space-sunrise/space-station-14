import pathlib
import os
import glob
from file import FluentFile

class Project:
    def __init__(self):
        self.base_dir_path = pathlib.Path(__file__).parents[2].resolve()
        self.resources_dir_path = os.path.join(self.base_dir_path, 'Resources')
        self.prototypes_dir_path = os.path.join(self.resources_dir_path, "Prototypes")
        self.locales_dir_path = os.path.join(self.resources_dir_path, 'Locale')
        self.ru_locale_dir_path = os.path.join(self.locales_dir_path, 'ru-RU')
        self.en_locale_dir_path = os.path.join(self.locales_dir_path, 'en-US')
        self.en_locale_prototypes_dir_path = os.path.join(self.en_locale_dir_path, '_prototypes')
        self.ru_locale_prototypes_dir_path = os.path.join(self.ru_locale_dir_path, '_prototypes')

    def get_files_paths_by_dir(self, dir_path, files_extension):
        return glob.glob(f'{dir_path}/**/*.{files_extension}', recursive=True)

    def get_fluent_files_by_dir(self, dir_path):
        files = []
        files_paths_list = glob.glob(f'{dir_path}/**/*.ftl', recursive=True)

        for file_path in files_paths_list:
            try:
                files.append(FluentFile(file_path))
            except:
                continue

        return files

