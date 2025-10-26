#!/usr/bin/env python3
"""
Lightweight repository metrics extractor tailored for Unity C# projects.

Relies on lizard for per-function complexity and augments with additional
class-level and asset-oriented metrics.
"""
from __future__ import annotations

import argparse
import json
import math
import re
import statistics
import subprocess
import sys
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from datetime import datetime, timedelta
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Set, Tuple

import lizard

ROOT_SENTINEL = {"Library", "Logs", "obj", "ProjectSettings", "UserSettings", ".git"}

# Keywords that should not be interpreted as identifiers for method invocations.
CONTROL_KEYWORDS = {
    "if",
    "for",
    "foreach",
    "while",
    "switch",
    "case",
    "catch",
    "lock",
    "using",
    "when",
    "return",
    "sizeof",
    "typeof",
    "nameof",
    "new",
    "delegate",
    "event",
    "yield",
}


@dataclass
class MethodMetrics:
    name: str
    class_name: Optional[str]
    qualified_name: str
    file_path: str
    start_line: int
    end_line: int
    loc: int
    complexity: int
    parameter_count: int
    fan_out_calls: Set[str] = field(default_factory=set)


@dataclass
class ClassMetrics:
    name: str
    kind: str
    file_path: str
    start_line: int
    end_line: int
    namespace: Optional[str]
    bases_raw: List[str]
    methods: List[MethodMetrics] = field(default_factory=list)
    fields: Set[str] = field(default_factory=set)
    base_class: Optional[str] = None
    interfaces: List[str] = field(default_factory=list)
    dit: int = 0
    noc: int = 0
    wmc: int = 0
    rfc: int = 0
    lcom: float = 0.0
    fan_out_classes: Set[str] = field(default_factory=set)
    fan_in: int = 0
    cbo: int = 0


@dataclass
class FileMetrics:
    path: str
    total_lines: int
    blank_lines: int
    comment_lines: int
    code_lines: int
    using_count: int
    cyclomatic_total: int
    functions: List[MethodMetrics] = field(default_factory=list)
    classes: List[ClassMetrics] = field(default_factory=list)


def iter_cs_files(root: Path) -> Iterable[Path]:
    for path in root.rglob("*.cs"):
        try:
            relative = path.relative_to(root)
        except ValueError:
            continue
        if any(part in ROOT_SENTINEL for part in relative.parts):
            continue
        yield path


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def count_using_statements(lines: Sequence[str]) -> int:
    count = 0
    for line in lines:
        stripped = line.strip()
        if not stripped:
            continue
        if stripped.startswith("using "):
            count += 1
        else:
            # stop once we hit non-using / non-blank content outside namespaces
            break
    return count


def parse_namespace(prefix_text: str) -> Optional[str]:
    matches = list(re.finditer(r"\bnamespace\s+([A-Za-z0-9_.]+)", prefix_text))
    return matches[-1].group(1) if matches else None


def split_bases(bases_str: str) -> List[str]:
    if not bases_str:
        return []
    raw = bases_str.split(":")[1]
    result: List[str] = []
    token = []
    depth = 0
    for ch in raw:
        if ch in "<({[":
            depth += 1
        elif ch in ">)}]":
            depth = max(0, depth - 1)
        if ch == "," and depth == 0:
            part = "".join(token).strip()
            if part:
                result.append(part)
            token = []
            continue
        token.append(ch)
    if token:
        part = "".join(token).strip()
        if part:
            result.append(part)
    return result


def class_pattern() -> re.Pattern[str]:
    return re.compile(
        r"""
        (?P<attr>(?:\[[^\]]*\]\s*)*)
        (?P<modifiers>(?:public|private|protected|internal|static|sealed|abstract|
                        partial|new|readonly|unsafe|ref|record)\s+)*
        (?P<kind>class|struct|interface|record)
        \s+
        (?P<name>[A-Za-z_][A-Za-z0-9_<>]*)
        (?P<bases>\s*:\s*[^{]+)?
        \s*\{
        """,
        re.MULTILINE | re.VERBOSE,
    )


def clean_string_literals(text: str) -> str:
    # Replace string and char literals with spaces to avoid false positives.
    string_pattern = re.compile(
        r"""
        (?:@?"(?:[^"]|"")*")      # verbatim or normal strings
        |(?:\$@"(?:[^"]|"")*")    # interpolated verbatim
        |(?:\$"(?:\\.|[^"\\])*")  # interpolated
        |'(?:\\.|[^'\\])'         # char
        """,
        re.VERBOSE | re.DOTALL,
    )
    return string_pattern.sub(lambda m: " " * len(m.group(0)), text)


def remove_comments(text: str) -> str:
    no_block = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), text, flags=re.DOTALL)
    return re.sub(r"//.*", "", no_block)


def extract_class_blocks(file_text: str, rel_path: str) -> List[ClassMetrics]:
    matches = []
    pattern = class_pattern()
    for match in pattern.finditer(file_text):
        brace_index = match.end() - 1
        body_start = brace_index
        depth = 0
        idx = body_start
        while idx < len(file_text):
            ch = file_text[idx]
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
                if depth == 0:
                    break
            idx += 1
        if depth != 0:
            continue
        body_end = idx
        prefix = file_text[: match.start()]
        namespace = parse_namespace(prefix)
        class_body = file_text[body_start + 1 : body_end]
        start_line = prefix.count("\n") + 1
        end_line = file_text[: body_end].count("\n") + 1
        bases = split_bases(match.group("bases") or "")
        metrics = ClassMetrics(
            name=match.group("name").strip(),
            kind="record" if match.group("kind") == "record" else match.group("kind"),
            file_path=rel_path,
            start_line=start_line,
            end_line=end_line,
            namespace=namespace,
            bases_raw=bases,
        )
        matches.append((metrics, class_body))
    return [m for m, _ in matches]


def extract_fields_from_class(class_body: str) -> Set[str]:
    fields: Set[str] = set()
    depth = 0
    buffer = []
    stripped_body = clean_string_literals(remove_comments(class_body))
    for ch in stripped_body:
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth = max(0, depth - 1)
        if depth == 0:
            if ch == ";":
                statement = "".join(buffer).strip()
                buffer = []
                if not statement or "=>" in statement or "(" in statement:
                    continue
                statement = re.sub(r"\[[^\]]*\]\s*", "", statement)
                declarators = statement.split()
                if not declarators:
                    continue
                # Remove modifiers
                modifiers = {
                    "public",
                    "private",
                    "protected",
                    "internal",
                    "static",
                    "readonly",
                    "volatile",
                    "const",
                    "unsafe",
                    "new",
                    "sealed",
                    "virtual",
                    "override",
                    "abstract",
                    "extern",
                    "partial",
                }
                declarators = [token for token in declarators if token not in modifiers]
                if not declarators:
                    continue
                # Rehydrate string after removing modifiers for more precise parsing.
                left_part = re.sub(
                    r"\b(?:public|private|protected|internal|static|readonly|volatile|const|unsafe|new|sealed|virtual|override|abstract|extern|partial)\b",
                    "",
                    statement,
                ).strip()
                if not left_part:
                    continue
                segments = split_declarators(left_part)
                if len(segments) <= 1:
                    name_token = left_part.split()[-1]
                    name_token = name_token.split("=")[0].strip()
                    if name_token.endswith("[]"):
                        name_token = name_token[:-2]
                    fields.add(name_token)
                else:
                    type_prefix = segments[0].split()
                    type_tokens = type_prefix[:-1] if len(type_prefix) > 1 else []
                    decls = segments[-1].split(",")
                    for decl in decls:
                        name_token = decl.split("=")[0].strip()
                        if " " in name_token:
                            name_token = name_token.split()[-1]
                        if name_token.endswith("[]"):
                            name_token = name_token[:-2]
                        if name_token:
                            fields.add(name_token)
            else:
                buffer.append(ch)
        else:
            if depth < 0:
                depth = 0
            # ignore inner content
    return {name for name in fields if name and name[0].isalpha()}


def split_declarators(segment: str) -> List[str]:
    parts: List[str] = []
    current: List[str] = []
    depth = 0
    for ch in segment:
        if ch in "<([{":
            depth += 1
        elif ch in ">)]}":
            depth = max(0, depth - 1)
        if ch == "," and depth == 0:
            if current:
                parts.append("".join(current).strip())
                current = []
            continue
        current.append(ch)
    if current:
        parts.append("".join(current).strip())
    return parts


def build_class_lookup(classes: Iterable[ClassMetrics]) -> Dict[str, ClassMetrics]:
    lookup: Dict[str, ClassMetrics] = {}
    for cls in classes:
        lookup.setdefault(cls.name, cls)
    return lookup


def assign_class_bases(classes: List[ClassMetrics]) -> None:
    lookup = build_class_lookup(classes)
    for cls in classes:
        bases = [base.strip() for base in cls.bases_raw if base.strip()]
        if not bases:
            continue
        resolved_base = next((base for base in bases if base in lookup), None)
        if resolved_base:
            cls.base_class = resolved_base
            cls.interfaces = [base for base in bases if base != resolved_base]
        else:
            cls.interfaces = bases


def compute_inheritance_metrics(classes: List[ClassMetrics]) -> None:
    children: Dict[str, Set[str]] = defaultdict(set)
    for cls in classes:
        if cls.base_class:
            children[cls.base_class].add(cls.name)
    lookup = build_class_lookup(classes)

    def compute_dit(name: str, visited: Set[str]) -> int:
        cls = lookup.get(name)
        if not cls or not cls.base_class or cls.base_class == cls.name or cls.base_class in visited:
            return 0
        return 1 + compute_dit(cls.base_class, visited | {name})

    for cls in classes:
        cls.dit = compute_dit(cls.name, set())
        cls.noc = len(children.get(cls.name, set()))


def strip_comments_and_strings(text: str) -> str:
    return clean_string_literals(remove_comments(text))


def compute_method_field_usage(method_text: str, fields: Set[str]) -> Set[str]:
    cleaned = strip_comments_and_strings(method_text)
    usage = set()
    for field in fields:
        pattern = rf"\b{re.escape(field)}\b"
        if re.search(pattern, cleaned):
            usage.add(field)
    return usage


def compute_method_calls(method_text: str) -> Set[str]:
    cleaned = strip_comments_and_strings(method_text)
    calls = {
        match.group(1)
        for match in re.finditer(r"\b([A-Za-z_][A-Za-z0-9_]*)\s*\(", cleaned)
        if match.group(1) not in CONTROL_KEYWORDS
    }
    return calls


def compute_lcom(method_usages: List[Set[str]]) -> float:
    if len(method_usages) <= 1:
        return 0.0
    pairs = 0
    disjoint_pairs = 0
    for i in range(len(method_usages)):
        for j in range(i + 1, len(method_usages)):
            pairs += 1
            if method_usages[i].isdisjoint(method_usages[j]):
                disjoint_pairs += 1
    shared_pairs = pairs - disjoint_pairs
    return float(max(disjoint_pairs - shared_pairs, 0))


def compute_rfc(method_calls: List[Set[str]], method_count: int) -> int:
    unique_calls = set().union(*method_calls) if method_calls else set()
    return method_count + len(unique_calls)


def analyze_cs_file(path: Path, root: Path) -> FileMetrics:
    relative_path = path.relative_to(root).as_posix()
    content = read_text(path)
    lines = content.splitlines()
    total_lines = len(lines)
    blank_lines = sum(1 for line in lines if not line.strip())
    using_count = count_using_statements(lines)
    lizard_info = lizard.analyze_file(str(path))
    code_lines = lizard_info.nloc
    comment_lines = max(total_lines - blank_lines - code_lines, 0)

    file_metrics = FileMetrics(
        path=relative_path,
        total_lines=total_lines,
        blank_lines=blank_lines,
        comment_lines=comment_lines,
        code_lines=code_lines,
        using_count=using_count,
        cyclomatic_total=0,
    )

    class_blocks = extract_class_blocks(content, relative_path)
    file_metrics.classes.extend(class_blocks)

    for func in lizard_info.function_list:
        class_name = None
        qualified_name = func.name
        if "::" in qualified_name:
            parts = qualified_name.split("::", 1)
            class_name = parts[0]
        method = MethodMetrics(
            name=func.unqualified_name,
            class_name=class_name,
            qualified_name=qualified_name,
            file_path=relative_path,
            start_line=func.start_line,
            end_line=func.end_line,
            loc=func.nloc,
            complexity=func.cyclomatic_complexity,
            parameter_count=func.parameter_count,
        )
        file_metrics.functions.append(method)
        file_metrics.cyclomatic_total += func.cyclomatic_complexity

    class_lookup = {cls.name: cls for cls in class_blocks}
    for method in file_metrics.functions:
        if method.class_name and method.class_name in class_lookup:
            class_lookup[method.class_name].methods.append(method)

    for cls in class_blocks:
        class_slice = content.splitlines()[cls.start_line - 1 : cls.end_line]
        class_text = "\n".join(class_slice)
        cls.fields = extract_fields_from_class(class_text)
        method_usages: List[Set[str]] = []
        method_calls: List[Set[str]] = []
        fan_out_classes: Set[str] = set()

        for method in cls.methods:
            method_lines = content.splitlines()[method.start_line - 1 : method.end_line]
            method_text = "\n".join(method_lines)
            usage = compute_method_field_usage(method_text, cls.fields)
            calls = compute_method_calls(method_text)
            method_usages.append(usage)
            method_calls.append(calls)
            method.fan_out_calls = calls

        cls.wmc = sum(m.complexity for m in cls.methods)
        cls.rfc = compute_rfc(method_calls, len(cls.methods))
        cls.lcom = compute_lcom(method_usages)

        cleaned_class_text = strip_comments_and_strings(class_text)
        identifiers = set(re.findall(r"\b([A-Za-z_][A-Za-z0-9_]*)\b", cleaned_class_text))
        for ident in identifiers:
            if ident != cls.name:
                fan_out_classes.add(ident)
        cls.fan_out_classes = fan_out_classes

    return file_metrics


def aggregate_metrics(files: List[FileMetrics]) -> Dict[str, object]:
    all_methods = [method for f in files for method in f.functions]
    all_classes = [cls for f in files for cls in f.classes]
    assign_class_bases(all_classes)
    compute_inheritance_metrics(all_classes)

    class_lookup = {cls.name: cls for cls in all_classes}
    for cls in all_classes:
        resolved = {name for name in cls.fan_out_classes if name in class_lookup and name != cls.name}
        cls.fan_out_classes = resolved
        cls.cbo = len(resolved)
    for cls in all_classes:
        for dep in cls.fan_out_classes:
            if dep in class_lookup:
                class_lookup[dep].fan_in += 1

    total_loc = sum(f.total_lines for f in files)
    total_code = sum(f.code_lines for f in files)
    total_comments = sum(f.comment_lines for f in files)

    method_locs = [m.loc for m in all_methods]
    method_complexities = [m.complexity for m in all_methods]
    method_count = len(all_methods)

    stats = {
        "files_analyzed": len(files),
        "total_loc": total_loc,
        "total_code_loc": total_code,
        "total_comment_loc": total_comments,
        "total_blank_loc": sum(f.blank_lines for f in files),
        "comment_density": (total_comments / total_loc * 100) if total_loc else 0,
        "total_methods": method_count,
        "average_method_loc": statistics.mean(method_locs) if method_locs else 0,
        "median_method_loc": statistics.median(method_locs) if method_locs else 0,
        "average_method_complexity": statistics.mean(method_complexities) if method_complexities else 0,
        "median_method_complexity": statistics.median(method_complexities) if method_complexities else 0,
    }
    return {
        "files": [f.__dict__ for f in files],
        "methods": [m.__dict__ for m in all_methods],
        "classes": [
            {
                **cls.__dict__,
                "fan_out_classes": sorted(cls.fan_out_classes),
                "fields": sorted(cls.fields),
            }
            for cls in all_classes
        ],
        "stats": stats,
    }


def detect_duplicate_lines(cs_files: Iterable[Path]) -> Dict[str, float]:
    counter: Counter[str] = Counter()
    total = 0
    for path in cs_files:
        content = read_text(path)
        stripped = strip_comments_and_strings(content)
        for line in stripped.splitlines():
            normalized = line.strip()
            if len(normalized) < 5:
                continue
            total += 1
            counter[normalized] += 1
    duplicates = sum(count for count in counter.values() if count > 1)
    percentage = (duplicates / total * 100) if total else 0.0
    return {"duplicate_lines": duplicates, "total_considered": total, "percentage": percentage}


def collect_asset_inventory(root: Path) -> Dict[str, int]:
    assets_dir = root / "Assets"
    if not assets_dir.exists():
        return {}
    extensions = {
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
    counts = {key: 0 for key in extensions}
    counts["scriptable_objects"] = 0
    counts["sprites"] = 0
    counts["asmdef"] = 0

    meta_cache: Dict[Path, Optional[str]] = {}

    for path in assets_dir.rglob("*"):
        if path.is_dir():
            continue
        suffix = path.suffix.lower()
        for key, exts in extensions.items():
            if suffix in exts:
                counts[key] += 1
        if suffix == ".asset":
            text = read_text(path)
            if "m_Script:" in text or "MonoBehaviour:" in text or "ScriptableObject:" in text:
                counts["scriptable_objects"] += 1
        if suffix == ".asmdef":
            counts["asmdef"] += 1
        if suffix in extensions["textures"]:
            meta_path = path.with_suffix(path.suffix + ".meta")
            if meta_path not in meta_cache:
                meta_cache[meta_path] = read_text(meta_path) if meta_path.exists() else None
            meta_text = meta_cache[meta_path]
            if meta_text and ("spriteMode:" in meta_text or "textureType: Sprite" in meta_text):
                counts["sprites"] += 1

    return counts


def collect_git_metrics(root: Path) -> Dict[str, object]:
    try:
        proc = subprocess.run(
            ["git", "log", "--pretty=%H|%ad", "--date=short", "--numstat"],
            cwd=root,
            capture_output=True,
            text=True,
            check=True,
        )
    except subprocess.CalledProcessError:
        return {}

    commits: List[Dict[str, object]] = []
    current: Optional[Dict[str, object]] = None

    for line in proc.stdout.splitlines():
        if not line:
            continue
        if "|" in line and "\t" not in line:
            commit_hash, date_str = line.split("|", 1)
            current = {
                "hash": commit_hash,
                "date": date_str,
                "additions": 0,
                "deletions": 0,
            }
            commits.append(current)
        elif current and "\t" in line:
            parts = line.split("\t")
            if len(parts) < 3:
                continue
            added, deleted = parts[0], parts[1]
            if added.isdigit():
                current["additions"] += int(added)
            if deleted.isdigit():
                current["deletions"] += int(deleted)

    if not commits:
        return {}

    total_additions = sum(c["additions"] for c in commits)
    total_deletions = sum(c["deletions"] for c in commits)
    total_churn = total_additions + total_deletions

    dates = [datetime.strptime(c["date"], "%Y-%m-%d").date() for c in commits]
    span_days = (max(dates) - min(dates)).days or 1
    commits_per_month = len(commits) / max(span_days / 30.0, 1)

    cutoff = datetime.utcnow().date() - timedelta(days=90)
    recent = [c for c, d in zip(commits, dates) if d >= cutoff]
    recent_churn = sum(c["additions"] + c["deletions"] for c in recent)

    return {
        "total_commits": len(commits),
        "total_churn": total_churn,
        "total_additions": total_additions,
        "total_deletions": total_deletions,
        "commits_per_month": commits_per_month,
        "recent_90d_commit_count": len(recent),
        "recent_90d_churn": recent_churn,
        "first_commit_date": min(dates).isoformat(),
        "last_commit_date": max(dates).isoformat(),
    }


def collect_test_metrics(cs_files: Iterable[Path]) -> Dict[str, int]:
    test_files = 0
    test_methods = 0
    for path in cs_files:
        rel = "/".join(path.parts)
        if "Test" in path.name or "Tests" in rel:
            text = read_text(path)
            occurrences = len(re.findall(r"\[Test\]", text))
            if occurrences:
                test_files += 1
                test_methods += occurrences
    return {"test_files_with_attributes": test_files, "test_method_count": test_methods}


def calculate_metrics(root: Path) -> Dict[str, object]:
    cs_files = list(iter_cs_files(root))
    file_metrics = [analyze_cs_file(path, root) for path in cs_files]
    summary = aggregate_metrics(file_metrics)
    duplicate_info = detect_duplicate_lines(cs_files)
    asset_inventory = collect_asset_inventory(root)
    git_metrics = collect_git_metrics(root)
    test_metrics = collect_test_metrics(cs_files)

    summary.update(
        {
            "duplicates": duplicate_info,
            "assets": asset_inventory,
            "git": git_metrics,
            "tests": test_metrics,
            "cs_file_count": len(cs_files),
        }
    )
    return summary


def main() -> None:
    parser = argparse.ArgumentParser(description="Repository metrics collector")
    parser.add_argument("--root", default=".", help="Repository root directory")
    parser.add_argument("--output", help="Optional JSON output file path")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    if not root.exists():
        raise SystemExit(f"Root path not found: {root}")

    metrics = calculate_metrics(root)
    output = json.dumps(metrics, indent=2)
    if args.output:
        Path(args.output).write_text(output, encoding="utf-8")
    else:
        print(output)


if __name__ == "__main__":
    main()
