using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Scene2RouteBuilder
{
    static Scene2RouteBuilder()
    {
        EditorApplication.delayCall += Inspect;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Scene2Validate", false))
            {
                SessionState.SetBool("Scene2Validate", false);
                try { ValidateInPlayMode(); }
                finally { EditorApplication.isPlaying = false; }
            }
        };
    }

    [MenuItem("Tools/Scene 2/Inspect Route %#F7")]
    public static void Inspect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/2.unity") return;
        var report = new StringBuilder();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!root.activeSelf) continue;
            foreach (Transform t in root.GetComponentsInChildren<Transform>())
            {
                var collider = t.GetComponent<Collider>();
                var ball = t.GetComponent<BallForces>();
                if (t != root.transform && collider == null && ball == null) continue;
                report.AppendLine($"{PathOf(t)} pos={t.position} scale={t.lossyScale} rotation={t.eulerAngles}");
                if (collider != null) report.AppendLine($"  {collider.GetType().Name} center={collider.bounds.center} size={collider.bounds.size} trigger={collider.isTrigger}");
                if (ball != null) report.AppendLine($"  PLAYER force={ball.minLaunchForce}..{ball.maxLaunchForce} drag={ball.dragSensitivity} gravity={ball.useGravity}");
                var body = t.GetComponent<Rigidbody>();
                if (body != null) report.AppendLine($"  BODY mass={body.mass} kinematic={body.isKinematic} constraints={body.constraints}");
            }
        }
        File.WriteAllText("Temp/Scene2Inspection.txt", report.ToString());
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

    private const string CourseName = "Scene 2 - Wind Chess Course";
    private static Transform course;
    private static Mesh pieceMesh;
    private static Material black, gold, teal, link;
    private static PhysicsMaterial railPhysics;
    private static readonly Vector2[] route = {
        new Vector2(-110f, 245f), new Vector2(-20f, 285f),
        new Vector2(80f, 385f), new Vector2(155f, 460f),
        new Vector2(140f, 493f), new Vector2(111.48f, 511.34f)
    };

    [MenuItem("Tools/Scene 2/Build Chess Route %#F8")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/2.unity")
            throw new System.InvalidOperationException("Open scene 2 before building its course.");
        if (GameObject.Find(CourseName) != null)
            throw new System.InvalidOperationException("Course already exists; use Undo before rebuilding.");
        File.Copy(scene.path, "Temp/Scene2BeforeCourse.unity", true);
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build scene 2 chess route");
        int undoGroup = Undo.GetCurrentGroup();

        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/model__159 (25).prefab");
        pieceMesh = source.GetComponent<MeshFilter>().sharedMesh;
        if (!AssetDatabase.IsValidFolder("Assets/RouteMaterials"))
            AssetDatabase.CreateFolder("Assets", "RouteMaterials");
        black = MakeMaterial("Chess Graphite", new Color(0.075f, 0.09f, 0.12f));
        gold = MakeMaterial("Hinge Amber", new Color(0.95f, 0.56f, 0.12f));
        teal = MakeMaterial("Spring Jade", new Color(0.08f, 0.64f, 0.58f));
        link = new Material(Resources.Load<Shader>("WindLines"));
        AssetDatabase.CreateAsset(link, "Assets/RouteMaterials/Joint Lines.mat");
        railPhysics = new PhysicsMaterial("Smooth Chess Guides") {
            dynamicFriction = 0.05f, staticFriction = 0.05f,
            bounciness = 0.05f, frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        AssetDatabase.CreateAsset(railPhysics, "Assets/RouteMaterials/Smooth Chess Guides.physicMaterial");

        // Keep previous arrangements in the scene, disabled, so they remain recoverable.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Mon" || root.name == "Mon (1)" || root.name == "Mon (2)" || root.name == "protector")
            {
                Undo.RecordObject(root, "Archive previous chess layout");
                root.SetActive(false);
            }
        }
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name.StartsWith("model__159") && t.GetComponent<Collider>() != null)
            {
                Undo.RecordObject(t.gameObject, "Archive previous chess piece");
                t.gameObject.SetActive(false);
            }
        }

        course = new GameObject(CourseName).transform;
        Undo.RegisterCreatedObjectUndo(course.gameObject, "Create chess course");
        Transform guides = Group("01 - Chess Guide Rails");
        // Parallel bead rails with mitred corners. Capsules prevent snagging between pieces.
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2[] edge = new Vector2[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                Vector2 before = (route[Mathf.Max(1, i)] - route[Mathf.Max(0, i - 1)]).normalized;
                Vector2 after = (route[Mathf.Min(route.Length - 1, i + 1)] - route[Mathf.Min(route.Length - 2, i)]).normalized;
                Vector2 n1 = new Vector2(-before.y, before.x);
                Vector2 n2 = new Vector2(-after.y, after.x);
                Vector2 miter = (n1 + n2).normalized;
                edge[i] = route[i] + miter * (side * 28f / Mathf.Max(0.6f, Vector2.Dot(miter, n2)));
            }
            for (int i = 0; i < edge.Length - 1; i++)
                Rail(guides, $"Guide {side} / {i + 1}", edge[i], edge[i + 1]);
        }
        Rail(guides, "Start catch platform", new Vector2(-160f, 220f), new Vector2(-96f, 220f));
        Rail(guides, "Goal catcher", new Vector2(90f, 499f), new Vector2(120f, 499f));

        Transform hinges = Group("02 - Amber Hinge Gates");
        HingeGate(hinges, "Gate A - Launch flap", new Vector2(-30f, 256f), -20f, 3);
        HingeGate(hinges, "Gate B - Lift pendulum", new Vector2(47f, 389f), -65f, 4);
        HingeGate(hinges, "Gate C - Summit flap", new Vector2(181f, 456f), 150f, 3);

        Transform springs = Group("03 - Jade Spring Bumpers");
        SpringBumper(springs, "Spring A - Entry cushion", new Vector2(18f, 279f), new Vector2(4f, 289f));
        SpringBumper(springs, "Spring B - Lift cushion", new Vector2(108f, 382f), new Vector2(95f, 392f));
        SpringBumper(springs, "Spring C - Goal cushion", new Vector2(174f, 499f), new Vector2(160f, 494f));

        WindZone[] winds = Object.FindObjectsByType<WindZone>(FindObjectsSortMode.None).OrderBy(w => w.name).ToArray();
        ConfigureWind(winds[0], new Vector2(9f, 308f), new Vector2(142f, 150f), new Vector3(0.65f, 1f, 0f), 170f);
        ConfigureWind(winds[1], new Vector2(108f, 409f), new Vector2(126f, 130f), new Vector3(0.65f, 1f, 0f), 150f);
        ConfigureWind(winds[2], new Vector2(139f, 499f), new Vector2(92f, 62f), new Vector3(-0.65f, 1f, 0f), 120f);

        var player = Object.FindFirstObjectByType<BallForces>();
        Undo.RecordObject(player.transform, "Position launch piece");
        Undo.RecordObject(player, "Tune scene 2 launch");
        player.transform.position = new Vector3(route[0].x, route[0].y, -79f);
        player.minY = 213f;
        player.maxY = 530f;
        player.dragSensitivity = 0.04f;
        player.minLaunchForce = 180f;
        player.maxLaunchForce = 270f;
        var playerBody = player.GetComponent<Rigidbody>();
        Undo.RecordObject(playerBody, "Tune scene 2 movement");
        playerBody.linearDamping = 0.7f;
        playerBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        playerBody.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject goal = GameObject.Find("wincondition");
        var goalBody = goal.GetComponent<Rigidbody>();
        Undo.RecordObject(goalBody, "Keep goal fixed");
        goalBody.isKinematic = true;
        goalBody.useGravity = false;
        var goalRenderer = goal.GetComponent<Renderer>();
        Undo.RecordObject(goalRenderer, "Highlight goal");
        goalRenderer.sharedMaterial = gold;
        foreach (Joint joint in goal.GetComponents<Joint>())
            Undo.DestroyObjectImmediate(joint);

        var hints = Group("04 - Route Markers");
        Label(hints, "START  /  DRAG DOWN-LEFT", new Vector2(-135f, 266f), 3f);
        Label(hints, "01  RIDE THE WIND", new Vector2(-30f, 320f), 2.5f);
        Label(hints, "02  HINGE GATES", new Vector2(48f, 429f), 2.5f);
        Label(hints, "03  SPRING TURN", new Vector2(190f, 482f), 2.5f);
        Label(hints, "GOAL", new Vector2(111f, 524f), 3f);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = course.gameObject;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(35f, 375f, -79f), Quaternion.identity, 220f, true);
        Inspect();
        Debug.Log("Scene 2 chess route built and saved. Ctrl+Shift+F9 validates launch trajectories.");
    }

    private static Material MakeMaterial(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.55f);
        AssetDatabase.CreateAsset(mat, $"Assets/RouteMaterials/{name}.mat");
        return mat;
    }

    private static Transform Group(string name)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(course, false);
        return t;
    }

    private static GameObject Piece(Transform parent, string name, Vector2 xy, Material material, bool solid = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(xy.x, xy.y, -77.24f);
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        go.transform.localScale = Vector3.one * 40f;
        go.AddComponent<MeshFilter>().sharedMesh = pieceMesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (solid)
        {
            var col = go.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0f, 0.044f);
            col.radius = 0.11f;
            col.sharedMaterial = railPhysics;
        }
        return go;
    }

    private static void Rail(Transform parent, string name, Vector2 a, Vector2 b)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, -79f);
        root.transform.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(b.x - a.x, b.y - a.y, 0f));
        var col = root.AddComponent<CapsuleCollider>();
        col.direction = 1;
        col.radius = 4.4f;
        col.height = Vector2.Distance(a, b) + 8.8f;
        col.sharedMaterial = railPhysics;
        int count = Mathf.CeilToInt(Vector2.Distance(a, b) / 7.5f);
        for (int i = 0; i <= count; i++)
            Piece(parent, $"{name} / Stone {i + 1:00}", Vector2.Lerp(a, b, i / (float)count), black);
    }

    private static void HingeGate(Transform parent, string name, Vector2 pivot, float angle, int count)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(pivot.x, pivot.y, -79f);
        root.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        var body = root.AddComponent<Rigidbody>();
        body.mass = 1.5f;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        body.angularDamping = 1.5f;
        body.solverIterations = 12;
        body.solverVelocityIterations = 8;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = root.transform.TransformPoint(new Vector3(i * 7.5f, 0f, 0f));
            Piece(root.transform, "Hinge stone " + (i + 1), p, gold, true);
        }
        var joint = root.AddComponent<HingeJoint>();
        joint.axis = Vector3.forward;
        joint.anchor = Vector3.zero;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = root.transform.position;
        joint.useLimits = true;
        joint.limits = new JointLimits { min = -65f, max = 65f };
        joint.useSpring = true;
        joint.spring = new JointSpring { spring = 16f, damper = 6f, targetPosition = 0f };
        Piece(parent, "Fixed hinge pin", pivot, black, false);
    }

    private static void SpringBumper(Transform parent, string name, Vector2 pin, Vector2 position)
    {
        Piece(parent, name + " / Fixed pin", pin, black);
        GameObject go = Piece(parent, name, position, teal, true);
        Rigidbody body = go.AddComponent<Rigidbody>();
        body.mass = 1.2f;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.linearDamping = 1.5f;
        body.solverIterations = 12;
        SpringJoint joint = go.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = new Vector3(0f, 0f, 0.044f);
        joint.connectedAnchor = new Vector3(pin.x, pin.y, -79f);
        joint.spring = 35f;
        joint.damper = 12f;
        joint.minDistance = Vector2.Distance(pin, position) - 2f;
        joint.maxDistance = Vector2.Distance(pin, position) + 2f;
        joint.tolerance = 0.1f;
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = link;
        line.useWorldSpace = true;
        line.widthMultiplier = 0.5f;
        line.startColor = new Color(0.08f, 0.75f, 0.62f);
        line.endColor = line.startColor;
        var visual = go.AddComponent<JointLinkVisual>();
        visual.joint = joint;
        visual.spring = true;
    }

    private static void ConfigureWind(WindZone wind, Vector2 center, Vector2 size, Vector3 direction, float force)
    {
        Undo.RecordObject(wind, "Tune route wind");
        Undo.RecordObject(wind.transform, "Position route wind");
        var col = wind.GetComponent<BoxCollider>();
        Undo.RecordObject(col, "Size route wind");
        wind.transform.position = new Vector3(center.x, center.y, -79f);
        col.center = Vector3.zero;
        col.size = new Vector3(size.x, size.y, 30f);
        wind.windDirection = direction;
        wind.windForce = force;
        wind.affectJointedBodies = false;
        wind.showWindLines = true;
        wind.lineWidth = 0.7f;
        wind.lineLength = 12f;
        wind.lineCount = 35;
        wind.lineSpeed = 25f;
    }

    private static void Label(Transform parent, string text, Vector2 xy, float size)
    {
        var go = new GameObject(text);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(xy.x, xy.y, -86f);
        var label = go.AddComponent<TextMesh>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.characterSize = size;
        label.fontSize = 48;
        label.color = new Color(0.12f, 0.16f, 0.19f);
    }

    [MenuItem("Tools/Scene 2/Validate Launches %#F9")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("Scene2Validate", true);
        EditorApplication.isPlaying = true;
    }

    private static void ValidateInPlayMode()
    {
        var original = Object.FindFirstObjectByType<BallForces>();
        var windZones = Object.FindObjectsByType<WindZone>(FindObjectsSortMode.None);
        GameObject[] sources = new[] { CourseName, "ball", "wincondition", "Wall", "Wall (1)", "Wall (2)", "Wall (3)" }
            .Select(GameObject.Find).Where(g => g != null).ToArray();
        var report = new StringBuilder();
        float bestDistance = float.PositiveInfinity;
        string bestTrace = "";
        foreach (float angle in new[] { 25f, 35f, 45f, 55f })
        foreach (float force in new[] { 180f, 225f, 270f })
        {
            Scene simulation = SceneManager.CreateScene("Route Physics Test", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            try
            {
                foreach (GameObject source in sources)
                {
                    GameObject clone = Object.Instantiate(source);
                    clone.name = source.name;
                    SceneManager.MoveGameObjectToScene(clone, simulation);
                    foreach (MonoBehaviour script in clone.GetComponentsInChildren<MonoBehaviour>(true))
                        Object.DestroyImmediate(script);
                }
                GameObject ball = simulation.GetRootGameObjects().First(g => g.name == "ball");
                GameObject goal = simulation.GetRootGameObjects().First(g => g.name == "wincondition");
                Rigidbody body = ball.GetComponent<Rigidbody>();
                Collider ballCollider = ball.GetComponent<Collider>();
                Collider goalCollider = goal.GetComponent<Collider>();
                body.isKinematic = false;
                body.linearVelocity = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * force / body.mass;
                PhysicsScene physics = simulation.GetPhysicsScene();
                var trace = new StringBuilder();
                float closest = float.PositiveInfinity;
                bool won = false;
                float elapsed = 0f;
                for (int step = 0; step < 2000; step++)
                {
                    foreach (WindZone wind in windZones)
                    {
                        Bounds bounds = wind.GetComponent<BoxCollider>().bounds;
                        if (bounds.SqrDistance(body.position) <= 4.29f * 4.29f)
                            body.AddForce(wind.windDirection.normalized * wind.windForce, ForceMode.Force);
                    }
                    physics.Simulate(0.02f);
                    elapsed = step * 0.02f;
                    float distance = Vector3.Distance(body.position, goal.transform.position);
                    closest = Mathf.Min(closest, distance);
                    if (step % 10 == 0) trace.AppendLine($"{elapsed:F2},{body.position.x:F2},{body.position.y:F2}");
                    // Contact can be resolved before the next step, so allow contact offset.
                    if (goalCollider.bounds.SqrDistance(body.position) <= 4.4f * 4.4f ||
                        Physics.ComputePenetration(ballCollider, ball.transform.position, ball.transform.rotation,
                            goalCollider, goal.transform.position, goal.transform.rotation, out _, out _))
                    {
                        won = true;
                        break;
                    }
                }
                report.AppendLine($"angle={angle} force={force} won={won} time={elapsed:F2} closest={closest:F2} final={body.position}");
                if (closest < bestDistance)
                {
                    bestDistance = closest;
                    bestTrace = trace.ToString();
                }
            }
            finally
            {
                foreach (GameObject root in simulation.GetRootGameObjects()) Object.DestroyImmediate(root);
                SceneManager.UnloadSceneAsync(simulation);
            }
        }
        File.WriteAllText("Temp/Scene2RouteValidation.txt", report.ToString());
        File.WriteAllText("Temp/Scene2BestTrajectory.csv", "time,x,y\n" + bestTrace);
        Debug.Log("Route physics sweep complete. " + report);
    }
}
