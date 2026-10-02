#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
ASSETS = REPO / "Assets"
PRODUCT = ASSETS / "_Project"

errors: list[str] = []
warnings: list[str] = []

REQUIRED_DOCS = [
    "README.md",
    "QUICK_START.md",
    "DEPENDENCIES.md",
    "API.md",
    "UPGRADING.md",
    "CLEAN_IMPORT.md",
    "THIRD_PARTY_NOTICES.md",
    "RELEASE_CHECKLIST.md",
    "CI.md",
]

REQUIRED_SCENES = [
    "Assets/_Project/Scenes/SampleScene.unity",
    "Assets/_Project/Scenes/ArenaShowcase.unity",
    "Assets/_Project/Scenes/MobileDemo.unity",
]

TEXT_SUFFIXES = {
    ".cs", ".md", ".txt", ".json", ".asmdef", ".asmref", ".inputactions",
    ".shader", ".cginc", ".hlsl", ".compute", ".uss", ".uxml", ".yaml", ".yml",
}

FORBIDDEN_ARCHIVE_SUFFIXES = {".zip", ".rar", ".7z", ".unitypackage"}
FORBIDDEN_EXECUTABLE_SUFFIXES = {".exe", ".apk", ".app", ".msi", ".bat", ".cmd"}
GENERIC_ASSEMBLY_NAMES = {"Core", "Application", "Unity", "Runtime", "Game", "Editor"}


def fail(message: str) -> None:
    errors.append(message)


def warn(message: str) -> None:
    warnings.append(message)


def rel(path: Path) -> str:
    return path.relative_to(REPO).as_posix()


if not PRODUCT.is_dir():
    fail("Missing commercial product root: Assets/_Project")
else:
    if not (ASSETS / "_Project.meta").is_file():
        fail("Missing Assets/_Project.meta for the commercial root folder.")

if (ASSETS / "Scenes").exists() or (ASSETS / "Scenes.meta").exists():
    fail("Legacy Assets/Scenes content exists outside the commercial product root.")

for scene in REQUIRED_SCENES:
    if not (REPO / scene).is_file():
        fail(f"Missing required demo scene: {scene}")

docs_dir = PRODUCT / "Documentation"
for doc in REQUIRED_DOCS:
    if not (docs_dir / doc).is_file():
        fail(f"Missing required buyer documentation: Assets/_Project/Documentation/{doc}")

build_settings = REPO / "ProjectSettings" / "EditorBuildSettings.asset"
if not build_settings.is_file():
    fail("Missing ProjectSettings/EditorBuildSettings.asset")
else:
    build_text = build_settings.read_text(encoding="utf-8", errors="replace")
    for scene in REQUIRED_SCENES:
        if scene not in build_text:
            fail(f"Demo scene is not enabled/referenced in build settings: {scene}")

for path in PRODUCT.rglob("*"):
    path_text = rel(path)

    if len(path_text) >= 150:
        fail(f"Asset Store path limit reached/exceeded ({len(path_text)} chars): {path_text}")

    if any(part.lower() == "assetstoretools" for part in path.parts):
        fail(f"Forbidden AssetStoreTools folder inside product root: {path_text}")

    if not path.is_file():
        continue

    suffix = path.suffix.lower()
    if suffix in FORBIDDEN_ARCHIVE_SUFFIXES:
        fail(f"Archive/package file should not ship inside the product root: {path_text}")
    if suffix in FORBIDDEN_EXECUTABLE_SUFFIXES:
        fail(f"Executable file should not ship inside the product root: {path_text}")

    if "TextMesh Pro" in path_text or "EmojiOne" in path_text:
        fail(f"Third-party/sample resource unexpectedly included in product root: {path_text}")

    if suffix in TEXT_SUFFIXES:
        text = path.read_text(encoding="utf-8", errors="replace")
        match = re.search(r"(?<!/)\\b(TODO|FIXME)\\b(?!/)", text, flags=re.IGNORECASE)
        if match:
            fail(f"Release blocker marker {match.group(1).upper()} found in {path_text}")


# Every shipped asset file and folder should have a stable Unity meta file.
for path in PRODUCT.rglob("*"):
    if path.name.endswith(".meta"):
        continue
    meta = Path(str(path) + ".meta")
    if not meta.is_file():
        fail(f"Missing Unity meta file for {rel(path)}")

# Duplicate GUIDs are a common package-import failure.
guid_to_paths: dict[str, list[str]] = {}
for meta in ASSETS.rglob("*.meta"):
    text = meta.read_text(encoding="utf-8", errors="replace")
    match = re.search(r"^guid:\s*([0-9a-fA-F]+)\s*$", text, flags=re.MULTILINE)
    if not match:
        continue
    guid = match.group(1).lower()
    guid_to_paths.setdefault(guid, []).append(rel(meta))

for guid, paths in sorted(guid_to_paths.items()):
    if len(paths) > 1:
        fail(f"Duplicate Unity GUID {guid}: {', '.join(paths)}")

# Assembly names should not collide with common customer assemblies.
for asmdef in PRODUCT.rglob("*.asmdef"):
    try:
        data = json.loads(asmdef.read_text(encoding="utf-8"))
    except Exception as exc:
        fail(f"Invalid asmdef JSON in {rel(asmdef)}: {exc}")
        continue

    name = str(data.get("name", "")).strip()
    root_namespace = str(data.get("rootNamespace", "")).strip()

    if not name:
        fail(f"Assembly definition has no name: {rel(asmdef)}")
    elif name in GENERIC_ASSEMBLY_NAMES:
        fail(f"Assembly name is too generic for a commercial package: {name} ({rel(asmdef)})")
    elif not name.startswith("MiniTopDownShooter."):
        warn(f"Assembly name is not product-prefixed: {name} ({rel(asmdef)})")

    if root_namespace in {"Core", "Application", "Game", "Unity"}:
        warn(f"Root namespace remains generic and should be audited before 1.0: {root_namespace} ({rel(asmdef)})")

# Guard against development-only direct package dependencies returning.
manifest_path = REPO / "Packages" / "manifest.json"
if manifest_path.is_file():
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    dependencies = set(manifest.get("dependencies", {}).keys())
    banned = {
        "com.unity.collab-proxy",
        "com.unity.ide.rider",
        "com.unity.ide.visualstudio",
        "com.unity.multiplayer.center",
        "com.unity.timeline",
        "com.unity.visualscripting",
    }
    for dependency in sorted(dependencies & banned):
        fail(f"Development-only direct package dependency returned: {dependency}")

print("Mini Top Down Shooter - Asset Store preflight")
print(f"Product root: {rel(PRODUCT)}")

for message in warnings:
    print(f"WARNING: {message}")

for message in errors:
    print(f"ERROR: {message}")

print(f"Result: {len(errors)} error(s), {len(warnings)} warning(s).")

if errors:
    sys.exit(1)
