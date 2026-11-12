from pathlib import Path
import csv,json,subprocess,hashlib,math
root=Path.cwd(); report=root/'Reports/ScientificValidation'; temp=root/'Temp/ScientificValidation';temp.mkdir(parents=True,exist_ok=True)
source=root/'Assets/_Project/Scripts/UI/Graph/PVCSVReader.cs'
# Compile the production wind methods verbatim; only engine primitives are substituted.
s=source.read_text(encoding='utf-8'); s=s[:s.index('    public List<PVPoint> data')]+ '\n}'
stubs='''namespace UnityEngine {
public class MonoBehaviour {} public class TextAsset {public string text; public TextAsset(string t){text=t;}}
public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;} public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>new Vector2(Mathf.LerpUnclamped(a.x,b.x,Mathf.Clamp(t,0,1)),Mathf.LerpUnclamped(a.y,b.y,Mathf.Clamp(t,0,1)));}
public static class Mathf {public static float Max(float a,float b)=>System.Math.Max(a,b);public static int Max(int a,int b)=>System.Math.Max(a,b);public static float Min(float a,float b)=>System.Math.Min(a,b);public static float Clamp(float v,float a,float b)=>Max(a,Min(b,v));public static float LerpUnclamped(float a,float b,float t)=>a+(b-a)*t;public static float InverseLerp(float a,float b,float v)=>a==b?0:Clamp((v-a)/(b-a),0,1);}}
'''
main='''class Program { static void Main(string[] args){System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
foreach(var line in System.IO.File.ReadAllLines(args[0])){var p=line.Split('\\t');var asset=new UnityEngine.TextAsset(System.IO.File.ReadAllText(p[2]));bool ok;object result;
if(p[1]=="wind"){ok=PVCSVReader.TryCalculateWind(asset,float.Parse(p[3]),float.Parse(p[4]),out var r);result=r;}
else if(p[1]=="graph"){ok=PVCSVReader.TryCalculateWindGraph(asset,float.Parse(p[3]),out var r);System.Console.WriteLine(p[0]+"\\t"+ok+"\\tvoltage="+r.ToString("R"));continue;}
else{ok=PVCSVReader.TryCalculateOffGrid(asset,float.Parse(p[3]),int.Parse(p[4]),int.Parse(p[5]),bool.Parse(p[6]),out var r,bool.Parse(p[7]));result=r;}
System.Console.WriteLine(p[0]+"\\t"+ok+"\\t"+string.Join(";",result.GetType().GetFields().Select(f=>f.Name+"="+((float)f.GetValue(result)).ToString("R"))));}}}
'''
(temp/'Solver.cs').write_text(s+stubs+main,encoding='utf-8')
cases=[]; expected={}; provenance={}
def add(id,kind,asset,inputs,exp,origin):
 cases.append('\t'.join([id,kind,str(root/asset),*map(str,inputs)]));expected[id]=exp;provenance[id]=origin
fields={'AC_Voltage_V':'acVoltage','AC_Current_A':'acCurrent','AC_Power_W':'acPower','DC_Voltage_V':'dcVoltage','DC_Current_A':'dcCurrent','DC_Power_W':'dcPower','RPM':'rpm','Resistance_Ohm':'resistance'}
for name,start in [('WindExp1Data',3),('WindExp2Data',70),('WindExp4Data',70)]:
 for i,row in enumerate(csv.DictReader((root/f'Assets/_Project/Data/Wind/{name}.bytes').read_text().splitlines())):
  add(f'{name}-{i+1}','wind',f'Assets/_Project/Data/Wind/{name}.bytes',[row['RPM'],row['Resistance_Ohm']],{v:float(row[k]) for k,v in fields.items()},f'workbook sheet1 row {start+i}')
for i,row in enumerate(csv.DictReader((root/'Assets/_Project/Data/Wind/WindExp1Graph.bytes').read_text().splitlines())):
 add(f'graph-reference-{i+1}','graph','Assets/_Project/Data/Wind/WindExp1Graph.bytes',[row['RPM']],{'voltage':float(row['Voltage_V'])},f'workbook O{i+3}:P{i+3}; reference-only API, live graph uses DC model')
# Off-grid source rows have one halogen DC and one halogen AC; compare explicit workbook grid columns.
rows=list(csv.DictReader((root/'Assets/_Project/Data/Wind/WindExp4Data.bytes').read_text().splitlines()))
for i,row in enumerate(rows[:6]):
 add(f'offgrid-grid-{i}','off','Assets/_Project/Data/Wind/WindExp4Data.bytes',[row['DC_Current_A'],1,1,True,False],{'gridCurrent':float(row['Grid_AC_With_Wind_A']),'batteryCurrent':9,'acLoadCurrent':.2,'acLoadVoltage':220,'dcLoadCurrent':2},f'workbook X{70+i},V{70+i},P{70+i},R{70+i},L{70+i}')
# Boundary expectations independent of the CSV solver implementation.
for rpm in [0,200,400,600,800,1000]:
 for r in [4,20,40,60,80,100,150,800]:
  add(f'boundary-{rpm}-{r}','wind','Assets/_Project/Data/Wind/WindExp1Data.bytes',[rpm,r],{'rpm':rpm,'acVoltage':.055*rpm,'acCurrent':.055*rpm/r,'acPower':(.055*rpm)**2/r},'AC workbook equations F2:H2; >100 Ohm calibration approval pending')
for dc,ac,led,cur in [(0,0,False,0),(1,2,True,0),(1,2,True,20),(1,1,False,0)]:
 load=dc*(.2 if led else 2);equiv=ac*2;balance=cur-load-equiv
 add(f'balance-{dc}-{ac}-{led}-{cur}','off','Assets/_Project/Data/Wind/WindExp4Data.bytes',[cur,dc,ac,False,led],{'batteryCurrent':min(balance,9),'dumpCurrent':max(0,balance-9),'dcLoadCurrent':load,'inverterCurrent':equiv},'derived workbook current balance; variant assumptions require approval')
for name,rpm,res in [('zero-resistance',200,0),('negative-resistance',200,-1),('nan-rpm','NaN',100),('infinite-rpm','Infinity',100)]:
 add(name,'wind','Assets/_Project/Data/Wind/WindExp1Data.bytes',[rpm,res],None,'invalid input rejection')
(temp/'cases.tsv').write_text('\n'.join(cases),encoding='utf-8')
unity=Path('C:/Program Files/Unity/Hub/Editor/6000.3.21f1/Editor/Data');runtime=unity/'NetCoreRuntime/shared/Microsoft.NETCore.App/6.0.21';dotnet=unity/'NetCoreRuntime/dotnet.exe'
rsp=['-nologo','-target:exe',f'-out:"{temp / "Solver.dll"}"',*['-r:"'+str(p)+'"' for p in [runtime/n for n in ['System.Private.CoreLib.dll','System.Runtime.dll','System.Console.dll','System.Collections.dll','System.Linq.dll','System.IO.FileSystem.dll']]],f'"{temp / "Solver.cs"}"']
(temp/'compile.rsp').write_text('\n'.join(rsp),encoding='utf-8')
p=subprocess.run([str(dotnet),str(unity/'DotNetSdkRoslyn/csc.dll'),'@'+str(temp/'compile.rsp')],capture_output=True,text=True);(report/'Compiler.txt').write_text(p.stdout+p.stderr,encoding='utf-8');assert p.returncode==0,p.stdout+p.stderr
(temp/'Solver.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.21'}}}))
p=subprocess.run([str(dotnet),str(temp/'Solver.dll'),str(temp/'cases.tsv')],capture_output=True,text=True);assert p.returncode==0,p.stderr
out=[]
for line in p.stdout.splitlines():
 id,ok,data=line.split('\t');actual={k:float(v) for k,v in (pair.split('=') for pair in data.split(';'))}; exp=expected[id]
 if exp is None:out.append({'case':id,'field':'accepted','expected':False,'actual':ok=='True','status':'PASS' if ok=='False' else 'FAIL','reference':provenance[id]});continue
 for field,value in exp.items():
  val=actual[field];tol=max(1e-5,abs(value)*2e-6);passed=ok=='True' and math.isfinite(val) and abs(val-value)<=tol
  out.append({'case':id,'field':field,'expected':value,'actual':val,'tolerance':tol,'status':'PASS' if passed else 'FAIL','reference':provenance[id]})
with (report/'Results.csv').open('w',newline='',encoding='utf-8') as f:
 w=csv.DictWriter(f,fieldnames=['case','field','expected','actual','tolerance','status','reference']);w.writeheader();w.writerows(out)
summary={'cases':len(cases),'assertions':len(out),'passed':sum(x['status']=='PASS' for x in out),'failed':sum(x['status']=='FAIL' for x in out),'solverSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'tolerance':'provisional engineering tolerance max(1e-5 absolute, 2e-6 relative); BEDO approval required','engine':'production wind solver methods compiled verbatim with minimal engine primitive substitutes; not Unity/player UI validation'}
(report/'Summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(json.dumps(summary,indent=2))

assert summary['failed']==0, 'Numerical validation failed; see Results.csv'
