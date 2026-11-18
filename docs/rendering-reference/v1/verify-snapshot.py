#!/usr/bin/env python3
"""Verify frozen v1 RASC renderer-source/fixture Git blob identities offline.

Run from ANY working directory:
    python docs/rendering-reference/v1/verify-snapshot.py
No dependencies beyond Python 3. No Steam, game, network, GUI or repo writes.
Source-changing future commits must version this archive rather than silently
rebasing accepted reference assumptions. Git blob SHA-1 is not file SHA-256.
"""
import hashlib
import json
from pathlib import Path
import sys

THIS_DIR = Path(__file__).resolve().parent
ROOT = THIS_DIR.parents[2]
MANIFEST = THIS_DIR / "manifest.json"


def git_blob_id(data: bytes) -> str:
    header = b"blob " + str(len(data)).encode("ascii") + bytes([0])
    return hashlib.sha1(header + data).hexdigest()


def main() -> int:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if manifest.get("schema") != "rasc.renderer-reference-lock.v1":
        print("FAIL manifest schema mismatch", file=sys.stderr)
        return 1
    checked = 0
    errors = 0
    seen = set()
    for item in manifest["locked_paths"]:
        relative = item["path"]
        if relative in seen:
            print(f"FAIL duplicate manifest entry: {relative}")
            errors += 1
            continue
        seen.add(relative)
        path = (ROOT / relative).resolve()
        if not path.is_relative_to(ROOT):
            print(f"FAIL unsafe path outside repository: {relative}")
            errors += 1
            continue
        if not path.is_file():
            print(f"FAIL missing path: {relative}")
            errors += 1
            continue
        observed = git_blob_id(path.read_bytes())
        checked += 1
        if observed != item["git_blob_sha"]:
            print(f"FAIL {relative}: expected {item['git_blob_sha']}, found {observed}")
            errors += 1
    print(f"RASC renderer v1 reference locks: {checked} checked, {errors} mismatched.")
    if not errors:
        print("PASS: source and embedded reference files match frozen baseline.")
        print("Note: this DOES NOT validate visual pixel parity, license or external screenshot custody.")
    return int(errors != 0)


if __name__ == "__main__":
    raise SystemExit(main())
