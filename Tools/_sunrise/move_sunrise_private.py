import os
import shutil

CLONE_DIR = "SunrisePrivate"
TARGET_DIRS = ["Resources", ".github", "Content.Client", "Content.Server", "Content.Shared", "Content.Packaging"]

def merge_directories(src_dir, dst_dir):
    for item in os.listdir(src_dir):
        src_item = os.path.join(src_dir, item)
        dst_item = os.path.join(dst_dir, item)

        if os.path.isdir(src_item):
            if not os.path.exists(dst_item):
                os.makedirs(dst_item)
            merge_directories(src_item, dst_item)
        else:
            if os.path.exists(dst_item):
                os.remove(dst_item)
            shutil.copy2(src_item, dst_item)

def move_directories():
    for directory in TARGET_DIRS:
        src = os.path.join(CLONE_DIR, directory)
        dst = directory
        if os.path.exists(src):
            print(f"Transfer {directory} ...")
            if os.path.exists(dst):
                merge_directories(src, dst)
            else:
                shutil.move(src, dst)

def main():
    move_directories()
    print("Private files move")

if __name__ == "__main__":
    main()
