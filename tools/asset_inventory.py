#!/usr/bin/env python3
"""Summarize Unity asset counts (scripts, prefabs, sprites, etc.)."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Dict, Iterable, List


ASSET_EXTENSIONS = {
    "scripts": [".cs"],
    "prefabs": [".prefab"],
    "scenes": [".unity"],
    "materials": [".mat"],
    "shaders": [".shader"],
    "animations": [".anim"],
    "controllers": [".controller"],
    "models": [".fbx", ".obj", ".dae", ".blend"],
    "audio": [".wav", ".mp3", ".ogg"],
    "textures": [".png", ".jpg", ".jpeg", ".tga", ".bmp", ".psd", ".tif", ".tiff"],
}


def enumerate_assets(assets_dir: Path) -> Dict[str, int]:
    counts = {key: 0 for key in ASSET_EXTENSIONS}
    counts["scriptable_objects"] = 0
    counts["sprites"] = 0
    counts["asmdef"] = 0

    meta_cache: Dict[Path, str] = {}

    for path in assets_dir.rglob("*"):
        if path.is_dir():
            continue
        suffix = path.suffix.lower()
        for key, extensions in ASSET_EXTENSIONS.items():
            if suffix in extensions:
                counts[key] += 1
        if suffix == ".asset":
            try:
                text = path.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            if "ScriptableObject:" in text or "MonoBehaviour:" in text or "m_Script:" in text:
                counts["scriptable_objects"] += 1
        if suffix == ".asmdef":
            counts["asmdef"] += 1
        if suffix in ASSET_EXTENSIONS["textures"]:
            meta_path = Path(str(path) + ".meta")
            if meta_path.exists():
                if meta_path not in meta_cache:
                    meta_cache[meta_path] = meta_path.read_text(encoding="utf-8", errors="ignore")
                meta_text = meta_cache[meta_path]
                if "spriteMode:" in meta_text or "textureType: Sprite" in meta_text:
                    counts["sprites"] += 1
    return counts


def render_markdown(counts: Dict[str, int]) -> str:
    lines = [
        "| Asset Type | Count |",
        "| --- | --- |",
    ]
    for key in sorted(counts):
        lines.append(f"| {key} | {counts[key]} |")
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Count Unity asset types under Assets directory.")
    parser.add_argument("--assets", default="Assets", help="Assets directory path (default: Assets).")
    parser.add_argument("--output", help="Optional JSON output file.")
    parser.add_argument("--markdown", action="store_true", help="Render as markdown table.")
    args = parser.parse_args()

    assets_dir = Path(args.assets)
    if not assets_dir.is_dir():
        raise SystemExit(f"Assets directory not found: {assets_dir}")

    counts = enumerate_assets(assets_dir)

    if args.output:
        Path(args.output).write_text(json.dumps(counts, indent=2), encoding="utf-8")
    if args.markdown:
        print(render_markdown(counts))
    elif not args.output:
        print(json.dumps(counts, indent=2))


if __name__ == "__main__":
    main()
