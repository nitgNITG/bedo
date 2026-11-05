from pathlib import Path
import json,numpy as np
class cKDTree:
 def __init__(self,data): self.data=data
 def query(self,points):
  result=[]
  for k in range(0,len(points),64):
   delta=points[k:k+64,None,:]-self.data[None,:,:]
   result.append(np.sqrt(np.sum(delta*delta,axis=2).min(axis=1)))
  return np.concatenate(result),None

def vec(p):return np.array([p['x'],p['y'],p['z']],dtype=float)
def sample(item):
 pts=[]
 for k in range(len(item['points'])-1):
  a=vec(item['points'][k]);b=a+vec(item['tout'][k]);d=vec(item['points'][k+1]);c=d+vec(item['tin'][k+1]);length=np.linalg.norm(a-b)+np.linalg.norm(b-c)+np.linalg.norm(c-d);n=max(32,min(1500,int(length/.07)+1));t=np.linspace(0,1,n)[:,None];u=1-t;pts.append(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d)
 return np.vstack(pts)
before=json.loads(Path('Temp/WireEntry/audit.before.json').read_text())['wires'];after=json.loads(Path('Temp/WireEntry/audit.after.json').read_text())['wires'];lookup={(w['context'],w['name']):w for w in after};cache={}
for i in before:
 key=(i['context'],i['name'])
 if key not in lookup:continue
 a=lookup[key];p=sample(i);q=sample(a);cache[key]=(p,cKDTree(p),q,cKDTree(q))
new=[];pairs=0
for context in ['Exp 1','Exp 2','Exp 3','Exp 4']:
 wires=[w for w in before if w['context']==context]
 for j,a in enumerate(wires):
  for b in wires[j+1:]:
   ka=(context,a['name']);kb=(context,b['name'])
   if ka not in cache or kb not in cache:continue
   olda,oldta,aa,ta=cache[ka];oldb,oldtb,bb,tb=cache[kb]
   # Stacked plugs on a common pin intentionally share the connector area.
   shared=False
   for pa in [a['a'],a['b']]:
    for pb in [b['a'],b['b']]:
     if not pa or not pb:continue
     delta=vec(pa['mouth'])-vec(pb['mouth']);axis=vec(pa['direction']);planar=delta-axis*np.dot(delta,axis)
     if np.linalg.norm(planar)<.1 and np.linalg.norm(delta)<2:shared=True
   if shared:continue
   pairs+=1
   lower=np.maximum(0,np.maximum(aa.min(axis=0)-bb.max(axis=0),bb.min(axis=0)-aa.max(axis=0)))
   if np.linalg.norm(lower)>.07:continue
   dnew=float(tb.query(aa)[0].min())
   if dnew>=.07:continue
   dold=float(oldtb.query(olda)[0].min())
   if dold>.14 and dnew<.07:new.append((context,a['name'],b['name'],dold,dnew))
report={'scope':'Dense curve sampling of Guided cable pairs; shared physical-pin regions excluded. Existing overlaps are distinguished from new ones. This is not an exhaustive mesh collision proof.','pairs':pairs,'newCloseApproaches':new,'thresholds':{'beforeClearAbove':.14,'afterBelow':.07},'sampleSpacingApprox':.07}
Path('Reports/WireEntries/Clearance.json').write_text(json.dumps(report,indent=2));print('Checked',pairs,'guided cable pairs; new close approaches',len(new));print(new[:8])
