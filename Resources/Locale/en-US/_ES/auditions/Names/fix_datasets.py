# run this script to fix all ftl files in this dir into the correct loc format
# works if theyre just loose names or if they have the assignment but just the wrong number or whatever
# wont alphabetize or any shit like that
# doesnt run if u have uncommitted changes
# also doesnt bulldoze comments

import os
import sys
import subprocess
import re
from pathlib import Path

def extract_prefix(line):
    # comment at top of file
    match = re.match(r'#\s*(.+)', line.strip())
    if match:
        return match.group(1)
    return None

def process_name_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    if not lines:
        return

    # extract prefix from first line
    prefix = extract_prefix(lines[0])
    if not prefix:
        return

    print(f"processing {filepath}: {prefix}")

    new_lines = [lines[0]]
    counter = 1

    for line in lines[1:]:
        stripped = line.strip()
        if not stripped or stripped[0] == '#':
            new_lines.append(line)
            continue

        # check if already has assignment
        if '=' in stripped:
            parts = stripped.split('=', 1)
            value = parts[1].strip()
            new_line = f"{prefix}-{counter} = {value}\n"
        else:
            new_line = f"{prefix}-{counter} = {stripped}\n"

        new_lines.append(new_line)
        counter += 1

    with open(filepath, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)

def main():
    result = subprocess.run(['git', 'status'],capture_output=True,text=True,check=True)
    if ".ftl" in result.stdout:
        print("git status is not clean, commit or stash changes first")
        return

    current_dir = Path('.')
    name_files = list(current_dir.glob('*.ftl'))

    if not name_files:
        return

    for name_file in name_files:
        process_name_file(name_file)

if __name__ == '__main__':
    main()
