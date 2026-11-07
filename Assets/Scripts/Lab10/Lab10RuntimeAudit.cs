#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class Lab10RuntimeAudit : MonoBehaviour
{
    [MenuItem("Tools/Lab 10/Run audio verification %&F10")]
    static void Run()
    {
        SessionState.SetBool("Lab10Audit",true);
        EditorApplication.isPlaying = true;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Started()
    {
        if (!SessionState.GetBool("Lab10Audit",false)) return;
        SessionState.SetBool("Lab10Audit",false);
        new GameObject("Temporary Lab10 audio verification").AddComponent<Lab10RuntimeAudit>();
    }
    IEnumerator Start()
    {
        string report="Runtime audio audit\n";
        yield return new WaitForSeconds(2);
        var listener=FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Single(x=>x.enabled&&x.gameObject.activeInHierarchy);
        var sources=FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(x=>x.clip!=null).ToArray();
        report+="Listener count=1; head="+listener.name+"\n";
        foreach(var source in sources) report+=source.name+" playing="+source.isPlaying+" load="+source.clip.loadState+"\n";
        var points=FindObjectsByType<Lab10PointAudio>(FindObjectsSortMode.None);
        var ambient=sources.First(x=>x.spatialBlend==0);
        bool ambientMute=ambient.mute;ambient.mute=true;
        foreach(var point in points)
        {
            var source=point.GetComponent<AudioSource>();
            Vector3 saved=source.transform.position;
            foreach(var other in points) other.GetComponent<AudioSource>().mute=other!=point;
            foreach(float distance in new[]{2f,8f,source.maxDistance+1})
            {
                source.transform.position=listener.transform.position+listener.transform.forward*distance;
                source.Play();yield return new WaitForSeconds(.2f);
                report+=source.name+" distance="+distance+" volume="+source.volume+"\n";
            }
            foreach(float side in new[]{-1f,1f})
            {
                source.transform.position=listener.transform.position+listener.transform.right*side*2f;
                source.Play();yield return new WaitForSeconds(.15f);
                float left=0,right=0;var data=new float[1024];
                for(int j=0;j<8;j++)
                {
                    AudioListener.GetOutputData(data,0);left+=data.Sum(x=>x*x);
                    AudioListener.GetOutputData(data,1);right+=data.Sum(x=>x*x);
                    yield return null;
                }
                report+="side="+side+" channel energy L="+left+" R="+right+"\n";
            }
            source.transform.position=saved;
        }
        ambient.mute=ambientMute;
        foreach(var point in points)point.GetComponent<AudioSource>().mute=false;
        report+="Finished; temporary source positions and mutes restored.\n";
        File.WriteAllText("Temp/Lab10RuntimeAudit.txt",report);Debug.Log(report);
        Destroy(gameObject);
    }
}
#endif
