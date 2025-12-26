import os
import shutil
import argparse

def parse_args():
    parser = argparse.ArgumentParser(description='Move files from SunrisePrivate repository')
    parser.add_argument('--clone-dir', required=True,
                      help='Directory containing the cloned repository')
    parser.add_argument('--target-dirs', nargs='+', required=True,
                      help='List of target directories to move')
    return parser.parse_args()

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

def move_directories(clone_dir, target_dirs):
    for directory in target_dirs:
        src = os.path.join(clone_dir, directory)
        dst = directory
        if os.path.exists(src):
            print(f"Transfer {directory} ...")
            if os.path.exists(dst):
                merge_directories(src, dst)
            else:
                shutil.move(src, dst)

def main():
    args = parse_args()
    move_directories(args.clone_dir, args.target_dirs)
    print("Private files move")

if __name__ == "__main__":
    main()
