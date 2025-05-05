import os
import logging
from fluent.syntax import ast
from fluent.syntax.parser import FluentParser
from fluent.syntax.serializer import FluentSerializer
from file import YAMLFile, FluentFile
from fluentast import FluentSerializedMessage, FluentAstAttributeFactory
from fluentformatter import FluentFormatter
from project import Project
from tqdm import tqdm

class YAMLExtractor:
    def __init__(self, yaml_files):
        self.yaml_files = yaml_files
        self.existing_ids_by_locale = {}
        self.entries_to_remove = []

    def scan_existing_locale_files(self):
        locales = self.get_locales_from_dir(project.locales_dir_path)
        for locale in tqdm(locales, desc="Сканирование локалей"):
            self.existing_ids_by_locale[locale] = {}
            locale_dir_path = os.path.join(project.locales_dir_path, locale)
            fluent_files = project.get_fluent_files_by_dir(locale_dir_path)
            for fluent_file in tqdm(fluent_files, desc=f"Обработка файлов в {locale}", leave=False):
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

    def get_locales_from_dir(self, locales_dir_path):
        return [name for name in os.listdir(locales_dir_path)
                if os.path.isdir(os.path.join(locales_dir_path, name))]

    def get_correct_path_for_entry(self, entry_id, locale, relative_parent_dir, file_name):
        locale_attr = f'{locale.split("-")[0]}_locale_prototypes_dir_path'
        return os.path.join(getattr(project, locale_attr), relative_parent_dir, f'{file_name}.ftl')

    def remove_entry_from_file(self, file_path, entry_id):
        fluent_file = FluentFile(file_path)
        data = fluent_file.read_data()
        parsed = parser.parse(data)
        parsed.body = [e for e in parsed.body if not (isinstance(e, ast.Message) and e.id.name == entry_id)]
        fluent_file.save_data(serializer.serialize(parsed))
        rel_path = os.path.relpath(file_path, project.base_dir_path)
        logging.debug(f'Удален дублирующийся элемент {entry_id} из {rel_path}')

    def execute(self):
        self.scan_existing_locale_files()

        for yaml_file in tqdm(self.yaml_files, desc="Обработка YAML файлов"):
            yaml_elements = yaml_file.get_elements(yaml_file.parse_data(yaml_file.read_data()))

            if not len(yaml_elements):
                continue

            fluent_file_serialized = self.get_serialized_fluent_from_yaml_elements(yaml_elements)

            if not fluent_file_serialized:
                continue

            pretty_fluent_file_serialized = formatter.format_serialized_file_data(fluent_file_serialized)

            relative_parent_dir = yaml_file.get_relative_parent_dir(project.prototypes_dir_path).lower()
            file_name = yaml_file.get_name()

            self.create_or_update_fluent_file(relative_parent_dir, file_name, pretty_fluent_file_serialized, 'en-US')
            self.create_or_update_fluent_file(relative_parent_dir, file_name, pretty_fluent_file_serialized, 'ru-RU')

        if self.entries_to_remove:
            for path, entry_id in tqdm(self.entries_to_remove, desc="Удаление дублей"):
                self.remove_entry_from_file(path, entry_id)

    def get_serialized_fluent_from_yaml_elements(self, yaml_elements):
        fluent_serialized_messages = []

        for el in tqdm(yaml_elements, desc="Обработка YAML элементов", leave=False):
            if isinstance(el.parent_id, list):
                el.parent_id = el.parent_id[0]

            fluent_message = FluentSerializedMessage.from_yaml_element(el.id, el.name, FluentAstAttributeFactory.from_yaml_element(el), el.parent_id)
            if fluent_message:
                fluent_serialized_messages.append(fluent_message)

        if not fluent_serialized_messages:
            return None

        return '\n'.join(fluent_serialized_messages)

    def create_or_update_fluent_file(self, relative_parent_dir, file_name, file_data, locale):
        locale_attr = f'{locale.split("-")[0]}_locale_prototypes_dir_path'
        new_dir_path = os.path.join(getattr(project, locale_attr), relative_parent_dir)
        os.makedirs(new_dir_path, exist_ok=True)

        fluent_file_path = os.path.join(new_dir_path, f'{file_name}.ftl')

        parsed_new = parser.parse(file_data)
        new_entries = {}

        for entry in parsed_new.body:
            if isinstance(entry, ast.Message):
                entry_id = entry.id.name
                translation_found = False

                if entry_id in self.existing_ids_by_locale[locale]:
                    existing_paths = self.existing_ids_by_locale[locale][entry_id]
                    for existing_path in existing_paths:
                        if existing_path != fluent_file_path:
                            try:
                                existing_file = FluentFile(existing_path)
                                existing_data = existing_file.read_data()
                                existing_parsed = parser.parse(existing_data)

                                for existing_entry in existing_parsed.body:
                                    if isinstance(existing_entry, ast.Message) and existing_entry.id.name == entry_id:
                                        new_entries[entry_id] = existing_entry
                                        translation_found = True
                                        self.entries_to_remove.append((existing_path, entry_id))
                                        rel_existing_path = os.path.relpath(existing_path, project.base_dir_path)
                                        rel_new_path = os.path.relpath(fluent_file_path, project.base_dir_path)
                                        logging.debug(f'Найден существующий перевод для {entry_id} в неверном месте: {rel_existing_path}')
                                        logging.debug(f'Будет перемещен в: {rel_new_path}')
                                        logging.debug(f'Для сущности {entry_id} используется существующий перевод')
                                        break
                            except Exception as e:
                                rel_path = os.path.relpath(existing_path, project.base_dir_path)
                                logging.error(f"Ошибка при чтении файла {rel_path}: {str(e)}")

                        if translation_found:
                            break

                if not translation_found:
                    new_entries[entry_id] = entry
                else:
                    logging.debug(f'Для сущности {entry_id} используется существующий перевод')

        new_body = list(new_entries.values())
        file_data = serializer.serialize(ast.Resource(body=new_body))

        rel_path = os.path.relpath(fluent_file_path, project.base_dir_path)
        if os.path.isfile(fluent_file_path):
            logging.debug(f'Обновление существующего файла локали {locale} {rel_path}')
            self.update_fluent_file(fluent_file_path, file_data)
        else:
            fluent_file = FluentFile(fluent_file_path)
            fluent_file.save_data(file_data)
            logging.debug(f'Создан файл локали {locale} {rel_path}')

        return fluent_file_path

    def update_fluent_file(self, fluent_file_path, new_data):
        fluent_file = FluentFile(fluent_file_path)
        existing_data = fluent_file.read_data()

        existing_parsed = parser.parse(existing_data)
        new_parsed = parser.parse(new_data)

        existing_entries = {entry.id.name: entry for entry in existing_parsed.body if isinstance(entry, ast.Message)}
        new_entries = {entry.id.name: entry for entry in new_parsed.body if isinstance(entry, ast.Message)}

        # Merge new entries into existing entries, giving priority to existing entries
        merged_entries = {**new_entries, **existing_entries}

        # Serialize the merged entries back to the file
        merged_parsed = ast.Resource(body=list(merged_entries.values()))
        fluent_file.save_data(serializer.serialize(merged_parsed))

logging.basicConfig(level=logging.INFO)
project = Project()
serializer = FluentSerializer()
parser = FluentParser()
formatter = FluentFormatter()

logging.info('Searching for YAML files...')
yaml_files_paths = project.get_files_paths_by_dir(project.prototypes_dir_path, 'yml')
if not yaml_files_paths:
    logging.info("No YAML files found!")
else:
    logging.info(f"Found {len(yaml_files_paths)} YAML files. Processing...")
yaml_files = list(map(lambda yaml_file_path: YAMLFile(yaml_file_path), yaml_files_paths))

YAMLExtractor(yaml_files).execute()
