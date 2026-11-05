from geometry_lib import *
from collisions import nearest
p=Path('Temp/V12BatchNext/collision-results.jsonl');rows=[json.loads(l) for l in p.read_text().splitlines()];bad=[];changed=[];drift=0
for row in rows:
 def cur(key):return np.array([v(x) for x in row[key]['controls']]).reshape(-1,4,3)
 a,b,before=cur('afterA'),cur('afterB'),cur('beforeB');r=row['afterA']['radius']+row['afterB']['radius'];n=nearest(samples(a,.035),samples(b,.035),r)
 if n and n['distance']<r:bad.append({'index':row['index'],'a':row['a'],'b':row['b'],**n})
 # An obstructing old cable may detour when the new fixed plug entry is blocked.
 drift=max(drift,float(np.abs(b[[0,-1]][[0,1],[0,3]]-before[[0,-1]][[0,1],[0,3]]).max()))
 if b.shape!=before.shape or not np.allclose(b,before):changed.append(row['index'])
print('checked',len(rows),'fail',len(bad),'detoured',len(changed),'mouth drift',drift)
for x in bad[:12]:print(x['index'],x['a'],'/',x['b'],round(x['distance'],3))
Path('Temp/V12BatchNext/collision-verification.json').write_text(json.dumps({'checked':len(rows),'failures':bad,'changed':changed,'mouthDrift':drift},indent=2))
