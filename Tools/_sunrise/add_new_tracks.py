import os
from ruamel.yaml import YAML
from ruamel.yaml.comments import CommentedSeq

tapes_file_path = 'Resources/Prototypes/_Sunrise/Entities/Objects/Specific/tapes.yml'
audio_dir_path = 'Resources/Audio/_Sunrise/TapePlayer/Tracks'

yaml = YAML()
yaml.default_flow_style = False


def read_yaml(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        return yaml.load(file)


def write_yaml(file_path, data):
    with open(file_path, 'w', encoding='utf-8') as file:
        yaml.dump(data, file)


all_tracks = [f for f in os.listdir(audio_dir_path) if f.endswith('.ogg')]

new_tapes = []
tape_counter = 1


def get_tape_state(counter):
    return f'tape{(counter - 1) % 40 + 1}'


base_tape = {
    'type': 'entity',
    'parent': 'BaseItem',
    'id': 'BaseMusicTape',
    'name': 'tape',
    'abstract': True,
    'description': 'this is tape',
    'components': [
        {'type': 'Item', 'size': 'Tiny'},
        {'type': 'Sprite', 'sprite': '_Sunrise/Objects/Devices/tapes.rsi'}
    ]
}
new_tapes.append(base_tape)

for track in all_tracks:
    track_path = f'/Audio/_Sunrise/TapePlayer/Tracks/{track}'
    new_tape = {
        'type': 'entity',
        'parent': 'BaseMusicTape',
        'id': f'MusicTape{tape_counter}',
        'name': f'кассета магнитофона ({os.path.splitext(track)[0].replace('_', ' ')})',
        'description': None,
        'components': [
            {'type': 'Sprite', 'state': get_tape_state(tape_counter)},
            {'type': 'MusicTape', 'songName': os.path.splitext(track)[0].replace('_', ' '), 'sound': track_path}
        ]
    }
    new_tapes.append(new_tape)
    tape_counter += 1

formatted_tapes_data = CommentedSeq()

for new_tape in new_tapes:
    formatted_tapes_data.append(new_tape)
    formatted_tapes_data.yaml_set_comment_before_after_key(len(formatted_tapes_data) - 1, before='\n')

write_yaml(tapes_file_path, formatted_tapes_data)

print(f'Добавлено {len(new_tapes)} новых кассет.')
