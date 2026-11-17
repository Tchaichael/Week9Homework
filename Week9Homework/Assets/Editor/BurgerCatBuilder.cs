using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Editor-only authoring tool. All geometry and materials are saved as ordinary assets.
public static class BurgerCatBuilder
{
    const string Folder = "Assets/BurgerCat";
    static Transform root;
    static int meshIndex;
    static Material bun, toast, cream, meat, cheese, dark, pink;

    [MenuItem("Tools/Scene 3/Build Burger Cat")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/3.unity");
        if (GameObject.Find("Burger Cat") != null) throw new Exception("Burger Cat already exists.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        bun = Mat("Golden bun", new Color(.77f,.47f,.19f));
        toast = Mat("Toasted ears", new Color(.43f,.22f,.10f));
        cream = Mat("Soft bread", new Color(.92f,.77f,.47f));
        meat = Mat("Grilled patty", new Color(.29f,.14f,.075f));
        cheese = Mat("Melted cheddar", new Color(1f,.67f,.12f));
        dark = Mat("Chocolate face", new Color(.09f,.045f,.023f));
        pink = Mat("Inner ears", new Color(.80f,.43f,.27f));
        root = new GameObject("Burger Cat").transform;
        // Front is -Z. Broad, squat silhouette, with a genuinely rounded 3D back.
        RingShape("Bottom bun", new[]{new Vector2(.05f,-.30f),new Vector2(.40f,-.30f),new Vector2(.48f,-.24f),new Vector2(.50f,-.14f),new Vector2(.46f,-.11f)}, cream, .76f);
        RingShape("Patty", new[]{new Vector2(.02f,-.12f),new Vector2(.47f,-.12f),new Vector2(.49f,-.08f),new Vector2(.48f,.015f),new Vector2(.02f,.02f)}, meat,.76f, true);
        RingShape("Cat bun head",new[]{new Vector2(.02f,.025f),new Vector2(.50f,.025f),new Vector2(.49f,.14f),new Vector2(.44f,.26f),new Vector2(.33f,.34f),new Vector2(.17f,.38f),new Vector2(0,.39f)},bun,.76f);
        // Thin draped cheese disk, with four soft hanging corners.
        var cv=new List<Vector3>{new Vector3(0,.024f,0)}; var ct=new List<int>();
        for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64; float r=.51f+.025f*Mathf.Cos(4*a); cv.Add(new Vector3(Mathf.Cos(a)*r,.018f-.065f*Mathf.Pow(Mathf.Max(0,Mathf.Cos(4*a)),10),Mathf.Sin(a)*r*.78f));}
        for(int i=0;i<64;i++){ct.Add(0);ct.Add(1+(i+1)%64);ct.Add(i+1);} Part("Draped cheese",cv,ct,cheese);
        Ear(-.31f); Ear(.31f);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2) Ellipsoid("Little paw",new Vector3(x*.28f,-.32f,z*.20f),new Vector3(.18f,.17f,.20f),cream);
        Ellipsoid("Left eye",new Vector3(-.255f,.155f,-.334f),new Vector3(.072f,.083f,.035f),dark);
        Ellipsoid("Right eye",new Vector3(.255f,.155f,-.334f),new Vector3(.072f,.083f,.035f),dark);
        Ellipsoid("Muzzle",new Vector3(0,.105f,-.374f),new Vector3(.22f,.105f,.021f),cream);
        Tube("Smile left",new[]{new Vector3(-.085f,.11f,-.392f),new Vector3(-.064f,.091f,-.397f),new Vector3(-.033f,.093f,-.400f),new Vector3(0,.111f,-.401f)},.009f,dark);
        Tube("Smile right",new[]{new Vector3(0,.111f,-.401f),new Vector3(.033f,.093f,-.400f),new Vector3(.064f,.091f,-.397f),new Vector3(.085f,.11f,-.392f)},.009f,dark);
        Tube("Upright curved tail",new[]{new Vector3(-.40f,-.17f,.14f),new Vector3(-.53f,-.07f,.16f),new Vector3(-.61f,.09f,.16f),new Vector3(-.66f,.28f,.15f),new Vector3(-.68f,.45f,.14f),new Vector3(-.67f,.53f,.13f)},.065f,bun);
        Ellipsoid("Tail tip",new Vector3(-.67f,.53f,.13f),new Vector3(.13f,.14f,.13f),cream);
        var random=new System.Random(17);
        for(int i=0;i<27;i++){float a=(float)random.NextDouble()*Mathf.PI*2;float r=Mathf.Sqrt((float)random.NextDouble())*.38f;float x=Mathf.Cos(a)*r;float z=Mathf.Sin(a)*r*.76f;float y=Top(r);var seed=Ellipsoid("Sesame",new Vector3(x,y+.006f,z),new Vector3(.018f,.009f,.038f),cream);seed.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*180,0);}
        var ball=GameObject.Find("ball");
        if(ball==null)throw new Exception("No active ball in scene 3.");
        Bounds bb=BoundsOf(ball); Bounds cb=BoundsOf(root.gameObject);
        float diameter=Mathf.Max(bb.size.x,bb.size.y,bb.size.z);
        float scale=diameter/cb.size.x;
        root.localScale=Vector3.one*scale;
        // Place beside ball, at its bottom height, leaving clear space around the player.
        cb=BoundsOf(root.gameObject);
        root.position=new Vector3(bb.center.x+diameter*1.35f,bb.min.y-cb.min.y,bb.center.z);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"/Burger Cat.prefab");
        Vector3 pos=root.position; UnityEngine.Object.DestroyImmediate(root.gameObject);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.transform.position=pos;
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Temp/BurgerCatReport.txt",$"ball bounds: {bb}\ncat bounds: {BoundsOf(instance)}\ncat position: {pos}\nmesh count: {meshIndex}\n");
        Preview(instance);
        AssetDatabase.SaveAssets();
    }
    static float Top(float r){if(r<.17f)return Mathf.Lerp(.39f,.38f,r/.17f);if(r<.33f)return Mathf.Lerp(.38f,.34f,(r-.17f)/.16f);return Mathf.Lerp(.34f,.26f,(r-.33f)/.11f);}
    [MenuItem("Tools/Scene 3/Finish Burger Cat")] public static void Finish(){var path=Folder+"/Burger Cat.prefab";var p=PrefabUtility.LoadPrefabContents(path);var random=new System.Random(17);foreach(var t in p.GetComponentsInChildren<Transform>()){if(t.name=="Sesame"){float a=(float)random.NextDouble()*Mathf.PI*2;float r=Mathf.Sqrt((float)random.NextDouble())*.38f;t.localPosition=new Vector3(Mathf.Cos(a)*r,Top(r)+.006f,Mathf.Sin(a)*r*.76f);random.NextDouble();}if(t.name=="Draped cheese"){var mesh=t.GetComponent<MeshFilter>().sharedMesh;var old=mesh.triangles;if(old.Length==384){var tris=new List<int>();for(int i=0;i<old.Length;i+=6)tris.AddRange(new[]{old[i],old[i+1],old[i+2]});mesh.triangles=tris.ToArray();mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);}}}PrefabUtility.SaveAsPrefabAsset(p,path);PrefabUtility.UnloadPrefabContents(p);AssetDatabase.SaveAssets();RefreshPreview();}
    static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.color=color;m.SetFloat("_Smoothness",.22f);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
    static GameObject Part(string name,List<Vector3> v,List<int> t,Material mat){var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/Mesh"+(meshIndex++)+".asset");var g=new GameObject(name);g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;}
    static void RingShape(string name,Vector2[] rings,Material mat,float depth,bool rough=false){var v=new List<Vector3>();var t=new List<int>();const int n=40;for(int j=0;j<rings.Length;j++)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;float r=rings[j].x*(rough?1+.022f*Mathf.Sin(i*4.7f+j*2):1);v.Add(new Vector3(Mathf.Cos(a)*r,rings[j].y,Mathf.Sin(a)*r*depth));}for(int j=0;j<rings.Length-1;j++)for(int i=0;i<n;i++){int a=j*n+i,b=j*n+(i+1)%n,c=a+n,d=b+n;t.AddRange(new[]{a,c,b,b,c,d});}Part(name,v,t,mat);}
    static GameObject Ellipsoid(string name,Vector3 p,Vector3 s,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
    static void Ear(float x){var v=new List<Vector3>{new Vector3(x-.125f,.26f,-.10f),new Vector3(x+.125f,.26f,-.10f),new Vector3(x+Mathf.Sign(x)*.025f,.60f,-.045f),new Vector3(x-.10f,.27f,.12f),new Vector3(x+.10f,.27f,.12f)};Part("Pointed cat ear",v,new List<int>{0,2,1,0,3,2,1,2,4,3,4,2,0,1,4,0,4,3},bun);Part("Toasted inner ear",new List<Vector3>{new Vector3(x-.072f,.315f,-.105f),new Vector3(x+Mathf.Sign(x)*.021f,.535f,-.061f),new Vector3(x+.072f,.315f,-.105f)},new List<int>{0,1,2},pink);}
    static void Tube(string name,Vector3[] points,float radius,Material mat){var v=new List<Vector3>();var t=new List<int>();const int n=10;for(int j=0;j<points.Length;j++){var direction=(points[Mathf.Min(j+1,points.Length-1)]-points[Mathf.Max(0,j-1)]).normalized;var u=Vector3.Cross(direction,Vector3.forward).normalized;var w=Vector3.Cross(direction,u);for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;v.Add(points[j]+radius*(u*Mathf.Cos(a)+w*Mathf.Sin(a)));}}for(int j=0;j<points.Length-1;j++)for(int i=0;i<n;i++){int a=j*n+i,b=j*n+(i+1)%n;t.AddRange(new[]{a,b,a+n,b,b+n,a+n});}Part(name,v,t,mat);}
    [MenuItem("Tools/Scene 3/Preview Burger Cat")] public static void RefreshPreview(){var cat=GameObject.Find("Burger Cat"); Preview(cat); Selection.activeGameObject=cat; var sv=SceneView.lastActiveSceneView;sv.sceneLighting=false;sv.sceneViewState.showImageEffects=false;sv.LookAt(BoundsOf(cat).center,Quaternion.Euler(10,-15,0),BoundsOf(cat).size.x*1.2f);sv.Repaint();}
    static void Preview(GameObject cat){var preview=EditorSceneManager.NewPreviewScene();var copy=UnityEngine.Object.Instantiate(cat);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,preview);foreach(var t in copy.GetComponentsInChildren<Transform>())t.gameObject.layer=31;copy.transform.position=Vector3.zero;copy.transform.localScale=Vector3.one;var b=BoundsOf(copy);var cg=new GameObject("Preview camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,preview);var cam=cg.AddComponent<Camera>();cam.cullingMask=1<<31;cam.scene=preview;cam.transform.position=b.center+new Vector3(-1.4f,.85f,-3);cam.transform.LookAt(b.center);cam.orthographic=true;cam.orthographicSize=.78f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.91f,.88f,.81f);var lg=new GameObject("Preview light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lg,preview);var l=lg.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.5f;lg.transform.rotation=Quaternion.Euler(35,-25,0);var rt=new RenderTexture(800,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(800,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,800,800),0,0);tex.Apply();File.WriteAllBytes("Temp/BurgerCatPreview.png",tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);EditorSceneManager.ClosePreviewScene(preview);}
}



