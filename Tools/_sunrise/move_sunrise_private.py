import os
import shutil

CLONE_DIR = "SunrisePrivate"
TARGET_DIRS = ["Resources", "Content.Client", "Content.Server", "Content.Shared"]

def move_directories():
    for directory in TARGET_DIRS:
        src = os.path.join(CLONE_DIR, directory)
        dst = directory
        if os.path.exists(src):
            print(f"Transfer {directory} ...")
            if os.path.exists(dst):
                shutil.rmtree(dst)
            shutil.move(src, dst)


def cleanup():
    print(f"Delete dir {CLONE_DIR}...")
    shutil.rmtree(CLONE_DIR, ignore_errors=True)


def main():
    move_directories()
    cleanup()
    print("Private files move")


if __name__ == "__main__":
    main()
