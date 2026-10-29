import json,numpy as np, math,copy
from pathlib import Path
import json,numpy as np
from pathlib import Path
D=json.load(open('Temp/WireVisibility/batch-next-before.json'))
def v(x):return np.array([x['x'],x['y'],x['z']],float)
def rotation(q):
 x,y,z,w=[q[k] for k in ['x','y','z','w']];return np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])
def project(c,p):
 xyz=(p-v(c['position']))@rotation(c['rotation']);f=540/np.tan(np.deg2rad(c['fov']/2));return xyz[:,:2]/xyz[:,2:3]*f+np.array([960,540]),xyz[:,2]
def sample(w):
 points=np.array([v(x) for x in w['points']]);ins=np.array([v(x) for x in w['tin']]);outs=np.array([v(x) for x in w['tout']]);result=[]
 for k in range(len(points)-1):
  a=points[k];b=a+outs[k];d=points[k+1];c=d+ins[k+1];length=sum(np.linalg.norm(t) for t in [b-a,c-b,d-c]);n=max(20,int(length/.1));t=np.linspace(0,1,n)[:,None];u=1-t;result.append(u**3*a+3*u*u*t*b+3*u*t*t*c+t**3*d)
 return np.vstack(result)

def curves(w):
 p=np.array([v(x) for x in w['points']]);i=np.array([v(x) for x in w['tin']]);o=np.array([v(x) for x in w['tout']]);return np.array([[p[k],p[k]+o[k],p[k+1]+i[k+1],p[k+1]] for k in range(len(p)-1)])
def split(c,t):
 a,b,d=(1-t)*c[:-1]+t*c[1:];e=(1-t)*a+t*b;f=(1-t)*b+t*d;g=(1-t)*e+t*f;return np.array([c[0],a,e,g]),np.array([g,f,d,c[3]])
def take(c,a,b):
 left,_=split(c,b)
 if a<=0:return left
 _,result=split(left,a/b);return result
def samples(cur,spacing=.15):
 result=[];params=[]
 for k,c in enumerate(cur):
  length=sum(np.linalg.norm(t) for t in np.diff(c,axis=0));n=max(12,int(length/spacing));t=np.linspace(0,1,n);u=1-t;result.append(u[:,None]**3*c[0]+3*(u*u*t)[:,None]*c[1]+3*(u*t*t)[:,None]*c[2]+(t**3)[:,None]*c[3]);params.extend(k+t)
 return np.vstack(result),np.array(params)
def bump(cur,index,param,delta,span):
 pts,g=samples(cur,.06);length=np.r_[0,np.cumsum(np.linalg.norm(np.diff(pts,axis=0),axis=1))];middle=np.interp(index+param,g,length);a=max(length[np.searchsorted(g,1)],middle-span);b=min(length[np.searchsorted(g,len(cur)-1)],middle+span)
 if b-a<.3:return None
 center=np.clip(middle,a+.05,b-.05);bounds=[a,center,b];gb=np.interp(bounds,length,g);result=[]
 def weight(s):
  if s<=a or s>=b:return 0.,0.
  if s<center:
   t=(s-a)/(center-a);return .5-.5*np.cos(np.pi*t),.5*np.pi*np.sin(np.pi*t)/(center-a)
  t=(s-center)/(b-center);return .5+.5*np.cos(np.pi*t),-.5*np.pi*np.sin(np.pi*t)/(b-center)
 for k,c in enumerate(cur):
  ts=[0]+[float(x-k) for x in gb if k+.000001<x<k+1-.000001]+[1]
  for l,r in zip(ts,ts[1:]):
   cc=take(c,l,r);sa=np.interp(k+l,g,length);sb=np.interp(k+r,g,length);wa,da=weight(sa);wb,db=weight(sb)
   weights=np.array([wa,wa+da*np.linalg.norm(cc[1]-cc[0]),wb-db*np.linalg.norm(cc[3]-cc[2]),wb]);cc=cc+weights[:,None]*delta;result.append(cc)
 return np.array(result)
