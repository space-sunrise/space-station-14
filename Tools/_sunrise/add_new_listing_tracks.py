import os
from ruamel.yaml import YAML
from ruamel.yaml.comments import CommentedSeq

tapes_file_path = 'Resources/Prototypes/_Sunrise/Entities/Objects/Specific/tapes.yml'
catalog_file_path = 'Resources/Prototypes/_Sunrise/Catalog/sponsor_uplink_catalog_music.yml'

yaml = YAML()
yaml.default_flow_style = False


def read_yaml(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        return yaml.load(file)


def write_yaml(file_path, data):
    with open(file_path, 'w', encoding='utf-8') as file:
        yaml.dump(data, file)


try:
    tapes_data = read_yaml(tapes_file_path)
    if not isinstance(tapes_data, list):
        raise ValueError(f"{tapes_file_path} must contain a list of tapes.")
except Exception as e:
    print(f"Error loading {tapes_file_path}: {e}")
    exit(1)

print(f"Successfully loaded {len(tapes_data)} tapes from {tapes_file_path}.")

new_listings = CommentedSeq()


def get_tape_state(counter):
    return f'tape{(counter - 1) % 40 + 1}'


tape_counter = 1

for tape in tapes_data:
    tape_id = tape['id']
    tape_name = tape['name']

    if tape_id == "BaseMusicTape":
        continue

    state = get_tape_state(tape_counter)

    icon_value = yaml.load(f"{{ sprite: _Sunrise/Interface/Misc/icons.rsi, state: {state} }}")

    listing = {
        'type': 'listing',
        'id': f'UplinkSunrise{tape_id}',
        'name': tape_name,
        'productEntity': tape_id,
        'icon': icon_value,
        'cost': {'Suntick': 3},
        'categories': ['Music'],
        'conditions': [
            # Правильный формат с использованием тега
            yaml.load("!type:ListingLimitedStockCondition\n"
                      "stock: 1")
        ]
    }
    new_listings.append(listing)
    new_listings.yaml_set_comment_before_after_key(len(new_listings) - 1, before='\n')

    tape_counter += 1

catalog_data = CommentedSeq()
print(f"Adding {len(new_listings)} new listings...")

for listing in new_listings:
    catalog_data.append(listing)
    catalog_data.yaml_set_comment_before_after_key(len(catalog_data) - 1, before='\n')

write_yaml(catalog_file_path, catalog_data)

print(f"Successfully added {len(new_listings)} new listings to '{catalog_file_path}'.")
