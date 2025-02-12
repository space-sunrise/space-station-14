import os
import logging
from fluent.syntax import ast
from fluent.syntax.parser import FluentParser
from fluent.syntax.serializer import FluentSerializer
from file import YAMLFile, FluentFile
from fluentast import FluentSerializedMessage, FluentAstAttributeFactory
from fluentformatter import FluentFormatter
from project import Project

class YAMLExtractor:
    def __init__(self, yaml_files):
        self.yaml_files = yaml_files

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

            self.create_or_update_fluent_file(relative_parent_dir, file_name, pretty_fluent_file_serialized, 'en-US')
            self.create_or_update_fluent_file(relative_parent_dir, file_name, pretty_fluent_file_serialized, 'ru-RU')

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
