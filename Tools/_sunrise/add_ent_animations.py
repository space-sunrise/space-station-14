import os
import yaml


def format_id(folder_name):
    return ''.join(word.capitalize() for word in folder_name.split('_'))


def generate_entity_prototype(rsi_path):
    animation_id = os.path.splitext(os.path.basename(rsi_path))[0]
    formatted_id = f"Animation{format_id(animation_id)}"

    return {
        'type': 'entity',
        'id': formatted_id,
        'placement': {
            'mode': 'SnapgridCenter'
        },
        'components': [
            {
                'type': 'Transform',
                'anchored': True
            },
            {
                'type': 'Sprite',
                'noRot': True,
                'sprite': rsi_path.replace("\\", "/").replace("Resources/Textures/", ""),
                'layers': [
                    {
                        'state': 'animation',
                        'shader': 'unshaded'
                    }
                ],
                'drawdepth': 'Overlays'
            }
        ]
    }


def process_rsi_files(base_path):
    prototypes = []

    for file_name in os.listdir(base_path):
        if file_name.endswith('.rsi'):
            rsi_path = os.path.join(base_path, file_name)
            prototype = generate_entity_prototype(rsi_path)
            prototypes.append(prototype)

    return prototypes


def save_prototypes_to_yaml(prototypes, output_path):
    yaml_content = "\n\n".join(
        yaml.dump([prototype], allow_unicode=True, default_flow_style=False, sort_keys=False).strip()
        for prototype in prototypes
    )

    with open(output_path, 'w', encoding='utf-8') as yaml_file:
        yaml_file.write(yaml_content)


base_path = 'Resources/Textures/_Sunrise/Lobby/Animations'
output_yaml_path = 'Resources/Prototypes/_Sunrise/Animations/animations.yml'

prototypes = process_rsi_files(base_path)
save_prototypes_to_yaml(prototypes, output_yaml_path)

print(f"Прототипы сохранены в {output_yaml_path}")
