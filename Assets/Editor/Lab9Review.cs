using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

[InitializeOnLoad]
public static class Lab9Review
{
    const string Request = "Temp/Lab9Review.request";
    const string Result = "Temp/Lab9Review.txt";
    static readonly List<float> Frames = new List<float>();
    static int stage;
    static int lastFrame;
    static int warmup;
    static double due;
    static XRGrabInteractable sample;
    static XRDirectInteractor hand;
    static Transform oldAttach;
    static Transform testAttach;
    static Vector3 oldPosition;
    static Quaternion oldRotation;
    static bool oldKinematic;
    static bool oldGravity;
    static bool oldThrow;
    static int selections;
    static int exits;
    static readonly List<string> Errors = new List<string>();

    static Lab9Review() { EditorApplication.update += Poll; Application.logMessageReceived += Log; }

    static void Log(string message,string trace,LogType type)
    {
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Errors.Add(type + " " + message);
    }

    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        string task = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try
        {
            switch(task)
            {
                case "audit": Audit(false); break;
                case "managers":
                    foreach(var manager in UnityEngine.Object.FindObjectsByType<XRInteractionManager>(FindObjectsInactive.Include))
                    {
                        string path = manager.name;
                        for(var parent=manager.transform.parent;parent!=null;parent=parent.parent) path=parent.name+"/"+path;
                        File.AppendAllText(Result,"MANAGER "+manager.gameObject.scene.name+"/"+path+" enabled="+manager.enabled+" active="+manager.gameObject.activeInHierarchy+"\n");
                    }
                    break;
                case "unify":
                    var scene=SceneManager.GetActiveScene();
                    var managers=UnityEngine.Object.FindObjectsByType<XRInteractionManager>(FindObjectsInactive.Include).Where(m=>m.gameObject.scene==scene).ToArray();
                    var main=managers.Single(m=>m.enabled && m.gameObject.activeInHierarchy);
                    foreach(var item in UnityEngine.Object.FindObjectsByType<XRBaseInteractable>(FindObjectsInactive.Include).Where(i=>i.gameObject.scene==scene)) item.interactionManager=main;
                    foreach(var interactor in UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include).Where(i=>i.gameObject.scene==scene)) interactor.interactionManager=main;
                    foreach(var unused in managers.Where(m=>m!=main)) UnityEngine.Object.DestroyImmediate(unused);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    File.AppendAllText(Result,"UNIFIED interaction manager references\n");
                    break;
                case "save":
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                    File.AppendAllText(Result,"SAVED\n");
                    break;
                case "reload":
                    EditorSceneManager.OpenScene("Assets/Scenes/Lab9_Lighting.unity");
                    Audit(false);
                    break;
                case "play": Errors.Clear(); EditorApplication.isPlaying = true; break;
                case "runtime": BeginRuntime(); break;
                case "stop": EditorApplication.isPlaying = false; break;
            }
        }
        catch(Exception error) { File.AppendAllText(Result,error.ToString()+"\n"); Debug.LogException(error); }
    }

    static void Audit(bool append)
    {
        var scene = SceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var root = all.First(t=>t.name == "09 • Architectural lighting gallery");
        var lights = all.Select(t=>t.GetComponent<Light>()).Where(l=>l != null && l.enabled && l.gameObject.activeInHierarchy).ToArray();
        var text = new StringBuilder();
        text.AppendLine("SCENE " + scene.path + " play="+Application.isPlaying+" dirty="+scene.isDirty);
        text.AppendLine("Sun="+RenderSettings.sun?.name+" ambient="+RenderSettings.ambientMode+" intensity="+RenderSettings.ambientIntensity);
        foreach(var light in lights)
            text.AppendLine("LIGHT "+light.name+" "+light.type+" "+light.lightmapBakeType+" intensity="+light.intensity+" range="+light.range+" angle="+light.spotAngle+" shadow="+light.shadows+" strength="+light.shadowStrength+" color="+light.color);
        var settings = new SerializedObject(Lightmapping.lightingSettings);
        foreach(string key in new[]{"m_EnableBakedLightmaps","m_EnableRealtimeLightmaps","m_BakeBackend","m_BakeResolution","m_LightmapMaxSize","m_LightmapCompression","m_MixedBakeMode","m_PVRBounces"})
        {
            var value = settings.FindProperty(key);
            text.AppendLine("SETTING "+key+"="+(value.propertyType == SerializedPropertyType.Boolean ? value.boolValue.ToString() : value.propertyType == SerializedPropertyType.Float ? value.floatValue.ToString() : value.intValue.ToString()));
        }
        text.AppendLine("BAKE maps="+LightmapSettings.lightmaps.Length+" probes="+LightmapSettings.lightProbes?.count+" data="+AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset));
        foreach(var data in LightmapSettings.lightmaps)
            text.AppendLine("MAP "+data.lightmapColor?.width+"x"+data.lightmapColor?.height+" directional="+(data.lightmapDir!=null)+" shadowmask="+(data.shadowMask!=null));
        text.AppendLine("PIPELINE "+QualitySettings.renderPipeline?.name);
        text.AppendLine("MISSING SCRIPTS "+all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
        var renderers = all.Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r!=null && r.gameObject.activeInHierarchy).ToArray();
        foreach(var renderer in renderers)
        {
            bool gi = GameObjectUtility.AreStaticEditorFlagsSet(renderer.gameObject,StaticEditorFlags.ContributeGI);
            bool dynamic = renderer.GetComponentInParent<Rigidbody>() != null;
            if(dynamic && gi) text.AppendLine("FAIL dynamic GI flag "+renderer.name);
            bool architecture = renderer.transform.IsChildOf(root) && !dynamic && renderer.GetComponentInParent<TMP_Text>() == null && renderer.GetComponentInParent<TextMesh>() == null;
            if(architecture && !gi) text.AppendLine("FAIL gallery static GI flag "+renderer.name);
            if(gi && renderer.lightmapIndex < LightmapSettings.lightmaps.Length && renderer.lightmapIndex>=0) continue;
            if(architecture) text.AppendLine("FAIL gallery missing lightmap "+renderer.name+" index="+renderer.lightmapIndex);
        }
        foreach(var group in all.Select(t=>t.GetComponent<LightProbeGroup>()).Where(g=>g!=null))
        {
            text.AppendLine("PROBES "+group.name+" count="+group.probePositions.Length+" origin="+group.transform.position);
            if(group.name.Contains("Room") && (group.probePositions.Length<15 || group.probePositions.Length>30)) text.AppendLine("FAIL room probe count");
        }
        foreach(var grab in root.GetComponentsInChildren<XRGrabInteractable>())
            text.AppendLine("SAMPLE "+grab.name+" manager="+grab.interactionManager?.name+" static="+grab.gameObject.isStatic+" probes="+grab.GetComponent<Renderer>().lightProbeUsage+" movement="+grab.movementType+" gravity="+grab.GetComponent<Rigidbody>().useGravity+" collider="+grab.GetComponents<Collider>().Length);
        foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Lab9"}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
            text.AppendLine("MAT "+material.name+" shader="+material.shader.name+" metallic="+material.GetFloat("_Metallic")+" smooth="+material.GetFloat("_Smoothness")+" normal="+(material.GetTexture("_BumpMap")!=null)+" normalKeyword="+material.IsKeywordEnabled("_NORMALMAP"));
        }
        if(append) File.AppendAllText(Result,text.ToString()); else File.WriteAllText(Result,text.ToString());
    }

    static Vector3 Zone(int index)
    {
        var root = GameObject.Find("09 • Architectural lighting gallery").transform;
        var point = index==0 ? new Vector3(-3,1.35f,3) : index==1 ? new Vector3(3,1.35f,3) : new Vector3(4.4f,1.25f,1.3f);
        return root.TransformPoint(point);
    }

    static void BeginRuntime()
    {
        if(!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        Audit(true);
        sample=GameObject.Find("Lab9 • Movable material samples").GetComponentsInChildren<XRGrabInteractable>().First(g=>g.name.Contains("Stone"));
        hand=UnityEngine.Object.FindObjectsByType<XRDirectInteractor>().First(h=>h.enabled && h.gameObject.activeInHierarchy);
        var body=sample.GetComponent<Rigidbody>();
        oldPosition=sample.transform.position;
        oldRotation=sample.transform.rotation;
        oldKinematic=body.isKinematic;
        oldGravity=body.useGravity;
        oldThrow=sample.throwOnDetach;
        oldAttach=hand.attachTransform;
        selections=exits=0;
        sample.selectEntered.AddListener(_=>selections++);
        sample.selectExited.AddListener(_=>exits++);
        body.isKinematic=true;
        body.useGravity=false;
        var collider=hand.GetComponent<Collider>();
        sample.transform.position=collider==null ? hand.transform.position : collider.bounds.center;
        Physics.SyncTransforms();
        stage=0;
        due=EditorApplication.timeSinceStartup+0.7;
        EditorApplication.update+=RuntimeStep;
    }

    static void RuntimeStep()
    {
        if(!Application.isPlaying) { EditorApplication.update-=RuntimeStep; return; }
        if(EditorApplication.timeSinceStartup<due) return;
        try
        {
            if(stage==0)
            {
                File.AppendAllText(Result,"DIRECT hovered="+sample.isHovered+" validLayers="+((sample.interactionLayers.value & hand.interactionLayers.value)!=0)+" hand="+hand.name+"\n");
                testAttach=new GameObject("Temporary grip target").transform;
                testAttach.SetPositionAndRotation(hand.GetAttachTransform(sample).position,hand.GetAttachTransform(sample).rotation);
                hand.attachTransform=testAttach;
                sample.throwOnDetach=false;
                hand.StartManualInteraction((IXRSelectInteractable)sample);
                stage=1;
            }
            else if(stage<=3)
            {
                if(stage>1) ReportZone(stage==2 ? "warm" : "cool");
                File.AppendAllText(Result,"GRIP selected="+sample.isSelected+" events="+selections+"\n");
                testAttach.position=Zone(stage-1);
                stage++;
            }
            else if(stage==4)
            {
                ReportZone("shade");
                File.AppendAllText(Result,"HELD position="+sample.transform.position+" target="+testAttach.position+" distance="+Vector3.Distance(sample.transform.position,testAttach.position)+"\n");
                hand.EndManualInteraction();
                hand.attachTransform=oldAttach;
                UnityEngine.Object.Destroy(testAttach.gameObject);
                var body=sample.GetComponent<Rigidbody>();
                body.isKinematic=true;
                sample.transform.SetPositionAndRotation(oldPosition,oldRotation);
                body.isKinematic=oldKinematic;
                body.useGravity=oldGravity;
                sample.throwOnDetach=oldThrow;
                File.AppendAllText(Result,"RELEASE selected="+sample.isSelected+" exitEvents="+exits+"\n");
                warmup=45;
                lastFrame=-1;
                Frames.Clear();
                stage=5;
            }
            else
            {
                if(lastFrame==Time.frameCount) return;
                lastFrame=Time.frameCount;
                if(warmup-->0) return;
                Frames.Add(Time.unscaledDeltaTime);
                if(Frames.Count<240) return;
                var sorted=Frames.OrderBy(f=>f).ToArray();
                File.AppendAllText(Result,"PERFORMANCE frames=240 averageFPS="+(1/Frames.Average()).ToString("F1")+" p95ms="+(sorted[228]*1000).ToString("F1")+"\nERRORS="+Errors.Count+"\n"+string.Join("\n",Errors));
                EditorApplication.update-=RuntimeStep;
                return;
            }
            due=EditorApplication.timeSinceStartup+0.6;
            if(stage==5) due=0;
        }
        catch(Exception error)
        {
            File.AppendAllText(Result,error.ToString()+"\n");
            EditorApplication.update-=RuntimeStep;
            Debug.LogException(error);
        }
    }

    static void ReportZone(string name)
    {
        LightProbes.GetInterpolatedProbe(sample.transform.position,sample.GetComponent<Renderer>(),out var sh);
        var colors=new Color[1];
        sh.Evaluate(new[]{Vector3.forward},colors);
        File.AppendAllText(Result,"ZONE "+name+" actual="+sample.transform.position+" probeRGB="+colors[0]+"\n");
    }
}
