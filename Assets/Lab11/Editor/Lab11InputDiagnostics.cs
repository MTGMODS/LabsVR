#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
[InitializeOnLoad]
public static class Lab11InputDiagnostics
{
    static double next;
    [MenuItem("Tools/Lab 11/Test button flow %&F11")]
    static void RunFlow(){SessionState.SetInt("Lab11Flow",1);SessionState.SetString("Lab11FlowReport","");EditorApplication.isPlaying=true;}
    static void Flow()
    {
        int step=SessionState.GetInt("Lab11Flow",0);
        if(step==0)return;
        var c=Object.FindAnyObjectByType<Lab11Interface>();if(c==null)return;
        var canvases=c.GetComponentsInChildren<Canvas>(true);
        var main=canvases.Single(x=>x.name.StartsWith("01 •"));
        var info=canvases.Single(x=>x.name.StartsWith("02 •"));
        var wall=canvases.Single(x=>x.name.StartsWith("03 •"));
        var buttons=c.GetComponentsInChildren<Button>(true);
        System.Action<string> click=name=>buttons.Single(b=>b.name=="Кнопка • "+name).onClick.Invoke();
        bool okay=true;
        switch(step)
        {
            case 1:click("Інфо");okay=info.gameObject.activeSelf&&!main.gameObject.activeSelf;break;
            case 2:click("Закрити");okay=!info.gameObject.activeSelf&&main.gameObject.activeSelf;break;
            case 3:click("Почати");okay=!info.gameObject.activeSelf&&!main.gameObject.activeSelf;break;
            case 4:wall.GetComponentInChildren<Button>(true).onClick.Invoke();okay=info.gameObject.activeSelf&&!main.gameObject.activeSelf;break;
            case 5:click("Закрити");okay=!info.gameObject.activeSelf&&!main.gameObject.activeSelf;break;
            case 6:c.ShowProjectInfo();click("Закрити");okay=main.gameObject.activeSelf;break;
        }
        string report=SessionState.GetString("Lab11FlowReport","")+"Button flow step "+step+": "+(okay?"PASS":"FAIL")+"\n";
        SessionState.SetString("Lab11FlowReport",report);File.WriteAllText("Temp/Lab11ButtonFlow.txt",report);
        SessionState.SetInt("Lab11Flow",okay&&step<6?step+1:0);
        if(!okay)Debug.LogError("Lab11 button flow failed at "+step);
        if(step==6)Debug.Log("Lab11: all button flow checks passed.");
    }
    static Lab11InputDiagnostics(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<next || SessionState.GetInt("Lab11Flow",0)==0)return;
        next=EditorApplication.timeSinceStartup+1;
        Flow();
        string text="";
        foreach(var r in Object.FindObjectsByType<XRRayInteractor>())
        {
            var a=r.uiPressInput.inputActionPerformed;
            text+=r.name+" parent="+r.transform.parent?.name+" enabled="+r.enabled+" ui="+r.enableUIInteraction+" press="+r.uiPressInput.ReadIsPerformed()+" select="+r.selectInput.ReadIsPerformed()+" hit="+(r.TryGetCurrentUIRaycastResult(out var hit)?hit.gameObject.name:"none")+"\n";
            if(a!=null)text+="action="+a.name+" enabled="+a.enabled+" phase="+a.phase+" controls="+string.Join(",",a.controls.Select(c=>c.path))+"\n";
        }
        File.WriteAllText("Temp/Lab11InputState.txt",text);
    }
}
#endif
