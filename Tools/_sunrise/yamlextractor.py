import os
import logging
from fluent.syntax import ast
from fluent.syntax.parser import FluentParser
from fluent.syntax.serializer import FluentSerializer
from file import YAMLFile, FluentFile
from fluentast import FluentSerializedMessage, FluentAstAttributeFactory
from fluentformatter import FluentFormatter
from project import Project

######################################### Class defifitions ############################################################
class YAMLExtractor:
    def __init__(self, yaml_files):
        self.yaml_files = yaml_files
        self.all_entries_by_locale = {}

    def execute(self):
        for yaml_file in self.yaml_files:
            yaml_elements = yaml_file.get_elements(yaml_file.parse_data(yaml_file.read_data()))

            if not len(yaml_elements):
                continue

            fluent_file_serialized = self.get_serialized_fluent_from_yaml_elements(yaml_elements)

            if not fluent_file_serialized:
                continue

            pretty_fluent_file_serialized = formatter.format_serialized_file_data(fluent_file_serialized)

            relative_parent_dir = yaml_file.get_relative_parent_dir(project.prototypes_dir_path).lower()
            file_name = yaml_file.get_name()

            en_fluent_file_path = self.create_or_update_fluent_file(relative_parent_dir, file_name, pretty_fluent_file_serialized, 'en-US')
            ru_fluent_file_path = self.create_or_update_fluent_file(relative_parent_dir, file_name, pretty_fluent_file_serialized, 'ru-RU')

            if en_fluent_file_path:
                self.collect_entries(en_fluent_file_path, 'en-US')
            if ru_fluent_file_path:
                self.collect_entries(ru_fluent_file_path, 'ru-RU')

        self.remove_duplicates()

    def get_serialized_fluent_from_yaml_elements(self, yaml_elements):
        fluent_serialized_messages = []

        for el in yaml_elements:
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

        if os.path.isfile(fluent_file_path):
            logging.info(f'Updating existing {locale} locale file {fluent_file_path}')
            self.update_fluent_file(fluent_file_path, file_data)
        else:
            fluent_file = FluentFile(fluent_file_path)
            fluent_file.save_data(file_data)
            logging.info(f'Created {locale} locale file {fluent_file.full_path}')

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

    def collect_entries(self, fluent_file_path, locale):
        if locale not in self.all_entries_by_locale:
            self.all_entries_by_locale[locale] = {}

        fluent_file = FluentFile(fluent_file_path)
        data = fluent_file.read_data()

        parsed = parser.parse(data)
        for entry in parsed.body:
            if isinstance(entry, ast.Message):
                if entry.id.name not in self.all_entries_by_locale[locale]:
                    self.all_entries_by_locale[locale][entry.id.name] = []
                self.all_entries_by_locale[locale][entry.id.name].append((fluent_file_path, entry))

    def remove_duplicates(self):
        for locale, entries in self.all_entries_by_locale.items():
            for entry_list in entries.values():
                if len(entry_list) > 1:
                    # Sort entries by file path to keep the latest one
                    entry_list.sort(key=lambda x: x[0])
                    for file_path, entry in entry_list[:-1]:
                        self.remove_entry(file_path, entry)

    def remove_entry(self, file_path, entry):
        fluent_file = FluentFile(file_path)
        data = fluent_file.read_data()

        parsed = parser.parse(data)
        parsed.body = [e for e in parsed.body if not (isinstance(e, ast.Message) and e.id.name == entry.id.name)]

        fluent_file.save_data(serializer.serialize(parsed))
        logging.info(f'Removed duplicate entry {entry.id.name} from {file_path}')

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

########################################################################################################################

YAMLExtractor(yaml_files).execute()
