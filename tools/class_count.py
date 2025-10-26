#!/usr/bin/env python3
"""Count class/struct/interface definitions in Unity C# sources."""
from __future__ import annotations

import argparse
import json
import re
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List

ROOT_SENTINEL = {"Library", "Logs", "obj", "ProjectSettings", "UserSettings", ".git"}

CLASS_PATTERN = re.compile(
    r"""
    (?P<attributes>(?:\[[^\]]*\]\s*)*)
    (?P<modifiers>(?:public|private|protected|internal|static|sealed|abstract|
                    partial|new|readonly|unsafe|ref|record)\s+)*
    (?P<kind>class|struct|interface|record)
    \s+
    (?P<name>[A-Za-z_][A-Za-z0-9_<>]*)
    """,
    re.VERBOSE,
)


def iter_cs_files(root: Path) -> Iterable[Path]:
    for path in root.rglob("*.cs"):
        try:
            relative = path.relative_to(root)
        except ValueError:
            continue
        if any(part in ROOT_SENTINEL for part in relative.parts):
            continue
        yield path


def collect(root: Path) -> Dict[str, object]:
    result: Dict[str, List[str]] = defaultdict(list)
    totals = {"class": 0, "struct": 0, "interface": 0, "record": 0}
    for path in iter_cs_files(root):
        text = path.read_text(encoding="utf-8", errors="ignore")
        cleaned = strip_comments(text)
        matches = CLASS_PATTERN.finditer(cleaned)
        for match in matches:
            kind = match.group("kind")
            totals[kind] += 1
            result[path.as_posix()].append(match.group("name"))
    totals["types_total"] = sum(totals.values())
    return {"totals": totals, "files": result}


def strip_comments(text: str) -> str:
    def replace_block(match: re.Match[str]) -> str:
        return "\n" * match.group(0).count("\n")

    without_block = re.sub(r"/\*.*?\*/", replace_block, text, flags=re.DOTALL)
    return re.sub(r"//.*", "", without_block)


def render_markdown(data: Dict[str, object]) -> str:
    lines = [
        "| File | Declarations |",
        "| --- | --- |",
    ]
    for file_path, names in sorted(data["files"].items()):
        joined = ", ".join(names)
        lines.append(f"| {file_path} | {joined} |")
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Count class/struct/interface declarations.")
    parser.add_argument("--root", default=".", help="Root directory to scan (default: current).")
    parser.add_argument("--output", help="Optional JSON output file.")
    parser.add_argument("--markdown", action="store_true", help="Render a markdown table.")
    args = parser.parse_args()

    root = Path(args.root)
    data = collect(root)

    if args.output:
        Path(args.output).write_text(json.dumps(data, indent=2), encoding="utf-8")

    if args.markdown:
        print(render_markdown(data))
    elif not args.output:
        print(json.dumps(data, indent=2))


if __name__ == "__main__":
    main()
