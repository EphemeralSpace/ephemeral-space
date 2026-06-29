import subprocess
import sys
import re
from pathlib import Path

def get_referenced_rsis(project_root: Path) -> set[str]:
    # yeah you need rg sorry i was doing it initially with this
    # and then realized i probably wanted to do it progrmamatically
    # and didnt wanna figure out how to do this with python
    result = subprocess.run(
        [
            "rg",
            "--no-heading",
            "--no-filename",
            "--only-matching",
            r"[\w./\-]+\.rsi",
            "--glob", "*.cs",
            "--glob", "*.yml",
            str(project_root),
        ],
        capture_output=True,
        text=True,
    )

    if result.returncode != 0:
        sys.exit()

    raw_matches = result.stdout.splitlines()
    refs: set[str] = set()
    for match in raw_matches:
        match = match.strip()
        if not match:
            continue

        normalized = match.lstrip("/").lower()
        refs.add(normalized)

    return refs

def get_rsis(project_root: Path) -> list[Path]:
    if not project_root.exists():
        return []

    textures_dir = project_root / "Resources/Textures"
    if not textures_dir.exists():
        print("run it in project root, where Resources dir is")
        sys.exit()
        return []
    rsi_paths = []

    for p in textures_dir.rglob("*.rsi"):
        rsi_paths.append(p)

    return rsi_paths

def is_path_referenced(rsi_path: Path, project_root: Path, refs: set[str]) -> bool:
    try:
        rel = rsi_path.relative_to(project_root)
    except ValueError:
        rel = rsi_path

    rel_str = str(rel).lower()

    candidates = set()
    candidates.add(rel_str)
    candidates.add(rsi_path.name.lower())

    parts = rel_str.split("/")
    for i in range(len(parts)):
        candidates.add("/".join(parts[i:]))

    for ref in refs:
        for candidate in candidates:
            if candidate and candidate in ref:
                return True
            # Also check the other way: ref is a suffix of the candidate
            if ref and ref in candidate:
                return True

    return False


def main():
    project_root = Path.cwd()
    project_root = project_root.resolve()

    refs = get_referenced_rsis(project_root)
    rsis = get_rsis(project_root)
    print(f"{len(rsis)} .rsis")

    unused = []
    used = []

    for rsi_path in sorted(rsis):
        if is_path_referenced(rsi_path, project_root, refs):
            used.append(rsi_path)
        else:
            unused.append(rsi_path)

    print(f"{len(used)} used, {len(unused)} unused")

    if unused:
        # write to file
        out_file = project_root / "unused_rsis.txt"
        with open(out_file, 'w') as f:
            for p in sorted(unused):
                try:
                    rel = p.relative_to(project_root)
                except ValueError:
                    rel = p
                f.write(str(rel) + "\n")

        print(f"\nlist written to: {out_file}")
        print(f"you can use xargs or something to automatically delete shit but probably dont do that if you arent confident in it")

if __name__ == "__main__":
    main()
