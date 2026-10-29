from geometry_lib import *
from collisions import nearest
import itertools
file=Path('Temp/V12BatchNext/multi-runtime.jsonl');rows=[json.loads(x) for x in file.read_text().splitlines()];results=[]
for row in rows:
 paths=row['shapes'];shapes=[samples(np.array([v(x) for x in s['controls']]).reshape(-1,4,3),.035) for s in paths];bad=[]
 for i,j in itertools.combinations(range(len(paths)),2):
  limit=paths[i]['radius']+paths[j]['radius'];n=nearest(shapes[i],shapes[j],limit)
  if n and n['distance']<limit:bad.append(dict(a=paths[i]['name'],b=paths[j]['name'],**n))
 print('seed',row['seed'],'cables',len(paths),'touches',len(bad));results.append(dict(seed=row['seed'],cables=len(paths),pairs=len(paths)*(len(paths)-1)//2,contacts=bad))
Path('Temp/V12BatchNext/multi-verification.json').write_text(json.dumps(results,indent=2))
