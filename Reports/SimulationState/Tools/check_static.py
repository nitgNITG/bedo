from pathlib import Path
import zipfile,json,hashlib,subprocess
root=Path.cwd();report=root/'Reports/SimulationState';lines=[]
with zipfile.ZipFile(report/'Before/SourceAndScenes.zip') as z:
 old=z.read('Assets/_Project/Scenes/Main Menu.unity');assert old==(root/'Assets/_Project/Scenes/Main Menu.unity').read_bytes();lines.append('PASS: Main Menu is byte-for-byte unchanged.')
 old=z.read('Assets/_Project/Scripts/UI/Graph/PVCSVReader.cs')
 current=(root/'Assets/_Project/Scripts/UI/Graph/PVCSVReader.cs').read_bytes()
 for sep in (b'\r\n',b'\n'):
  for name in (b'OffGridReading',b'WindReading'):
   current=current.replace(b'    [System.Serializable]'+sep+b'    public struct '+name,b'    public struct '+name)
 assert old==current;lines.append('PASS: PVCSVReader equations unchanged; only two result-struct serialization attributes added.')
for item in json.loads((root/'Reports/EquipmentPrefabs/PrefabAssets.json').read_text()):
 assert hashlib.sha256((root/item['asset']).read_bytes()).hexdigest()==item['sha256'],item['asset']
lines.append('PASS: all 16 equipment prefab asset files are unchanged since phase 3.')
for line in (root/'Reports/ProjectOrganization/AssetMoves.tsv').read_text().splitlines()[1:]:
 cells=line.split('\t')
 if len(cells)<2 or not cells[1].endswith('.bytes'):continue
 prior=subprocess.run(['git','show','HEAD:'+cells[0]],capture_output=True)
 assert prior.returncode==0,(cells[0],prior.stderr.decode())
 assert prior.stdout.replace(b'\r\n',b'\n')==(root/cells[1]).read_bytes().replace(b'\r\n',b'\n'),cells[1]
lines.append('PASS: wind mathematical/reference CSV content matches tracked originals (line endings normalized).')
b=(report/'before.samples.txt').read_text().splitlines();a=(report/'after.samples.txt').read_text().splitlines()
assert len(b)==176 and a==b;lines.append('PASS: all 176 before/after numerical samples match exactly.')
for path in (root/'Assets/_Project/Data/Wind/Simulation').rglob('*.asset'):
 s=path.read_text();assert '  _saved: 1' not in s,path
 if '/Runtime State.asset' in path.as_posix():assert '    requestedRPM: 0' in s and '    turbineRunning: 0' in s,path
lines.append('PASS: context assets retain zero/off authored input defaults; none use PlayerPrefs persistence.')
(report/'StaticValidation.txt').write_text('\n'.join(lines)+'\n');print('\n'.join(lines))


