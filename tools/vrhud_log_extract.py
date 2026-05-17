#!/usr/bin/env python3
"""Check and optionally extract focused VRHud diagnostics from BepInEx logs.

Default behavior:
  - selects the newest *.log file in ./test_log
  - compares file mtime/size/sha256 with the previous run
  - does not read log contents unless --extract is set
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import Counter, deque
from datetime import datetime
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG_DIR = REPO_ROOT / "test_log"
DEFAULT_STATE = DEFAULT_LOG_DIR / ".vrhud_log_state.json"
DEFAULT_PATTERNS = [
    r"\[VRHud/(Graphic|CurveNode|TMP|TMPRestore|PopupRoot|HandReticle|ScannerIcon|CameraLevel)\]",
    r"\b(PowerIndicator|HUDPowerStatus|전력|PopupNotification|Unlock|Journal|CallAlAn|ErrorMessageCanvas|MessageInstance|mode=|segments=|afterZ=|span=|skipCurve|popupLocal|popupArc|popupRelative|relative|vertex)\b",
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Check whether the latest test log changed and extract VRHud diagnostic lines."
    )
    parser.add_argument(
        "--log-dir",
        type=Path,
        default=DEFAULT_LOG_DIR,
        help="Directory containing copied test logs. Default: ./test_log",
    )
    parser.add_argument(
        "--file",
        type=Path,
        default=None,
        help="Specific log file to read. Default: newest *.log in --log-dir",
    )
    parser.add_argument(
        "--state",
        type=Path,
        default=DEFAULT_STATE,
        help="State file used for update checks. Default: ./test_log/.vrhud_log_state.json",
    )
    parser.add_argument(
        "--pattern",
        action="append",
        default=None,
        help="Regex pattern to extract. Can be passed multiple times.",
    )
    parser.add_argument(
        "--last",
        type=int,
        default=200,
        help="Maximum matching lines to print from the end of the log when --extract is set. Default: 200",
    )
    parser.add_argument(
        "--extract",
        action="store_true",
        help="Read log contents and print matching diagnostic lines.",
    )
    parser.add_argument(
        "--require-new",
        action="store_true",
        help="Exit with code 2 when the selected log matches the previous state. With --extract, unchanged logs are not read.",
    )
    parser.add_argument(
        "--no-write-state",
        action="store_true",
        help="Do not update the state file after reading.",
    )
    parser.add_argument(
        "--reset-state",
        action="store_true",
        help="Ignore and overwrite existing state.",
    )
    return parser.parse_args()


def newest_log(log_dir: Path) -> Path:
    if not log_dir.exists():
        raise FileNotFoundError(f"log directory does not exist: {log_dir}")
    logs = [p for p in log_dir.glob("*.log") if p.is_file()]
    if not logs:
        raise FileNotFoundError(f"no *.log files found in: {log_dir}")
    return max(logs, key=lambda p: (p.stat().st_mtime_ns, p.name))


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def file_state(path: Path) -> dict[str, object]:
    stat = path.stat()
    return {
        "path": str(path.resolve()),
        "size": stat.st_size,
        "mtime_ns": stat.st_mtime_ns,
        "mtime": datetime.fromtimestamp(stat.st_mtime).isoformat(timespec="seconds"),
        "sha256": sha256_file(path),
    }


def load_state(path: Path, reset: bool) -> dict[str, object] | None:
    if reset or not path.exists():
        return None
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None


def write_state(path: Path, state: dict[str, object]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")


def state_changed(previous: dict[str, object] | None, current: dict[str, object]) -> tuple[bool, str]:
    if previous is None:
        return True, "no previous state"
    if previous.get("path") != current.get("path"):
        return True, "different file"
    changed_fields = [
        key for key in ("mtime_ns", "size", "sha256") if previous.get(key) != current.get(key)
    ]
    if changed_fields:
        return True, "changed " + ",".join(changed_fields)
    return False, "unchanged"


def compile_patterns(patterns: list[str]) -> list[re.Pattern[str]]:
    return [re.compile(pattern) for pattern in patterns]


def extract_lines(path: Path, patterns: list[re.Pattern[str]], limit: int) -> tuple[list[tuple[int, str]], Counter, int]:
    matches: deque[tuple[int, str]] = deque(maxlen=max(1, limit))
    counts: Counter = Counter()
    total_matches = 0
    with path.open("r", encoding="utf-8-sig", errors="replace") as f:
        for line_number, line in enumerate(f, start=1):
            if any(pattern.search(line) for pattern in patterns):
                total_matches += 1
                line = line.rstrip("\r\n")
                matches.append((line_number, line))
                if "[VRHud/TMP]" in line:
                    counts["TMP"] += 1
                elif "[VRHud/TMPRestore]" in line:
                    counts["TMPRestore"] += 1
                elif "[VRHud/Graphic]" in line:
                    counts["Graphic"] += 1
                elif "[VRHud/CurveNode]" in line:
                    counts["CurveNode"] += 1
                elif "[VRHud/HandReticle]" in line:
                    counts["HandReticle"] += 1
                elif "[VRHud/ScannerIcon]" in line:
                    counts["ScannerIcon"] += 1
                elif "[VRHud/CameraLevel]" in line:
                    counts["CameraLevel"] += 1
                elif "[VRHud/PopupRoot]" in line:
                    counts["PopupRoot"] += 1
                elif "[VRHud/GUIHand]" in line:
                    counts["GUIHand"] += 1
                else:
                    counts["Other"] += 1

                mode_match = re.search(r"\bmode=([A-Za-z0-9_-]+)", line)
                if mode_match:
                    counts[f"mode:{mode_match.group(1)}"] += 1
                reason_match = re.search(r"\breason=([A-Za-z0-9_-]+)", line)
                if reason_match:
                    counts[f"reason:{reason_match.group(1)}"] += 1
    return list(matches), counts, total_matches


def print_header(current: dict[str, object], changed: bool, reason: str, previous: dict[str, object] | None) -> None:
    print("== VRHud Log Check ==")
    print(f"file: {current['path']}")
    print(f"mtime: {current['mtime']}")
    print(f"size: {current['size']}")
    print(f"sha256: {str(current['sha256'])[:16]}...")
    print(f"updated: {'yes' if changed else 'no'} ({reason})")
    if previous is not None:
        print(f"previous_mtime: {previous.get('mtime', '<unknown>')}")
        prev_hash = str(previous.get("sha256", ""))
        print(f"previous_sha256: {prev_hash[:16]}..." if prev_hash else "previous_sha256: <unknown>")
    print()


def print_summary(counts: Counter, total_matches: int, printed_matches: int, limit: int) -> None:
    print("== Match Summary ==")
    print(f"total_matches: {total_matches}")
    print(f"printed_matches: {printed_matches} (last {limit})")
    for key, value in sorted(counts.items()):
        print(f"{key}: {value}")
    print()


def safe_print(text: str) -> None:
    try:
        print(text)
    except UnicodeEncodeError:
        encoded = text.encode(sys.stdout.encoding or "utf-8", errors="backslashreplace")
        print(encoded.decode(sys.stdout.encoding or "utf-8", errors="replace"))


def main() -> int:
    args = parse_args()
    log_file = args.file.resolve() if args.file else newest_log(args.log_dir).resolve()

    previous = load_state(args.state, args.reset_state)
    current = file_state(log_file)
    changed, reason = state_changed(previous, current)

    print_header(current, changed, reason, previous)
    if args.require_new and not changed:
        print("Log file is unchanged; stopping because --require-new was set.", file=sys.stderr)
        return 2

    if args.extract:
        patterns = compile_patterns(args.pattern or DEFAULT_PATTERNS)
        matches, counts, total_matches = extract_lines(log_file, patterns, args.last)
        print_summary(counts, total_matches, len(matches), args.last)

        print("== Matching Lines ==")
        for line_number, line in matches:
            safe_print(f"{line_number}: {line}")
    else:
        print("Content extraction skipped. Pass --extract to read matching log lines.")

    if not args.no_write_state:
        write_state(args.state, current)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
