import os
import logging
from fluent.syntax import ast
from fluent.syntax.parser import FluentParser
from fluent.syntax.serializer import FluentSerializer
from file import YAMLFile, FluentFile
from project import Project

class DuplicateRemover:
    def __init__(self):
        self.existing_ids_by_locale = {}
        self.yaml_file_paths = self.get_yaml_file_paths()

    def execute(self):
        self.scan_existing_locale_files()
        self.remove_duplicates()

    def scan_existing_locale_files(self):
        locales = self.get_locales_from_dir(project.locales_dir_path)
        for locale in locales:
            self.existing_ids_by_locale[locale] = {}
            locale_dir_path = os.path.join(project.locales_dir_path, locale)
            fluent_files = project.get_fluent_files_by_dir(locale_dir_path)
            for fluent_file in fluent_files:
                self.collect_existing_ids(fluent_file.full_path, locale)

    def collect_existing_ids(self, fluent_file_path, locale):
        fluent_file = FluentFile(fluent_file_path)
        data = fluent_file.read_data()
        parsed = parser.parse(data)
        for entry in parsed.body:
            if isinstance(entry, ast.Message):
                if entry.id.name in self.existing_ids_by_locale[locale]:
                    self.existing_ids_by_locale[locale][entry.id.name].append(fluent_file_path)
                else:
                    self.existing_ids_by_locale[locale][entry.id.name] = [fluent_file_path]

    def remove_duplicates(self):
        for locale, ids in self.existing_ids_by_locale.items():
            for entry_id, paths in ids.items():
                if len(paths) > 1:
                    correct_path = self.get_correct_path(entry_id, locale)
                    for path in paths:
                        if path != correct_path:
                            self.remove_entry(path, entry_id)

    def get_correct_path(self, entry_id, locale):
        for yaml_file_path in self.yaml_file_paths:
            yaml_file = YAMLFile(yaml_file_path)
            yaml_elements = yaml_file.get_elements(yaml_file.parse_data(yaml_file.read_data()))
            for el in yaml_elements:
                if el.id == entry_id:
                    relative_parent_dir = yaml_file.get_relative_parent_dir(project.prototypes_dir_path).lower()
                    locale_attr = f'{locale.split("-")[0]}_locale_prototypes_dir_path'
                    return os.path.join(getattr(project, locale_attr), relative_parent_dir, f'{yaml_file.get_name()}.ftl')
        return None

    def remove_entry(self, file_path, entry_id):
        fluent_file = FluentFile(file_path)
        data = fluent_file.read_data()
        parsed = parser.parse(data)
        parsed.body = [e for e in parsed.body if not (isinstance(e, ast.Message) and e.id.name == entry_id)]
        fluent_file.save_data(serializer.serialize(parsed))
        logging.info(f'Removed duplicate entry {entry_id} from {file_path}')

    def get_locales_from_dir(self, locales_dir_path):
        return [name for name in os.listdir(locales_dir_path) if os.path.isdir(os.path.join(locales_dir_path, name))]

    def get_yaml_file_paths(self):
        return project.get_files_paths_by_dir(project.prototypes_dir_path, 'yml')

logging.basicConfig(level=logging.INFO)
project = Project()
serializer = FluentSerializer()
parser = FluentParser()

DuplicateRemover().execute()
