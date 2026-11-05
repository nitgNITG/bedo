using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using M=TutorialFramework.Simulation.HybridMath;
public static class HybridModelVerification
{
 [Serializable] public class Case {public string name;public float[] inputs,expected;}
 [Serializable] public class Cases {public Case[] cases;}
 public static void Verify(){
 var p=M.ReadParameters(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Hybrid/HybridModelParameters.json"));
 var cases=JsonUtility.FromJson<Cases>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Hybrid/HybridModelValidation.json").text).cases;int checks=0;float worst=0;
 foreach(var c in cases){var x=c.inputs;var r=M.Calculate(p,true,x[5]>0,x[0]>0,x[6]*1000,x[1],x[2],x[3],x[4],(int)x[7],(int)x[8],x[9]>0,x[11],x[10]);
 var actual=new[]{r.pvVoltage,r.pvCurrent,r.pvPower,r.wind.rpm,r.wind.acVoltage,r.wind.acCurrent,r.wind.acPower,r.controllerCurrent,r.busVoltage,r.controllerPower,r.busVoltage,r.batteryCurrent,r.batteryPower,0,r.inverterCurrent,r.inverterPower,r.acVoltage,r.acCurrent,r.acPower,0,r.dumpCurrent,r.dumpPower,0,r.balanceResidual};
 for(int i=0;i<actual.Length;i++){if(i==13||i==19||i==22)continue;float error=Mathf.Abs(actual[i]-c.expected[i]);worst=Mathf.Max(worst,error);if(error>.003f)throw new Exception(c.name+" output "+i+": expected "+c.expected[i]+", actual "+actual[i]);checks++;}}
 var off=M.Calculate(p,false,true,true,1000,1,0,0,-40,2,2,true,.5f);if(off.busVoltage!=0||off.pvPower!=0||off.acPower!=0||off.wind.acVoltage!=0)throw new Exception("Unpowered outputs must be zero.");
 Directory.CreateDirectory("Logs/HybridModelReview");File.WriteAllText("Logs/HybridModelReview/workbook-verification.txt","PASS: "+cases.Length+" cached workbook operating points, "+checks+" numerical output comparisons; max absolute error="+worst+"; unpowered outputs zero. Workbook assumptions retained.");
 Debug.Log("Hybrid supplied workbook verification passed.");
 }
}
