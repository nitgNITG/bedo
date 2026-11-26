using System;
using UnityEngine;
namespace TutorialFramework.Simulation
{
 // Retained entry point; calculations now follow Energy Hybrid_Mathematical model.xlsx.
 public static class HybridMath
 {
  [Serializable] public sealed class Parameters
  {
   public double Voc_p,Pmax_p,Isc_p,Vk_p,N_ser,k_I,G_ref,eta_pv;
   public double ke,rpm_min,rpm_max,Vf,Ra,Rb,Pw_max,k_Iac,Z_ac;
   public double C_Ah,V_nom,Voc0,Voc_slope,R_dis,R_ch,dV_ch,I_0,I_ch_max,SOC_abs,V_reg,eta_cou,SOC_cut;
   public double eta_inv,P_idle,V_ac,P_halo,P_led,P_dump_max,dt_s;
  }
  public struct Reading
  {
   public PVCSVReader.WindReading wind;
   public float pvVoltage,pvCurrent,pvPower,busVoltage,sourcePower,loadPower;
   public float acVoltage,acCurrent,acPower,batteryCurrent,batteryPower,dumpCurrent,dumpPower;
   public float controllerCurrent,controllerPower,inverterCurrent,inverterPower,balanceResidual;
  }
  public static Parameters ReadParameters(TextAsset asset)
  {
   if(!asset)throw new ArgumentNullException(nameof(asset));
   var p=JsonUtility.FromJson<Parameters>(asset.text);
   if(p==null||p.C_Ah<=0||p.eta_inv<=0||p.V_ac<=0||p.Vk_p<=0||p.N_ser<=0||p.R_ch<=0||p.I_0<=0)throw new ArgumentException("Incomplete hybrid workbook parameters.");
   return p;
  }
  public static Reading Calculate(Parameters p,bool powered,bool windEnabled,bool solarEnabled,float rpmInput,float dimming,float tilt,float azimuth,float sun,int halogen,int led,bool inverterOn,float charge,float dcLoad=0)
  {
   if(!powered)return default;
   double soc=Math.Max(0,Math.Min(1,charge)),rpm=windEnabled?Math.Max(0,rpmInput):0;
   double g=solarEnabled?p.G_ref*Math.Max(0,dimming):0;
   double gt=Math.Max(0,g*Math.Cos((tilt-azimuth)*Math.PI/180)*Math.Cos(sun*Math.PI/180));
   double isc=p.Isc_p*p.k_I*gt/p.G_ref;
   double voc=gt>1?Math.Max(0,p.N_ser*(p.Voc_p+p.Vk_p*Math.Log(gt/p.G_ref))):0;
   double rectified=rpm>0?Math.Max(0,1.35*p.ke*rpm-2*p.Vf):0,req=p.Ra+p.Rb*rpm;
   bool inv=inverterOn&&soc>p.SOC_cut;
   double pac=inv?Math.Max(0,halogen)*p.P_halo+Math.Max(0,led)*p.P_led:0;
   double pinv=inv?pac/p.eta_inv+p.P_idle:0,pdc=Math.Max(0,dcLoad),vocBattery=p.Voc0+p.Voc_slope*soc;
   double taper=soc<p.SOC_abs?p.I_ch_max:p.I_ch_max*Math.Max(0,1-soc)/(1-p.SOC_abs);
   double acceptance=Math.Min(taper,Math.Max(0,(p.V_reg-vocBattery-p.dV_ch)/p.R_ch));
   double v=vocBattery,iw=0,ipv=0,net=0,ib=0;
   // Exactly Model!BA:BU: four current passes followed by the pass-five voltage.
   for(int pass=0;pass<4;pass++)
   {
    iw=rpm>0?Math.Min(Math.Max(0,(rectified-v)/req),p.Pw_max/v):0;
    ipv=isc>0?Math.Max(0,isc*(1-Math.Exp((v-voc)/(p.N_ser*p.Vk_p)))):0;
    net=iw+p.eta_pv*ipv-(pinv+pdc)/v;
    ib=net>=0?Math.Min(net,acceptance):net;
    v=Math.Min(p.V_reg,vocBattery+(ib>=0?p.dV_ch*(1-Math.Exp(-ib/p.I_0))+p.R_ch*ib:p.R_dis*ib));
   }
   double dump=Math.Max(0,net-ib),controller=iw+p.eta_pv*ipv-dump;
   double acCurrent=p.k_Iac*iw,phase=rpm>0?Math.Max(0,p.ke*rpm-Math.Sqrt(3)*p.Z_ac*acCurrent):0;
   var r=new Reading{
    pvVoltage=(float)(gt>1?Math.Min(voc,v):0),pvCurrent=(float)ipv,busVoltage=(float)v,sourcePower=(float)(v*(iw+p.eta_pv*ipv)),
    loadPower=(float)pac,acPower=(float)pac,acVoltage=(float)(inv?p.V_ac:0),acCurrent=(float)(pac/p.V_ac),
    batteryCurrent=(float)ib,batteryPower=(float)(v*ib),dumpCurrent=(float)dump,dumpPower=(float)Math.Min(v*dump,p.P_dump_max),
    controllerCurrent=(float)controller,controllerPower=(float)(v*controller),inverterCurrent=(float)(pinv/v),inverterPower=(float)pinv,
    balanceResidual=(float)(v*(controller-pdc/v-pinv/v-ib)),
    wind=new PVCSVReader.WindReading{rpm=(float)rpm,resistance=(float)req,acVoltage=(float)phase,acCurrent=(float)acCurrent,acPower=(float)(Math.Sqrt(3)*phase*acCurrent),dcVoltage=(float)v,dcCurrent=(float)iw,dcPower=(float)(v*iw)}
   };
   r.pvPower=r.pvVoltage*r.pvCurrent;return r;
  }
 }
}
