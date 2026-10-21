#!/usr/bin/env python3
import os
import sys
import re
from pathlib import Path
from collections import defaultdict

WORKSPACE = Path("/Volumes/DiscoAccesoRapido/UnityExternalJobs/unbreakable/running_late")
ASSETS_DIR = WORKSPACE / "Assets"
PROJECT_SETTINGS = WORKSPACE / "ProjectSettings" / "EditorBuildSettings.asset"

def format_bytes(size):
    for unit in ['B', 'KB', 'MB', 'GB']:
        if size < 1024.0:
            return f"{size:.2f} {unit}"
        size /= 1024.0
    return f"{size:.2f} TB"

def get_active_scenes():
    scenes = []
    if not PROJECT_SETTINGS.exists():
        return scenes
    with open(PROJECT_SETTINGS, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()
    
    # Matches: - enabled: 1\n    path: Assets/Scenes/...
    pattern = re.compile(r'enabled:\s*1\s+path:\s*([^\r\n]+)')
    for match in pattern.finditer(content):
        scene_path = match.group(1).strip()
        scenes.append(scene_path)
    return scenes

def build_guid_map():
    guid_to_path = {}
    path_to_guid = {}
    print("Indexing asset GUIDs...")
    for meta_file in ASSETS_DIR.rglob("*.meta"):
        try:
            with open(meta_file, "r", encoding="utf-8", errors="ignore") as f:
                for line in f:
                    if line.startswith("guid:"):
                        guid = line.split("guid:")[1].strip()
                        asset_path = str(meta_file)[:-5] # remove .meta
                        if os.path.exists(asset_path):
                            guid_to_path[guid] = asset_path
                            path_to_guid[asset_path] = guid
                        break
        except Exception:
            pass
    return guid_to_path, path_to_guid

def extract_guids_from_file(file_path):
    guids = set()
    try:
        with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
            # Standard Unity guid: [0-9a-f]{32}
            found = re.findall(r'guid:\s*([0-9a-f]{32})', content)
            guids.update(found)
    except Exception:
        pass
    return guids

def get_all_build_dependencies(active_scenes, guid_to_path, path_to_guid):
    referenced_guids = set()
    visited_paths = set()
    queue = []

    # Seed with active scenes
    for scene in active_scenes:
        abs_path = str(WORKSPACE / scene)
        if os.path.exists(abs_path):
            queue.append(abs_path)
            visited_paths.add(abs_path)
            if abs_path in path_to_guid:
                referenced_guids.add(path_to_guid[abs_path])

    # Seed with ALL assets inside any 'Resources' folder
    for res_file in ASSETS_DIR.rglob("*"):
        if res_file.is_file() and not res_file.name.endswith(".meta") and "/Resources/" in str(res_file):
            abs_path = str(res_file)
            queue.append(abs_path)
            visited_paths.add(abs_path)
            if abs_path in path_to_guid:
                referenced_guids.add(path_to_guid[abs_path])

    print(f"Traversing dependency graph from {len(queue)} initial root seeds...")
    while queue:
        current_path = queue.pop(0)
        ext = Path(current_path).suffix.lower()
        # Only parse text-based Unity assets for GUIDs
        if ext in ['.unity', '.prefab', '.asset', '.mat', '.anim', '.controller', '.overridecontroller']:
            found_guids = extract_guids_from_file(current_path)
            for g in found_guids:
                if g not in referenced_guids:
                    referenced_guids.add(g)
                    dep_path = guid_to_path.get(g)
                    if dep_path and dep_path not in visited_paths:
                        visited_paths.add(dep_path)
                        queue.append(dep_path)

    return referenced_guids, visited_paths

def main():
    print("=" * 70)
    print("  RUNNING LATE - BUILD SIZE & ASSET USAGE AUDIT")
    print("=" * 70)

    scenes = get_active_scenes()
    print(f"Active Build Scenes ({len(scenes)}):")
    for s in scenes:
        print(f"  • {s}")
    print()

    guid_to_path, path_to_guid = build_guid_map()
    print(f"Total Assets with GUIDs: {len(guid_to_path)}")

    ref_guids, visited_paths = get_all_build_dependencies(scenes, guid_to_path, path_to_guid)
    print(f"Total Assets referenced in Build: {len(visited_paths)}\n")

    # Analyze referenced assets by category
    build_categories = defaultdict(lambda: {'count': 0, 'size': 0})
    top_build_assets = []

    for path_str in visited_paths:
        p = Path(path_str)
        if not p.is_file():
            continue
        size = p.stat().st_size
        ext = p.suffix.lower()

        if ext in ['.png', '.jpg', '.jpeg', '.tga', '.psd', '.exr']:
            cat = 'Textures / Sprites'
        elif ext in ['.wav', '.mp3', '.ogg']:
            cat = 'Audio'
        elif ext in ['.ttf', '.otf'] or (ext == '.asset' and 'font' in p.name.lower()):
            cat = 'Fonts'
        elif ext in ['.prefab']:
            cat = 'Prefabs'
        elif ext in ['.unity']:
            cat = 'Scenes'
        else:
            cat = 'Other'

        build_categories[cat]['count'] += 1
        build_categories[cat]['size'] += size
        top_build_assets.append((p, size, cat))

    print("-" * 70)
    print("BUILD ASSETS BREAKDOWN (What actually goes into mobile package):")
    print("-" * 70)
    total_build_size = sum(c['size'] for c in build_categories.values())
    for cat, data in sorted(build_categories.items(), key=lambda x: x[1]['size'], reverse=True):
        pct = (data['size'] / total_build_size * 100) if total_build_size > 0 else 0
        print(f"  {cat:22} | {data['count']:5} files | {format_bytes(data['size']):>10} ({pct:5.1f}%)")
    print(f"  {'TOTAL BUILD DISK SIZE':22} | {len(visited_paths):5} files | {format_bytes(total_build_size):>10}")
    print()

    print("-" * 70)
    print("TOP 15 LARGEST ASSETS IN BUILD:")
    print("-" * 70)
    top_build_assets.sort(key=lambda x: x[1], reverse=True)
    for p, size, cat in top_build_assets[:15]:
        rel = p.relative_to(WORKSPACE)
        print(f"  [{format_bytes(size):>9}] [{cat:16}] {rel}")
    print()

    # Analyze UNUSED Assets in Assets/Imported
    imported_unused = []
    imported_used = []
    imported_dir = ASSETS_DIR / "Imported"

    if imported_dir.exists():
        for f in imported_dir.rglob("*"):
            if f.is_file() and not f.name.endswith(".meta") and not f.name.endswith(".cs"):
                f_str = str(f)
                size = f.stat().st_size
                if f_str in visited_paths:
                    imported_used.append((f, size))
                else:
                    imported_unused.append((f, size))

    print("-" * 70)
    print("UNUSED REPOSITORY ASSETS (Assets/Imported Bloat):")
    print("-" * 70)
    packs_unused = defaultdict(lambda: {'count': 0, 'size': 0})
    for f, size in imported_unused:
        try:
            rel = f.relative_to(imported_dir)
            pack_name = rel.parts[0]
        except Exception:
            pack_name = "Other"
        packs_unused[pack_name]['count'] += 1
        packs_unused[pack_name]['size'] += size

    total_unused_size = sum(p['size'] for p in packs_unused.values())
    for pack, data in sorted(packs_unused.items(), key=lambda x: x[1]['size'], reverse=True):
        print(f"  {pack:35} | {data['count']:5} unused files | {format_bytes(data['size']):>10}")
    print(f"  {'TOTAL UNUSED IN ASSETS/IMPORTED':35} | {len(imported_unused):5} unused files | {format_bytes(total_unused_size):>10}")
    print("=" * 70)

if __name__ == '__main__':
    main()
