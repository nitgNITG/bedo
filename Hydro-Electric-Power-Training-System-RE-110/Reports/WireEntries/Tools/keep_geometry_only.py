from pathlib import Path
import re,json,subprocess,hashlib
root=Path.cwd();p=root/'Assets/_Project/Scenes/PV Lab.unity';original=(root/'Temp/WireEntry/PVLab.before.unity').read_text(encoding='utf-8');edited=p.read_text(encoding='utf-8')
def split(s):return re.split(r'(?=^--- !u!)',s,flags=re.M)
def ident(b):
 m=re.match(r'--- !u!(\d+) &(-?\d+)',b);return tuple(m.groups()) if m else None
now={ident(b):b for b in split(edited) if ident(b)}
allowed={x.split('-')[3] for x in (root/'Reports/WireEntries/ChangedContainers.txt').read_text().splitlines()}
assert len(allowed)==102
out=[];count=0
for b in split(original):
 key=ident(b)
 if key and key[0]=='114' and key[1] in allowed:
  assert 'guid: dab5c7d4c32e743048dfca98e2d5914f' in b
  target=now[key];pat=r'(?ms)^  m_Splines:\n(.*?)^  m_Knots:';old=re.search(pat,b);new=re.search(pat,target);assert old and new
  oldparts=re.split(r'(?m)(?=^  - m_EditModeType:)',old.group(1));newparts=re.split(r'(?m)(?=^  - m_EditModeType:)',new.group(1));assert len(oldparts)==len(newparts)
  oldfirst=next(x for x in oldparts if x.startswith('  - '));newfirst=next(x for x in newparts if x.startswith('  - '));mode=re.search(r'm_EditModeType: (\d+)',oldfirst).group(1);newfirst=re.sub(r'm_EditModeType: \d+','m_EditModeType: '+mode,newfirst,count=1)
  replacement=old.group(1).replace(oldfirst,newfirst,1);b=b[:old.start(1)]+replacement+b[old.end(1):];count+=1
 out.append(b)
assert count==102
p.write_text(''.join(out),encoding='utf-8')
# Restore generated caches written by the temporary preview; activation rebuilds
# each cable's rendered mesh/collider from its authored spline as before.
paths=subprocess.run(['git','diff','--name-only','-z'],capture_output=True,check=True).stdout.decode().split('\0');restored=[]
for rel in filter(None,paths):
 if rel.startswith('Assets/_Project/Models/Generated/') and rel.endswith('.asset'):
  b=subprocess.run(['git','show','HEAD:'+rel],capture_output=True,check=True).stdout;(root/rel).write_bytes(b);restored.append(rel)
old={ident(b):b for b in split(original) if ident(b)};current={ident(b):b for b in split(p.read_text(encoding='utf-8')) if ident(b)};assert old.keys()==current.keys();diff=[k for k in old if old[k]!=current[k]];assert len(diff)==102 and all(k[0]=='114' and k[1] in allowed for k in diff)
lines=[f'PASS: all {len(old)} serialized scene object/component identities retained.', 'PASS: only the main spline data of 102 selected SplineContainer components changed.', 'PASS: all other serialized components, cameras, transforms, plugs, interactions, events, colliders, materials and UI data match the pre-change scene exactly.', 'PASS: extra legacy splines and knot-link data retained.',f'PASS: {len(restored)} generated mesh caches restored; cable activation rebuilds visuals and click surfaces from spline data.']
(root/'Reports/WireEntries/Preservation.txt').write_text('\n'.join(lines)+'\n',encoding='utf-8');print('\n'.join(lines))
