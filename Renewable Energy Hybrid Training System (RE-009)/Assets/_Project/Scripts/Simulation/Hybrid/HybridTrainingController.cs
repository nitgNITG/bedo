using System;
using System.Linq;
using TMPro;
using UnityEngine;
using TutorialFramework.Simulation;

namespace TutorialFramework
{
    /// <summary>RE-009 orchestration. Source calculations remain in the existing readers.</summary>
    public sealed class HybridTrainingController : MonoBehaviour
    {
        [Serializable] public class Circuit
        {
            public WireInteraction[] guided;
            public FreeLabConnectionGroup free;
            public bool Connected(FreeLabManager lab, bool sandbox) => sandbox
                ? free && free.IsComplete(lab)
                : guided != null && guided.Length > 0 && guided.All(w => w && w.wireObject &&
                    w.wireObject.activeSelf && w.plugA && w.plugA.activeSelf && w.plugB && w.plugB.activeSelf);
        }
        [Serializable] public class Lamp
        {
            public bool led;
            public Circuit connection;
            public GameObject light;
        }
        [Serializable] public class Meter
        {
            public TMP_Text voltage, current, power;
            public void Set(float v,float a,float p)
            {
                if(voltage) voltage.text=v.ToString("F2");
                if(current) current.text=a.ToString("F2");
                if(power) power.text=p.ToString("F2");
            }
        }
        public StoryboardSteps storyboard;
        public WindTurbineSpeedController windSpeed;
        public TextAsset hybridModelParameters;
        private HybridMath.Parameters parameters;
        private bool parametersLoaded;
        public FreeLabWindController freeController;
        public FreeLabControl solarEmergency,solarPower,solarRun,windRun,solarEnable;
        public Renderer[] freeModeHiddenHardware=Array.Empty<Renderer>();
        public Circuit mainWiring, batteryWiring, inverterWiring, windWiring, solarWiring;
        public Lamp[] lamps = Array.Empty<Lamp>();
        public Meter windAC,windDC,solar,battery,acLoad,dump,inverter;
        public Meter controllerOutput;
        public Meter utilityGrid;
        public TMP_Text irradianceText;
        public TMP_Text rpmText;
        public GameObject monitor,blackScreen;
        public GameObject powerAnalyzerCanvas;
        public TMP_Text powerAnalyzerText;
        public PVGraphController graph;
        [Tooltip("Enable only when this storyboard explicitly includes plotting measurements.")] public bool recordGraphMeasurements;
        [Range(0f,1f)] public float irradiance=1f;
        public float tilt,azimuth,sun;
        [SerializeField, Range(0,1)] private float batteryCharge=0f;
        private readonly bool[] instructionStates=new bool[26];
        private HybridMath.Reading reading;
        public HybridMath.Reading Reading => reading;
        public bool MainPowered {get; private set;}
        public bool SolarEnabled=>sandbox?On(solarEnable):instructionStates[17];
        public void ResetExperiment(){batteryCharge=0f;Array.Clear(instructionStates,0,instructionStates.Length);previousRPM=-1;previousSolarEnabled=false;previousSolarSettings=new Vector4(-1,-1,-1,-1);MainPowered=false;reading=default;if(windSpeed){windSpeed.TurnOff();windSpeed.SetSpeed(0);}if(graph)graph.ClearRuntimeGraph();}

        private bool sandbox => InteractionManager.Instance && InteractionManager.Instance.Free;
        private float previousRPM=-1;
        private Vector4 previousSolarSettings;
        private bool previousSolarEnabled;

        // Only authored instruction-completion callbacks change guided power state.
        public void CompleteInstruction(int number)
        {
            if(number>0&&number<instructionStates.Length)instructionStates[number]=true;
            if(number==20&&windSpeed&&windSpeed.CurrentSpeed<=0)windSpeed.SetSpeed(600);
            Refresh();
        }
        public void ToggleSolarEnable()
        {
            if(sandbox){if(solarEnable)solarEnable.Toggle();Refresh();return;}
            instructionStates[17]=!instructionStates[17];
            Refresh();
        }
        private void OnDisable(){if(powerAnalyzerCanvas)powerAnalyzerCanvas.SetActive(false);}
        private bool On(FreeLabControl control)=>control&&control.Value;
        private void LateUpdate()
        {
            if(!sandbox&&windSpeed)windSpeed.TickRotor();
            if(sandbox&&freeController&&freeController.IsReady&&freeController.rotor&&MainPowered&&On(freeController.windPower))
                freeController.rotor.Rotate(Vector3.right,freeController.RPM*6f*freeController.rotorVisualMultiplier*Time.deltaTime,Space.Self);
            // The Run selector owns visibility, generation owns the wind-only values.
            bool selected=sandbox?MainPowered&&freeController&&On(freeController.windPower)&&On(windRun):instructionStates[21];
            if(powerAnalyzerCanvas)powerAnalyzerCanvas.SetActive(selected);
            if(powerAnalyzerText)powerAnalyzerText.text=PVGuidedDataBinder.FormatPowerAnalyzer(reading.wind.acPower,Mathf.Sqrt(3f)*Mathf.Abs(reading.wind.acVoltage*reading.wind.acCurrent));
        }
        private void Update()
        {
            if(sandbox&&(!freeController||!freeController.IsReady))return;
            Refresh();
            if(reading.busVoltage>0&&parameters!=null)
                batteryCharge=Mathf.Clamp01(batteryCharge+reading.batteryCurrent*Time.deltaTime*(reading.batteryCurrent>0?(float)parameters.eta_cou:1f)/((float)parameters.C_Ah*3600f));
        }
        public void Refresh()
        {
            if(sandbox)foreach(var cover in freeModeHiddenHardware)if(cover)cover.enabled=false;
            var lab=sandbox&&freeController?freeController.lab:null;
            bool main=sandbox?freeController&&On(freeController.mainEmergency)&&On(freeController.mainBreaker)&&On(freeController.mainPower)
                :instructionStates[10]&&instructionStates[11]&&instructionStates[12];
            main &= mainWiring!=null&&mainWiring.Connected(lab,sandbox);
            MainPowered=main;
            bool bus=main&&batteryWiring!=null&&batteryWiring.Connected(lab,sandbox);
            bool inv=bus&&inverterWiring!=null&&inverterWiring.Connected(lab,sandbox);
            bool pv=bus&&(sandbox?On(solarEmergency)&&On(solarPower)&&On(solarRun)&&On(solarEnable)
                :instructionStates[13]&&instructionStates[14]&&instructionStates[15]&&instructionStates[17])&&solarWiring!=null&&solarWiring.Connected(lab,sandbox);
            bool wind=bus&&(sandbox?On(freeController.windEmergency)&&On(freeController.windBreaker)&&On(freeController.windPower)&&On(windRun)
                :instructionStates[18]&&instructionStates[19]&&instructionStates[20]&&instructionStates[21])&&windWiring!=null&&windWiring.Connected(lab,sandbox);
            int halogen=0,led=0;
            foreach(var lamp in lamps)
            {
                bool lit=inv&&lamp.connection!=null&&lamp.connection.Connected(lab,sandbox)&&batteryCharge>0;
                if(lamp.light)lamp.light.SetActive(lit);
                if(lit){if(lamp.led)led++;else halogen++;}
            }
            float rpm=sandbox&&freeController?freeController.RPM:windSpeed?windSpeed.CurrentSpeed:0;
            if(!parametersLoaded)
            {
                parametersLoaded=true;
                try { parameters=HybridMath.ReadParameters(hybridModelParameters); }
                catch(System.Exception error) { Debug.LogError("Hybrid model parameters could not be loaded: "+error.Message,this); }
            }
            reading=parameters!=null?HybridMath.Calculate(parameters,bus,wind,pv,rpm,irradiance,tilt,azimuth,sun,halogen,led,inv,batteryCharge):default;
            if(powerAnalyzerText)powerAnalyzerText.text=PVGuidedDataBinder.FormatPowerAnalyzer(reading.wind.acPower,Mathf.Sqrt(3f)*Mathf.Abs(reading.wind.acVoltage*reading.wind.acCurrent));
            if(windAC!=null)windAC.Set(reading.wind.acVoltage,reading.wind.acCurrent,reading.wind.acPower);
            if(windDC!=null)windDC.Set(reading.wind.dcVoltage,reading.wind.dcCurrent,reading.wind.dcPower);
            if(solar!=null)solar.Set(reading.pvVoltage,reading.pvCurrent,reading.pvPower);
            if(battery!=null)battery.Set(reading.busVoltage,reading.batteryCurrent,reading.batteryPower);
            if(acLoad!=null)acLoad.Set(reading.acVoltage,reading.acCurrent,reading.acPower);
            if(dump!=null)dump.Set(reading.dumpCurrent>0?reading.busVoltage:0,reading.dumpCurrent,reading.dumpPower);
            if(inverter!=null)inverter.Set(inv?reading.busVoltage:0,reading.inverterCurrent,reading.inverterPower);
            if(controllerOutput!=null)controllerOutput.Set(reading.busVoltage,reading.controllerCurrent,reading.controllerPower);
            // No utility-grid branch is connected in this off-grid storyboard.
            if(utilityGrid!=null)utilityGrid.Set(0,0,0);
            if(irradianceText)irradianceText.text=(irradiance*100f).ToString("0");
            if(rpmText)rpmText.text=reading.wind.rpm.ToString("0");
            if(blackScreen)blackScreen.SetActive(!main);
            if(!sandbox&&windSpeed)
            {
                // Power-on spins the turbine; Run controls electrical generation.
                bool running=main&&instructionStates[20];
                if(running&&!windSpeed.IsTurbineRunning)windSpeed.TurnOn();
                else if(!running&&windSpeed.IsTurbineRunning)windSpeed.TurnOff();
            }
            var solarSettings=new Vector4(irradiance,tilt,azimuth,sun);
            if(recordGraphMeasurements&&graph&&reading.wind.rpm>0&&(!Mathf.Approximately(previousRPM,reading.wind.rpm)||previousSolarSettings!=solarSettings||previousSolarEnabled!=SolarEnabled))
            {
                previousRPM=reading.wind.rpm;previousSolarSettings=solarSettings;previousSolarEnabled=SolarEnabled;
                graph.UpdateWindMeasurement();
            }
        }
        public bool TryGetWindReading(out PVCSVReader.WindReading value){value=reading.wind;return isActiveAndEnabled;}
        public bool TryGetGraphReading(out PVCSVReader.WindReading value)
        {
            value=reading.wind;
            value.dcVoltage=reading.busVoltage;value.dcCurrent=reading.controllerCurrent;value.dcPower=reading.controllerPower;
            return isActiveAndEnabled;
        }
    }
}
