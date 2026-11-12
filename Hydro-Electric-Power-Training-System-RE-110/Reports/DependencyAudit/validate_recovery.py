"""Read-only checks for the dependency recovery, run from the project root."""
from pathlib import Path
import collections
import json
import re
import zipfile

out = Path('Reports/DependencyAudit')
before = json.loads((out / 'MissingScriptReferences.before.json').read_text())
after = json.loads((out / 'MissingScriptReferences.after.json').read_text())
manifest = json.loads((out / 'RestoredSources.json').read_text())
checks = []
for item in manifest:
    path = Path(item['restored_path'])
    assert path.exists(), path
    if item.get('guid'):
        assert 'guid: ' + item['guid'] in Path(str(path) + '.meta').read_text(), path
checks.append('PASS: recovered source files exist and recorded original GUIDs match.')

with zipfile.ZipFile(out / 'BeforeLegacyHandlerIsolation.zip') as archive:
    for path in archive.namelist():
        old = archive.read(path).decode('utf8').splitlines()
        new = Path(path).read_text(encoding='utf8').splitlines()
        assert len(old) == len(new), path
        pairs = [(a.strip(), b.strip()) for a, b in zip(old, new) if a != b]
        assert all((a, b) in {('m_Enabled: 1', 'm_Enabled: 0'), ('value: 1', 'value: 0')}
                   for a, b in pairs), path
    checks.append(f'PASS: {len(archive.namelist())} existing YAML assets changed only legacy handler enable flags; all other serialized content is preserved.')

guids = collections.defaultdict(list)
for path in Path('Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', path.read_text(encoding='utf8', errors='replace'), re.M)
    if match:
        guids[match[1]].append(path.as_posix())
duplicates = {g: p for g, p in guids.items() if len(p) > 1}
assert not duplicates, duplicates
checks.append('PASS: no duplicate asset GUIDs under Assets.')
checks.append(f'Missing GUID identities: {len(before)} -> {len(after)}; resolved {len(before.keys() - after.keys())}.')
checks.append(f'Unresolved serialized references across all Assets (including archived demos): {sum(map(len, before.values()))} -> {sum(map(len, after.values()))}.')
checks.append('Unity version: ' + Path('ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0])
settings = Path('ProjectSettings/ProjectSettings.asset').read_text()
checks.append('Input backend: ' + re.search(r'activeInputHandler: \d+', settings)[0])
(out / 'StaticValidation.txt').write_text('\n'.join(checks) + '\n')
print('\n'.join(checks))
