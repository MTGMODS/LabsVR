using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class Lab11FinalAudit
{
    [MenuItem("Tools/Lab 11/Fix pavilion entrance collider %&F8")]
    public static void FixEntrance()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Зупиніть Play Mode.");
        var floor=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Single(t=>t.name=="Площа скульптури");
        var capsule=floor.GetComponent<CapsuleCollider>();
        if(capsule!=null)
        {
            var material=capsule.sharedMaterial;
            Object.DestroyImmediate(capsule);
            var mesh=floor.gameObject.AddComponent<MeshCollider>();
            mesh.sharedMesh=floor.GetComponent<MeshFilter>().sharedMesh;
            mesh.convex=false;mesh.sharedMaterial=material;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(floor.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(floor.gameObject.scene);
        Physics.SyncTransforms();Audit();TestWalking();
    }
    [MenuItem("Tools/Lab 11/Audit cumulative scene %&F9")]
    public static void Audit()
    {
        Physics.SyncTransforms();
        var all=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        string[] types={"XRGrabInteractable","XRSocketInteractor","XRRayInteractor","TeleportationArea","TeleportationProvider","ContinuousMoveProvider","DynamicMoveProvider","SnapTurnProvider","TunnelingVignetteController","LocomotionMediator","XRBodyTransformer","Lab7RayFeedback","Lab10PointAudio","Lab11Interface"};
        string report="Cumulative scene audit\\n";
        foreach(var type in types)report+=type+"="+all.Count(x=>x!=null&&x.GetType().Name==type)+"\\n";
        foreach(var cc in Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include))report+="Controller "+cc.name+" height="+cc.height+" radius="+cc.radius+" step="+cc.stepOffset+" skin="+cc.skinWidth+" center="+cc.center+"\\n";
        foreach(float x in new[]{-3f,3f})foreach(float z in new[]{12f,14f,16f,17f,18f,19f,20f})
        {
            var hits=Physics.OverlapCapsule(new Vector3(x,.45f,z),new Vector3(x,1.5f,z),.25f,~0,QueryTriggerInteraction.Ignore);
            report+="Route x="+x+" z="+z+": "+string.Join(" | ",hits.Where(c=>c.bounds.max.y>.5f).Select(c=>Hierarchy(c.transform)+" bounds="+c.bounds))+"\\n";
        }
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))if(light.enabled)report+="Light "+light.name+" "+light.type+" mode="+light.lightmapBakeType+"\\n";
        foreach(var p in Object.FindObjectsByType<LightProbeGroup>(FindObjectsInactive.Include))report+="Probes "+p.name+" count="+p.probePositions.Length+"\\n";
        foreach(var a in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include))if(a.clip!=null)report+="Audio "+a.name+" blend="+a.spatialBlend+" clip="+a.clip.name+"\\n";
        File.WriteAllText("Temp/Lab11FinalAudit.txt",report.Replace("\\n","\n"));Debug.Log("Final scene audit saved.");
    }
    static void TestWalking()
    {
        var original=Object.FindAnyObjectByType<CharacterController>();
        string report="CharacterController entrance tests\n";
        foreach(float x in new[]{-3f,3f})
        {
            var go=new GameObject("Temporary entrance test");go.hideFlags=HideFlags.HideAndDontSave;
            go.transform.position=new Vector3(x,.25f,14f);
            var cc=go.AddComponent<CharacterController>();
            cc.height=original.height;cc.radius=original.radius;cc.center=new Vector3(0,original.center.y,0);
            cc.skinWidth=original.skinWidth;cc.stepOffset=original.stepOffset;cc.slopeLimit=original.slopeLimit;
            Physics.SyncTransforms();
            for(int i=0;i<110;i++)cc.Move(new Vector3(0,-.02f,.05f));
            bool passed=go.transform.position.z>18.7f;
            report+="Entrance x="+x+" final="+go.transform.position+" "+(passed?"PASS":"FAIL")+"\n";
            Object.DestroyImmediate(go);
        }
        File.WriteAllText("Temp/Lab11EntranceWalk.txt",report);
        Debug.Log(report);
    }
    static string Hierarchy(Transform t){return t.parent==null?t.name:Hierarchy(t.parent)+"/"+t.name;}
}
