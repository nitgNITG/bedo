"""Read-only equipment prefab structure checks; run from the project root."""
from pathlib import Path
import hashlib
import json
import re
import zipfile

report = Path('Reports/EquipmentPrefabs')
folder = Path('Assets/_Project/Prefabs/Equipment')
assets = sorted(folder.rglob('*.prefab'))
assert len(assets) == 16, len(assets)
guid_paths = {}
for meta in Path('Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf8', errors='replace'), re.M)
    if match:
        assert match[1] not in guid_paths, (match[1], str(meta))
        guid_paths[match[1]] = meta.as_posix()[:-5]

rows = []
for path in assets:
    text = path.read_text(encoding='utf8')
    parents = re.findall(r'm_SourcePrefab: \{fileID: \d+, guid: (\w+), type: 3\}', text)
    for guid in parents:
        assert guid in guid_paths, (path, guid)
    name = path.stem
    base = {
        'Rheostats': 'Rheostat Assembly',
        'Lamps': 'Lamp On Off Assembly',
        'Connectors': 'Lamp Power Connector Assembly'
    }[path.parent.name]
    if name == base:
        assert not parents, path
    elif name == 'Wind Lamp Outputs':
        assert 'Assets/_Project/Models/Trainers/Outputs.fbx' in [guid_paths[g] for g in parents], path
        lamp_variants = {p for p in (guid_paths[g] for g in parents) if '/Equipment/Lamps/' in p}
        assert len(lamp_variants) == 8, lamp_variants
    else:
        base_path = path.with_name(base + '.prefab').as_posix()
        assert base_path in [guid_paths[g] for g in parents], (path, parents)
    rows.append({'asset': path.as_posix(), 'source_prefabs': [guid_paths[g] for g in parents],
                 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})

menu = Path('Assets/_Project/Scenes/Main Menu.unity')
with zipfile.ZipFile(report / 'VerificationBaseline.zip') as backup:
    assert menu.read_bytes() == backup.read('Main Menu.before.unity.txt'), 'Main Menu changed'
(report / 'PrefabAssets.json').write_text(json.dumps(rows, indent=2))
print('PASS: 16 equipment assets; 3 shared bases, 12 equipment variants and 1 model variant.')
print('PASS: output model retains its Outputs.fbx source and nests all eight lamp variants.')
print('PASS: all source prefab GUIDs resolve; no duplicate GUIDs under Assets.')
print('PASS: Main Menu scene is byte-for-byte unchanged.')
