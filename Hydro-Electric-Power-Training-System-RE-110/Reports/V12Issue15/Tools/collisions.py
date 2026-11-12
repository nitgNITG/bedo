from geometry_lib import *
import itertools,time
F=json.load(open('Temp/V12BatchNext/free-definitions.json'));W={w['id']:w for w in D['wires']};E={e['id']:e for e in F['ends']};BASE={i:curves(w) for i,w in W.items()};defs={d['id']:d for d in F['definitions']};variants={};pairs=[]
for i,c in BASE.items():variants[(i,-1,-1)]=c
for d in F['definitions']:
 i=d['id'];ea=E[d['a']];eb=E[d['b']];c=BASE[i];p=c[0,0];q=c[-1,-1]
 for a,b in itertools.product(range(len(ea['mouths'])),range(len(eb['mouths']))):
  cc=c.copy()
  if len(ea['mouths'])>1 or len(eb['mouths'])>1:
   start=v(eb['mouths'][b] if d['reverse'] else ea['mouths'][a]);end=v(ea['mouths'][a] if d['reverse'] else eb['mouths'][b]);da=start-p;db=end-q
   # AlignAllocatedPlugEntries shifts first/last two knots and their handles.
   for k in range(len(cc)):
    cc[k,0:2]+=da if k<=1 else 0;cc[k,2:4]+=da if k==0 else 0
    cc[k,0:2]+=db if k==len(cc)-1 else 0;cc[k,2:4]+=db if k>=len(cc)-2 else 0
  variants[(i,a,b)]=cc
GP=set()
for case in D['cases']:
 if case['side']=='Free':continue
 for a,b in itertools.combinations(case['wires'],2):GP.add(tuple(sorted([a,b])))
pairs += [((i,-1,-1),(j,-1,-1),'Guided') for i,j in GP]
for da,db in itertools.combinations(F['definitions'],2):
 if {da['a'],da['b']}=={db['a'],db['b']}:continue
 for aa,ab,ba,bb in itertools.product(range(len(E[da['a']]['mouths'])),range(len(E[da['b']]['mouths'])),range(len(E[db['a']]['mouths'])),range(len(E[db['b']]['mouths']))):
  slotsa={da['a']:aa,da['b']:ab};slotsb={db['a']:ba,db['b']:bb}
  if any(slotsa[e]==slotsb[e] for e in slotsa.keys()&slotsb.keys()):continue
  pairs.append(((da['id'],aa,ab),(db['id'],ba,bb),'Free'))

def clouds(curves):return {k:samples(c,.065) for k,c in curves.items()}
S=clouds(variants)
def nearest(pa,pb,ra=.3):
 p,pp=pa;q,qq=pb
 if np.any(p.min(0)>q.max(0)+ra) or np.any(q.min(0)>p.max(0)+ra):return None
 ids=np.where(np.all((p>=q.min(0)-ra)&(p<=q.max(0)+ra),axis=1))[0]
 if not len(ids):return None
 qs=np.where(np.all((q>=p[ids].min(0)-ra)&(q<=p[ids].max(0)+ra),axis=1))[0]
 if not len(qs):return None
 best=(ra*ra,None,None)
 for start in range(0,len(ids),100):
  ii=ids[start:start+100];ds=((p[ii,None,:]-q[None,qs,:])**2).sum(2);x,y=np.unravel_index(ds.argmin(),ds.shape)
  if ds[x,y]<best[0]:best=(ds[x,y],ii[x],qs[y])
 if best[1] is None:return None
 return dict(distance=float(np.sqrt(best[0])),pointA=p[best[1]].tolist(),pointB=q[best[2]].tolist(),paramA=float(pp[best[1]]),paramB=float(qq[best[2]]))

def audit(state,threshold=.264,excludeJoint=True):
 result=[]
 for ka,kb,mode in pairs:
  a=state[ka];b=state[kb]
  if excludeJoint:
   centers=[]
   for ea in [W[ka[0]]['endpointA'],W[ka[0]]['endpointB']]:
    for eb in [W[kb[0]]['endpointA'],W[kb[0]]['endpointB']]:
     T={t['id']:t for t in D['targets']}
     if ea in T and eb in T and np.linalg.norm(v(T[ea]['center'])-v(T[eb]['center']))<.15:centers.append(v(T[ea]['center']))
   for c in centers:
    mask=np.linalg.norm(a[0]-c,axis=1)>2.3;a=(a[0][mask],a[1][mask]);mask=np.linalg.norm(b[0]-c,axis=1)>2.3;b=(b[0][mask],b[1][mask])
   if not len(a[0]) or not len(b[0]):continue
  n=nearest(a,b,threshold)
  if n:result.append(dict(a=list(ka),b=list(kb),mode=mode,nameA=W[ka[0]]['name'],nameB=W[kb[0]]['name'],**n))
 return result
if __name__=='__main__':
 t=time.time();r=audit(S);Path('Temp/V12BatchNext/contacts-before.json').write_text(json.dumps(dict(pairs=len(pairs),contacts=r),indent=2));print('pairs',len(pairs),'variants',len(variants),'contacts',len(r),'seconds',time.time()-t)
 seen=set()
 for x in r:
  key=tuple(sorted([x['a'][0],x['b'][0]]))
  if key in seen:continue
  seen.add(key);print(x['mode'],x['nameA'].split('/')[-1],' / ',x['nameB'].split('/')[-1],round(x['distance'],3),x['pointA'])
