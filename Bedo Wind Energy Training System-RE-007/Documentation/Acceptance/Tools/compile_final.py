from pathlib import Path
import subprocess,re
root=Path.cwd();out=root/'Documentation/Acceptance';tmp=root/'Temp/FinalAcceptanceCompile';tmp.mkdir(parents=True,exist_ok=True)
unity=Path('C:/Program Files/Unity/Hub/Editor/6000.3.21f1/Editor/Data');compiler=[str(unity/'NetCoreRuntime/dotnet.exe'),str(unity/'DotNetSdkRoslyn/csc.dll')]
for name in ['Assembly-CSharp','Assembly-CSharp-Editor']:
 source=root/f'Library/Bee/artifacts/1900b0aE.dag/{name}.rsp';s=source.read_text(encoding='utf-8-sig')
 assert not any(x in s for x in ['WindAcceptanceRegression.cs','WindUIAcceptance.cs','WindCanvasStandard.cs','WindAcceptanceBuild.cs'])
 s=re.sub(r'^-out:.*$',f'-out:"{(tmp/name).as_posix()}.dll"',s,flags=re.M);s=re.sub(r'^-refout:.*$',f'-refout:"{(tmp/name).as_posix()}.ref.dll"',s,flags=re.M)
 if name.endswith('Editor'):s=s.replace('Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.ref.dll',(tmp/'Assembly-CSharp.ref.dll').as_posix())
 p=tmp/f'{name}.rsp';p.write_text(s,encoding='utf-8');r=subprocess.run(compiler+['@'+str(p)],capture_output=True,text=True)
 (out/f'{name}.Compiler.txt').write_text(r.stdout+r.stderr,encoding='utf-8');print(name,'exit',r.returncode);assert r.returncode==0

