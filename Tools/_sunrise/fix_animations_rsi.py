import os
import json
import logging
from PIL import Image

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')


def is_frame_empty(frame):
    frame = frame.convert("RGBA")
    return all(pixel[3] == 0 for pixel in frame.getdata())


def process_rsi_folder(folder_path, frame_delay=0.1):
    animation_path = os.path.join(folder_path, 'animation.png')
    meta_path = os.path.join(folder_path, 'meta.json')

    if not os.path.isfile(animation_path) or not os.path.isfile(meta_path):
        logging.warning(f"Пропуск директории {folder_path}: отсутствует animation.png или meta.json")
        return

    try:
        image = Image.open(animation_path)
        with open(meta_path, 'r', encoding='utf-8') as meta_file:
            meta_data = json.load(meta_file)

        if not isinstance(meta_data.get("size"), dict) or "x" not in meta_data["size"] or "y" not in meta_data["size"]:
            logging.error(f"Неправильный формат 'size' в {meta_path}")
            return

        frame_width, frame_height = meta_data["size"]["x"], meta_data["size"]["y"]
        frames_x = image.width // frame_width
        frames_y = image.height // frame_height

        if "states" not in meta_data or not isinstance(meta_data["states"], list) or len(meta_data["states"]) == 0:
            logging.error(f"Неправильная структура 'states' в {meta_path}")
            return

        # Проверяем, есть ли delays и правильно ли они заданы
        state_delays = meta_data["states"][0].get("delays", [[]])
        if not isinstance(state_delays, list) or len(state_delays) == 0 or len(state_delays[0]) != frames_x * frames_y:
            delays = []

            for y in range(frames_y):
                for x in range(frames_x):
                    frame = image.crop((
                        x * frame_width, y * frame_height,
                        (x + 1) * frame_width, (y + 1) * frame_height
                    ))
                    if not is_frame_empty(frame):
                        delays.append(frame_delay)

            meta_data["states"][0]["delays"] = [delays]

            with open(meta_path, 'w', encoding='utf-8') as meta_file:
                json.dump(meta_data, meta_file, indent=4, ensure_ascii=False)

            logging.info(f"Обработано {folder_path}: добавлено {len(delays)} задержек для кадров")
        else:
            logging.info(f"Задержки уже заданы корректно для {folder_path}, пропуск обновления.")

    except json.JSONDecodeError:
        logging.error(f"Ошибка чтения JSON в {meta_path}", exc_info=True)
    except Exception as e:
        logging.error(f"Ошибка при обработке {folder_path}: {e}", exc_info=True)


def process_all_rsi_folders(base_path):
    for folder_name in os.listdir(base_path):
        folder_path = os.path.join(base_path, folder_name)
        if os.path.isdir(folder_path) and folder_name.endswith('.rsi'):
            logging.info(f"Начало обработки {folder_path}")
            process_rsi_folder(folder_path)


base_path = 'Resources/Textures/_Sunrise/Lobby/Animations'
process_all_rsi_folders(base_path)
