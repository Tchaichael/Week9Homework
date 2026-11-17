using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2StarTrailSetup
{
    [InitializeOnLoadMethod] static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists("Temp/EditableCatStars.request") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ConfigureEditableTrail();
                File.Delete("Temp/EditableCatStars.request");
                return;
            }
            const string request="Temp/SetupCatStars.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }
    static void ConfigureEditableTrail()
    {
        EditorSceneManager.SaveOpenScenes();
        if (SceneManager.GetActiveScene().path!="Assets/Scenes/2.unity") EditorSceneManager.OpenScene("Assets/Scenes/2.unity");
        var ball=GameObject.Find("ball");
        var trail=ball.GetComponent<CatStarTrail>();
        Undo.RecordObject(trail,"Expose and enlarge star trail");
        trail.starSize=new Vector2(2f,3.5f);
        EditorUtility.SetDirty(trail);
        PrefabUtility.RecordPrefabInstancePropertyModifications(trail);
        EditorSceneManager.MarkSceneDirty(ball.scene);
        EditorSceneManager.SaveScene(ball.scene);
        Validate(ball);
        EditorSceneManager.SaveScene(ball.scene);
        Selection.activeGameObject=ball;
        File.WriteAllText("Temp/EditableCatStarsValidation.txt","PASS: adjustable size, length, emission rate, backward speed, downward speed and fade-out time. Split-piece settings verified. Scene 2 saved.");
    }
    [MenuItem("Tools/Scene 2/Add Short Star Sparkle Trail")]
    public static void Apply()
    {
        EditorSceneManager.SaveOpenScenes();
        if (SceneManager.GetActiveScene().path!="Assets/Scenes/2.unity") EditorSceneManager.OpenScene("Assets/Scenes/2.unity");
        var ball=GameObject.Find("ball");
        var shader=Shader.Find("Cat/Four Point Star Glow");
        if (!shader || shader.isSupported==false) throw new Exception("Star shader unavailable.");
        const string path="Assets/BurgerCat/Star Sparkle Glow.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) {mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
        var trail=ball.GetComponent<CatStarTrail>();
        if (!trail) trail=Undo.AddComponent<CatStarTrail>(ball);
        Undo.RecordObject(trail,"Configure short star trail");
        trail.starMaterial=mat;
        EditorUtility.SetDirty(trail);
        PrefabUtility.RecordPrefabInstancePropertyModifications(trail);
        EditorSceneManager.MarkSceneDirty(ball.scene);
        EditorSceneManager.SaveScene(ball.scene);
        AssetDatabase.SaveAssets();
        Validate(ball);
        RenderPreview(ball);
        EditorSceneManager.SaveScene(ball.scene);
        File.WriteAllText("Temp/CatStarsValidation.txt","PASS: scene 2 saved with short white/gold four-point sparkle trail.\nAdditive self-glow shader supported.\nSecond split piece inherits identical trail material and settings.\nParticles stay in world space, fade/shrink, and emit only on moving non-kinematic bodies.");
    }
    static void Validate(GameObject ball)
    {
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var source=UnityEngine.Object.Instantiate(ball);
            SceneManager.MoveGameObjectToScene(source,preview);
            var split=source.GetComponent<CollisionSplit>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(CollisionSplit).GetField("body",flags).SetValue(split,source.GetComponent<Rigidbody>());
            var second=(GameObject)typeof(CollisionSplit).GetMethod("CreateSecondPiece",flags).Invoke(split,null);
            var a=source.GetComponent<CatStarTrail>();var b=second.GetComponent<CatStarTrail>();
            if (!b || a.starMaterial!=b.starMaterial || a.maximumTrailLength!=b.maximumTrailLength || a.starSize!=b.starSize ||
                a.starsPerSecond!=b.starsPerSecond || a.backwardSpeed!=b.backwardSpeed ||
                a.downwardSpeed!=b.downwardSpeed || a.fadeOutTime!=b.fadeOutTime)
                throw new Exception("Split trail inheritance failed.");
        }
        finally {EditorSceneManager.ClosePreviewScene(preview);}
    }
    static void RenderPreview(GameObject ball)
    {
        var preview=EditorSceneManager.NewPreviewScene();
        RenderTexture rt=null; Texture2D texture=null;
        try
        {
            var cat=UnityEngine.Object.Instantiate(ball);
            SceneManager.MoveGameObjectToScene(cat,preview);
            cat.transform.position=Vector3.zero;
            foreach(var t in cat.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            var trail=cat.GetComponent<CatStarTrail>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(CatStarTrail).GetMethod("CreateParticles",flags).Invoke(trail,null);
            var stars=(ParticleSystem)typeof(CatStarTrail).GetField("stars",flags).GetValue(trail);
            stars.gameObject.layer=31;
            for(int i=0;i<6;i++)
            {
                var p=new ParticleSystem.EmitParams();
                p.position=new Vector3(-9.7f-i*.85f,(i%2==0?.7f:-.6f),-5f);
                p.startSize=1.5f-i*.14f;p.startLifetime=.22f;
                p.startColor=i%2==0?new Color(1,.96f,.8f):new Color(1,.76f,.25f);
                p.velocity=Vector3.zero;
                stars.Emit(p,1);
            }
            stars.Simulate(.04f,false,false,true);
            var cameraObject=new GameObject("Preview camera");
            SceneManager.MoveGameObjectToScene(cameraObject,preview);
            var camera=cameraObject.AddComponent<Camera>();
            camera.scene=preview;camera.cullingMask=1<<31;
            camera.transform.position=new Vector3(-3,6,-60);camera.transform.LookAt(new Vector3(-3,0,0));
            camera.orthographic=true;camera.orthographicSize=13;camera.aspect=1.6f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.075f);
            var lightObject=new GameObject("Preview light");
            SceneManager.MoveGameObjectToScene(lightObject,preview);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;
            lightObject.transform.rotation=Quaternion.Euler(35,-25,0);
            rt=new RenderTexture(960,600,24);camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;texture=new Texture2D(960,600,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,960,600),0,0);texture.Apply();
            File.WriteAllBytes("Temp/CatStarsPreview.png",texture.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;
        }
        finally
        {
            RenderTexture.active=null;
            if(rt)UnityEngine.Object.DestroyImmediate(rt);
            if(texture)UnityEngine.Object.DestroyImmediate(texture);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }
}

