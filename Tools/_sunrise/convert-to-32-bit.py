import os
import argparse
from PIL import Image

parser = argparse.ArgumentParser(description="Convert 8-bit PNG images in a specific directory to 32-bit PNG format.")
parser.add_argument(
    'directory',
    nargs='?',
    default=os.getcwd(),
    help="Directory to search for PNG files. Defaults to the current working directory."
)

args = parser.parse_args()
root_directory = args.directory

for dir_path, _, filenames in os.walk(root_directory):
    for filename in filenames:
        if filename.endswith(".png"):
            file_path = os.path.join(dir_path, filename)
            with Image.open(file_path) as img:
                if img.mode != 'RGBA':
                    img_32bit = img.convert('RGBA')
                    img_32bit.save(file_path)
                    print(f"Converted {file_path} to 32-bit.")
                else:
                    print(f"{file_path} is already in 32-bit, skipping.")
