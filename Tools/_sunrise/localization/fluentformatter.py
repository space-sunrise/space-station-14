#!/usr/bin/env python3

# Форматтер, приводящий fluent-файлы (.ftl) в соответствие стайлгайду
# path - путь к папке, содержащий форматируемые файлы. Для форматирования всего проекта, необходимо заменить значение на root_dir_path
import typing

from file import FluentFile
from project import Project
from fluent.syntax import ast, FluentParser, FluentSerializer


######################################### Class defifitions ############################################################

class FluentFormatter:
    @classmethod
    def format(cls, fluent_files: typing.List[FluentFile]):
        for file in fluent_files:
            file_data = file.read_data()
            parsed_file_data = file.parse_data(file_data)
            serialized_file_data = cls.format_serialized_file_data(file_data)
            file.save_data(serialized_file_data)

    @classmethod
    def format_serialized_file_data(cls, file_data: typing.AnyStr):
        parsed_data = FluentParser().parse(file_data)

        serialized_data = FluentSerializer(with_junk=True).serialize(parsed_data)
        
        lines = serialized_data.split('\n')
        formatted_lines = []
        for line in lines:
            if (line.strip().startswith('[color=') or 
                line.strip().startswith('[bold]') or 
                line.strip().startswith('[font') or
                line.strip().startswith('**')):
                if formatted_lines:
                    formatted_lines[-1] += ' ' + line.strip()
                else:
                    formatted_lines.append(line)
            else:
                formatted_lines.append(line)
        
        return '\n'.join(formatted_lines)



######################################## Var definitions ###############################################################
project = Project()
fluent_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)

########################################################################################################################

FluentFormatter.format(fluent_files)
