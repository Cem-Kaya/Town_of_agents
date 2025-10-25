#!/usr/bin/env python3
"""Calculate LOC metrics for C# files within a Unity project."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Iterable

import lizard

ROOT_SENTINEL = {"Library", "Logs", "obj", "ProjectSettings", "UserSettings", ".git"}


def iter_cs_files(root: Path) -> Iterable[Path]:
    for path in root.rglob("*.cs"):
        try:
            relative = path.relative_to(root)
        except ValueError:
            continue
        if any(part in ROOT_SENTINEL for part in relative.parts):
            continue
        yield path


def analyze_file(path: Path, root: Path) -> dict:
    relative = path.relative_to(root).as_posix()
    text = path.read_text(encoding="utf-8", errors="ignore")
    lines = text.splitlines()
    total_lines = len(lines)
    blank_lines = sum(1 for line in lines if not line.strip())
    nloc = lizard.analyze_file(str(path)).nloc
    comment_lines = max(total_lines - blank_lines - nloc, 0)
    non_blank = total_lines - blank_lines
    return {
        "file": relative,
        "total_lines": total_lines,
        "non_blank_lines": non_blank,
        "comment_lines": comment_lines,
        "code_lines": nloc,
    }


def collect_metrics(root: Path) -> dict:
    root = root.resolve()
    files = []
    totals = {
        "total_lines": 0,
        "non_blank_lines": 0,
        "comment_lines": 0,
        "code_lines": 0,
    }
    for path in iter_cs_files(root):
        metrics = analyze_file(path, root)
        files.append(metrics)
        for key in totals:
            totals[key] += metrics[key]
    files.sort(key=lambda item: item["file"])
    return {"totals": totals, "files": files}


def render_markdown(metrics: dict) -> str:
    lines = [
        "| File | Total Lines | Non-blank Lines | Code Lines | Comment Lines |",
        "| --- | --- | --- | --- | --- |",
    ]
    for row in metrics["files"]:
        lines.append(
            f"| {row['file']} | {row['total_lines']} | {row['non_blank_lines']} | {row['code_lines']} | {row['comment_lines']} |"
        )
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Compute LOC metrics for C# files.")
    parser.add_argument("--root", default=".", help="Root directory (default: current folder).")
    parser.add_argument("--output", help="Optional path to save JSON metrics.")
    parser.add_argument("--markdown", action="store_true", help="Render a markdown table instead of JSON.")
    args = parser.parse_args()

    root = Path(args.root)
    metrics = collect_metrics(root)

    if args.output:
        Path(args.output).write_text(json.dumps(metrics, indent=2), encoding="utf-8")

    if args.markdown:
        print(render_markdown(metrics))
    elif not args.output:
        print(json.dumps(metrics, indent=2))


if __name__ == "__main__":
    main()
