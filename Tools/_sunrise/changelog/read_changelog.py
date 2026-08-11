import os
import yaml
from datetime import datetime

def format_timestamp(timestamp):
    # Разбираем дату и время в формате ISO 8601.
    dt_object = datetime.fromisoformat(timestamp.replace("Z", "+00:00"))

    # Форматируем время без микросекунд.
    formatted_time = dt_object.strftime('%Y-%m-%d %H:%M')

    return formatted_time

changelog_file = os.environ.get("CHANGELOG_FILE")
if not changelog_file:
    raise RuntimeError("Переменная CHANGELOG_FILE не задана")

with open(changelog_file, "r", encoding="utf-8") as file:
    data = yaml.safe_load(file)

entries = data.get("Entries", [])

for entry in entries:
    print(f"Author: {entry['author']}")
    try:
        formatted_time = format_timestamp(entry['time'])
        print(f"Time: {formatted_time}")
    except ValueError as e:
        print(f"Error formatting time: {e}")

    print("Changes:")
    for change in entry['changes']:
        print(f"  Type: {change['type']}")
        print(f"  Message: {change['message']}")
        if 'id' in change:
            print(f"  ID: {change['id']}")
    print()
