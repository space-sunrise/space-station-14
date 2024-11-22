import os
import json
import yaml
import logging

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')


def calculate_scale(original_width, original_height, target_size=2560):
    scale = round(target_size / max(original_width, original_height))
    return scale


def format_id(folder_name):
    return ''.join(word.capitalize() for word in folder_name.split('_'))


def generate_animation_prototype(rsi_path, animation_id):
    animation_path = os.path.join(rsi_path, 'animation.png')
    meta_path = os.path.join(rsi_path, 'meta.json')

    if not os.path.isfile(animation_path) or not os.path.isfile(meta_path):
        logging.warning(f"Пропуск директории {rsi_path}: отсутствует animation.png или meta.json")
        return None

    try:
        with open(meta_path, 'r', encoding='utf-8') as meta_file:
            meta_data = json.load(meta_file)

        original_width = meta_data['size']['x']
        original_height = meta_data['size']['y']
        scale = calculate_scale(original_width, original_height)

        animation_rel_path = rsi_path.replace("\\", "/").replace("Resources/Textures/", "")

        return {
            'type': 'lobbyAnimation',
            'id': format_id(animation_id),
            'animation': animation_rel_path,
            'scale': f"{scale}, {scale}"
        }

    except json.JSONDecodeError:
        logging.error(f"Ошибка чтения JSON в {meta_path}")
        return None
    except Exception as e:
        logging.error(f"Ошибка при обработке {rsi_path}: {e}")
        return None


def process_all_rsi_folders(base_path):
    prototypes = []

    for folder_name in os.listdir(base_path):
        folder_path = os.path.join(base_path, folder_name)
        if os.path.isdir(folder_path) and folder_name.endswith('.rsi'):
            animation_id = folder_name[:-4]
            logging.info(f"Обработка {folder_path}")
            prototype = generate_animation_prototype(folder_path, animation_id)
            if prototype:
                prototypes.append(prototype)

    return prototypes


def save_prototypes_to_yaml(prototypes, output_path):
    formatted_prototypes = []

    with open(output_path, 'w', encoding='utf-8') as yaml_file:
        for i, prototype in enumerate(prototypes):
            yaml.dump(formatted_prototypes, allow_unicode=True, default_flow_style=False, sort_keys=False)
            formatted_prototypes.append(prototype)
            if i < len(prototypes) - 1:
                yaml_file.write('\n')

base_path = 'Resources/Textures/_Sunrise/Lobby/Animations'
output_yaml_path = 'Resources/Prototypes/_Sunrise/Lobby/animations.yml'

prototypes = process_all_rsi_folders(base_path)
save_prototypes_to_yaml(prototypes, output_yaml_path)

logging.info(f"Прототипы сохранены в {output_yaml_path}")
