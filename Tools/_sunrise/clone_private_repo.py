import os
import shutil
import subprocess
import sys

SSH_KEY_PATH = os.path.expanduser("~/.ssh/id_rsa_sunrise")
REPO_URL = os.getenv("SUNRISE_PRIVATE_REPO_URL")
CLONE_DIR = "SunrisePrivateTemp"
TARGET_DIRS = ["Resources", "Content.Client", "Content.Server", "Content.Shared"]

if not REPO_URL:
    print("Error: enc SUNRISE_PRIVATE_REPO_URL not set.", file=sys.stderr)
    sys.exit(1)


def run_command(command, check=True, shell=False):
    try:
        result = subprocess.run(command, shell=shell, check=check, capture_output=True, text=True)
        if result.stdout:
            print(result.stdout)
        if result.stderr:
            print(result.stderr, file=sys.stderr)
    except subprocess.CalledProcessError as e:
        print("Exception on process, rc=", e.returncode, "output=", e.output)

def clone_repo():
    if os.path.exists(CLONE_DIR):
        shutil.rmtree(CLONE_DIR)
    print(f"Clone {REPO_URL} in {CLONE_DIR}...")
    run_command(["git", "clone", "--depth=1", REPO_URL, CLONE_DIR])


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
    clone_repo()
    move_directories()
    cleanup()
    print("Private files loaded")


if __name__ == "__main__":
    main()
