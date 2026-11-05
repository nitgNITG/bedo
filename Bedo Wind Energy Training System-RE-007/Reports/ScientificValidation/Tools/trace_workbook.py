from pathlib import Path
import csv,json,hashlib
r=Path('Reports/ScientificValidation'); c=json.loads((r/'WorkbookCells.json').read_text())['sheets']['xl/worksheets/sheet1.xml']; checks=[]
columns=['Resistance_Ohm','C1','C2','Turns','RPM','AC_Voltage_V','AC_Current_A','AC_Power_W','DC_Voltage_V','DC_Current_A','DC_Power_W','DC_Load_Current_A','DC_LED_Current_A','PWM_Battery_Current_A','PWM_Dump_Current_A','AC_Lamp_Display_Current_A','AC_Lamp_DC_Equivalent_A','AC_Lamp_Voltage_V','Available_Wind_Charge_A','Battery_Charge_Input_A','Battery_Input_Balance_A','Grid_Charge_Current_A','Grid_DC_With_Wind_A','Grid_AC_With_Wind_A','DC_No_Wind_A','AC_No_Wind_A']
for name,start,count in [('WindExp1Data',3,11),('WindExp2Data',70,15),('WindExp4Data',70,26)]:
 rows=list(csv.DictReader(Path(f'Assets/_Project/Data/Wind/{name}.bytes').read_text().splitlines()))
 for i,row in enumerate(rows):
  for j in range(count):
   col=chr(65+j);key=columns[j]
   if name=='WindExp2Data':key={'DC_Load_Current_A':'DC_Halogen_Current_A','PWM_Battery_Current_A':'Battery_Current_A','PWM_Dump_Current_A':'Dump_Current_A'}.get(key,key)
   cell=f'{col}{start+i}';expected=float(c[cell]['value']);actual=float(row[key]);ok=abs(actual-expected)<=max(1e-10,abs(expected)*1e-12)
   checks.append({'asset':name,'cell':cell,'column':key,'expected':expected,'actual':actual,'status':'PASS' if ok else 'FAIL'})
for i,row in enumerate(csv.DictReader(Path('Assets/_Project/Data/Wind/WindExp1Graph.bytes').read_text().splitlines())):
 for col,key in [('O','RPM'),('P','Voltage_V')]:
  expected=float(c[f'{col}{i+3}']['value']);actual=float(row[key]);checks.append({'asset':'WindExp1Graph','cell':f'{col}{i+3}','column':key,'expected':expected,'actual':actual,'status':'PASS' if expected==actual else 'FAIL'})
with (r/'WorkbookTrace.csv').open('w',newline='',encoding='utf-8') as f:
 w=csv.DictWriter(f,fieldnames=checks[0].keys());w.writeheader();w.writerows(checks)
assert all(x['status']=='PASS' for x in checks)
print(f'{len(checks)} workbook-to-project values match')
# Record project assets now so later changes cannot silently inherit this validation.
paths=[*Path('Assets/_Project/Data/Wind').glob('*.bytes'),Path('Assets/_Project/Scripts/UI/Graph/PVCSVReader.cs'),Path('Assets/_Project/Scripts/UI/Graph/PVGuidedDataBinder.cs'),Path('Assets/_Project/Scripts/Framework/FreeMode/FreeLabWindController.cs'),Path('Assets/_Project/Scripts/PVGraphController.cs')]
(r/'SourceHashes.json').write_text(json.dumps({p.as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in paths},indent=2))
