"""Validate Chinese resource coverage and composite-format arguments (no dependencies)."""

from collections import Counter
from pathlib import Path
import re
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1] / "Blish HUD" / "Strings"
ARGUMENT = re.compile(r"(?<!\{)\{(\d+)(?:,[^}:]+)?(?::[^}]+)?\}(?!\})")


def read_resources(path):
    entries = ET.parse(path).findall("data")
    keys = [entry.attrib["name"] for entry in entries]
    assert len(keys) == len(set(keys)), f"Duplicate resource keys: {path}"
    return {entry.attrib["name"]: entry for entry in entries}


def check_resources(root=ROOT):
    files = strings = 0
    for neutral in sorted(root.rglob("*.resx")):
        if "." in neutral.stem:
            continue
        localized = neutral.with_name(neutral.stem + ".zh-CN.resx")
        original = read_resources(neutral)
        translated = read_resources(localized)
        # External license text is intentionally inherited from the neutral resource.
        expected = {key for key, entry in original.items() if "type" not in entry.attrib}
        assert set(translated) == expected, (
            f"{localized}: missing {expected - set(translated)}, "
            f"unexpected {set(translated) - expected}"
        )
        for key, entry in translated.items():
            source = original[key].findtext("value") or ""
            value = entry.findtext("value") or ""
            assert value.strip(), f"Empty translation: {localized}: {key}"
            assert Counter(ARGUMENT.findall(source)) == Counter(ARGUMENT.findall(value)), (
                f"Format arguments differ: {localized}: {key}"
            )
            strings += 1
        files += 1
    assert files > 0, "No resource files found"
    return files, strings


if __name__ == "__main__":
    files, strings = check_resources()
    print(f"Validated {strings} translations in {files} Chinese resource files.")
