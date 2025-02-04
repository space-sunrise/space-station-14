import os
import shutil
import subprocess
import sys

SSH_KEY_PATH = os.path.expanduser("~/.ssh/id_rsa_sunrise")
REPO_URL = os.getenv("SUNRISE_PRIVATE_REPO_URL")
CLONE_DIR = "SunrisePrivateTemp"
TARGET_DIRS = ["Resources", "Content.Client", "Content.Server", "Content.Shared"]

if not REPO_URL:
    print("Error: env SUNRISE_PRIVATE_REPO_URL not set.", file=sys.stderr)
    sys.exit(1)


def run_command(command):
    result = subprocess.run(command, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"❌ Error command: {' '.join(command)}", file=sys.stderr)
        print(f"📜 Result stdout: {result.stdout}", file=sys.stderr)
        print(f"⚠️ Result stderr: {result.stderr}", file=sys.stderr)
        sys.exit(result.returncode)

def setup_ssh():
    ssh_key = os.getenv("SUNRISE_SSH_KEY")
    if not ssh_key:
        print("❌ Error: env SUNRISE_SSH_KEY not set", file=sys.stderr)
        sys.exit(1)

    os.makedirs(os.path.expanduser("~/.ssh"), exist_ok=True)

    with open(SSH_KEY_PATH, "w") as f:
        f.write(ssh_key + "\n")

    os.chmod(SSH_KEY_PATH, 0o600)

    run_command(["ls", "-l", SSH_KEY_PATH])
    run_command(["cat", SSH_KEY_PATH])

    run_command(["eval $(ssh-agent -s)"])
    run_command(["ssh-add", SSH_KEY_PATH])


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
    setup_ssh()
    clone_repo()
    move_directories()
    cleanup()
    print("Private files loaded")


if __name__ == "__main__":
    main()
