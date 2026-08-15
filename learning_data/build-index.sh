#!/usr/bin/env bash
# Regenerates index.json from the packs in this directory.
#
# The index has to be written down because a directory cannot be listed over plain HTTP,
# which is the one place in this app where a registry beats a listing. Everything a
# registry can get wrong, it eventually does, so run this after adding or editing a pack
# and let the test suites tell you if you forgot.
set -euo pipefail

cd "$(dirname "$0")"

python3 - <<'PY'
import json, glob, os

packs = []

for path in sorted(glob.glob("*.json")):
    if os.path.basename(path) == "index.json":
        continue

    pack = json.load(open(path, encoding="utf-8"))
    stem = os.path.splitext(os.path.basename(path))[0]

    if pack["id"] != stem:
        raise SystemExit(f"{path}: id '{pack['id']}' does not match the file name")

    entry = {
        "id": pack["id"],
        "name": pack["name"],
        "kind": pack.get("kind", "vocabulary"),
        "wordCount": len(pack["words"]),
    }

    if pack.get("description"):
        entry["description"] = pack["description"]

    packs.append(entry)

with open("index.json", "w", encoding="utf-8") as out:
    json.dump({"version": 1, "packs": packs}, out, indent=2, ensure_ascii=False)
    out.write("\n")

print(f"index.json: {len(packs)} pack(s)")
PY
