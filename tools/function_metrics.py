#!/usr/bin/env python3
"""Gather per-function metrics (LOC, cyclomatic complexity) using lizard."""
from __future__ import annotations

import argparse
import json
import statistics
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Dict, Iterable, List

import lizard

ROOT_SENTINEL = {"Library", "Logs", "obj", "ProjectSettings", "UserSettings", ".git"}


@dataclass
class FunctionInfo:
    name: str
    file: str
    start_line: int
    end_line: int
    nloc: int
    cyclomatic_complexity: int
    parameters: int


def iter_cs_files(root: Path) -> Iterable[Path]:
    for path in root.rglob("*.cs"):
        try:
            relative = path.relative_to(root)
        except ValueError:
            continue
        if any(part in ROOT_SENTINEL for part in relative.parts):
            continue
        yield path


def analyze_functions(root: Path) -> Dict[str, object]:
    functions: List[FunctionInfo] = []
    for path in iter_cs_files(root):
        analysis = lizard.analyze_file(str(path))
        relative = path.relative_to(root).as_posix()
        for func in analysis.function_list:
            functions.append(
                FunctionInfo(
                    name=func.long_name,
                    file=relative,
                    start_line=func.start_line,
                    end_line=func.end_line,
                    nloc=func.nloc,
                    cyclomatic_complexity=func.cyclomatic_complexity,
                    parameters=func.parameter_count,
                )
            )
    if not functions:
        return {"summary": {}, "functions": []}

    nloc_values = [fn.nloc for fn in functions]
    ccn_values = [fn.cyclomatic_complexity for fn in functions]

    summary = {
        "function_count": len(functions),
        "average_loc": statistics.mean(nloc_values),
        "median_loc": statistics.median(nloc_values),
        "average_ccn": statistics.mean(ccn_values),
        "median_ccn": statistics.median(ccn_values),
        "max_loc": max(nloc_values),
        "max_ccn": max(ccn_values),
    }

    max_loc_fn = max(functions, key=lambda fn: fn.nloc)
    max_ccn_fn = max(functions, key=lambda fn: fn.cyclomatic_complexity)
    summary["max_loc_function"] = asdict(max_loc_fn)
    summary["max_ccn_function"] = asdict(max_ccn_fn)

    return {
        "summary": summary,
        "functions": [asdict(fn) for fn in functions],
    }


def render_markdown(data: Dict[str, object], limit: int | None = None) -> str:
    lines = [
        "| Function | File | LOC | CCN | Parameters |",
        "| --- | --- | --- | --- | --- |",
    ]
    functions = data["functions"]
    if limit is not None:
        functions = sorted(functions, key=lambda item: item["cyclomatic_complexity"], reverse=True)[:limit]
    for fn in functions:
        lines.append(
            f"| {fn['name']} | {fn['file']}:{fn['start_line']} | {fn['nloc']} | {fn['cyclomatic_complexity']} | {fn['parameters']} |"
        )
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Compute per-function LOC and cyclomatic complexity.")
    parser.add_argument("--root", default=".", help="Root directory (default: current).")
    parser.add_argument("--output", help="Optional JSON output.")
    parser.add_argument("--markdown", action="store_true", help="Render markdown table.")
    parser.add_argument("--top", type=int, help="Limit markdown output to top N functions by CCN.")
    args = parser.parse_args()

    root = Path(args.root)
    data = analyze_functions(root)

    if args.output:
        Path(args.output).write_text(json.dumps(data, indent=2), encoding="utf-8")

    if args.markdown:
        print(render_markdown(data, limit=args.top))
    elif not args.output:
        print(json.dumps(data, indent=2))


if __name__ == "__main__":
    main()
