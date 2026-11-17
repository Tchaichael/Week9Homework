using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SeesawPlayCheck
{
    static int stage;
    static double next;
    static ImpactSeesaw seesaw;
    static Rigidbody cat;
    static Vector3 pivot, leftPoint;
    static Quaternion rotation;
    static SeesawPlayCheck() { EditorApplication.update += Update; }
    static void Update()
    {
        const string request="Temp/SeesawPlayCheck.request";
        if (!File.Exists(request) || EditorApplication.isCompiling) return;
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path!="Assets/Scenes/3.unity") return;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorApplication.isPlaying=true;
            return;
        }
        if (EditorApplication.isPaused || EditorApplication.timeSinceStartup<next) return;
        try
        {
            if (stage==0)
            {
                seesaw=UnityEngine.Object.FindAnyObjectByType<ImpactSeesaw>();
                if (!seesaw) return;
                cat=UnityEngine.Object.FindAnyObjectByType<BallForces>().GetComponent<Rigidbody>();
                foreach(var b in cat.GetComponents<MonoBehaviour>()) b.enabled=false;
                cat.isKinematic=false; cat.useGravity=false; cat.linearVelocity=Vector3.zero; cat.angularVelocity=Vector3.zero;
                cat.constraints=RigidbodyConstraints.FreezeRotation|RigidbodyConstraints.FreezePositionZ;
                pivot=seesaw.transform.position; rotation=seesaw.transform.rotation;
                var plate=seesaw.GetComponentsInChildren<BoxCollider>().OrderBy(c=>c.bounds.center.x).First();
                leftPoint=seesaw.transform.InverseTransformPoint(plate.bounds.center);
                float radius=cat.GetComponents<Collider>().Where(c=>c.enabled).Max(c=>c.bounds.extents.y);
                cat.position=new Vector3(plate.bounds.center.x,plate.bounds.max.y+radius+.1f,cat.position.z);
                Physics.SyncTransforms();
                stage=1; next=EditorApplication.timeSinceStartup+.15;
                return;
            }
            if(stage==1)
            {
                Require(!seesaw.HasImpacted && Quaternion.Angle(rotation,seesaw.transform.rotation)<.1f,"stationary before Prince drop");
                Require(seesaw.dropTrigger.TryActivate(cat.GetComponent<Collider>()),"cat activates original trap");
                stage=2; next=EditorApplication.timeSinceStartup+.8;
                return;
            }
            Require(seesaw.HasImpacted,"Prince drop contacts right side");
            Require(Vector3.Distance(pivot,seesaw.transform.position)<.01f,"fixed central pivot");
            Require(Quaternion.Angle(rotation,seesaw.transform.rotation)>35f,"board rotates");
            Require(seesaw.transform.TransformPoint(leftPoint).y>(pivot+rotation*leftPoint).y+5f,"left side rises");
            Require(seesaw.LaunchCount==1 && cat.linearVelocity.y>50f,"cat is launched upward once");
            Finish("PASS: original scene 3 trap drops Prince (1), right-side impact rotates Mon (2) around fixed center; left side rises and cat launches once upward. Angle="+Quaternion.Angle(rotation,seesaw.transform.rotation)+"; cat velocity="+cat.linearVelocity);
        }
        catch(Exception e){Finish("FAIL: "+e);}
    }
    static void Require(bool b,string s){if(!b)throw new Exception(s);}
    static void Finish(string report)
    {
        File.WriteAllText("Temp/SeesawPlayCheckReport.txt",report);
        File.Delete("Temp/SeesawPlayCheck.request");
        EditorApplication.isPlaying=false;stage=0;
    }
}
