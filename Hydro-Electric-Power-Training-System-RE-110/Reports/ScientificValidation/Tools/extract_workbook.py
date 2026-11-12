from pathlib import Path
import zipfile,xml.etree.ElementTree as E,json,hashlib
p=Path(r'C:/Users/pc/Downloads/Renewable_Batch 2/Wind Energy Training System (RE-007)/Designing/ID/Phase 1/Wind turbine_Mathematical model .xlsx')
n={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
with zipfile.ZipFile(p) as z:
 strings=[]
 if 'xl/sharedStrings.xml' in z.namelist():
  strings=[''.join(e.itertext()) for e in E.fromstring(z.read('xl/sharedStrings.xml')).findall('s:si',n)]
 out={'source':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'sheets':{}}
 for name in z.namelist():
  if name.startswith('xl/worksheets/sheet') and name.endswith('.xml'):
   cells={}
   for c in E.fromstring(z.read(name)).findall('.//s:sheetData/s:row/s:c',n):
    v=c.find('s:v',n);f=c.find('s:f',n); val=v.text if v is not None else None
    if c.get('t')=='s' and val is not None:val=strings[int(val)]
    if val is not None or f is not None:cells[c.get('r')]={'value':val,'formula':f.text if f is not None else None}
   out['sheets'][name]=cells
 Path('Reports/ScientificValidation/WorkbookCells.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
 for name,cells in out['sheets'].items():
  print(name,len(cells));print(list(cells.items())[:25])
