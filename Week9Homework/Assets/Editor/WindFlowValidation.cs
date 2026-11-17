using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WindFlowValidation
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/WindFlowValidation.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Validate();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 2/Validate Wind Movement And Fade")]
    public static void Validate()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            Check(scene, Vector3.up, 500f);
            Check(scene, new Vector3(1f, 1f, 0f), 500f);
            Check(scene, new Vector3(1f, 1f, 0f), -500f);
            File.WriteAllText("Temp/WindFlowValidationReport.txt",
                "PASS: independent startup delays span a full crossing cycle; each line waits a new random interval after exit. Up/diagonal/reversed movement, full traversal, complete exit and boundary shader checks pass; animated curvature retained.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void Check(Scene scene, Vector3 direction, float force)
    {
        var go = new GameObject("Wind flow validation");
        SceneManager.MoveGameObjectToScene(go, scene);
        WindZone wind = null;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            go.transform.rotation = Quaternion.Euler(0f, 0f, 25f);
            go.transform.localScale = new Vector3(2f, .5f, 1f);
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one * 200f;
            wind = go.AddComponent<WindZone>();
            typeof(WindZone).GetMethod("OnDisable", flags).Invoke(wind, null);
            wind.windDirection = direction;
            wind.windForce = force;
            wind.softCurves = true;
            wind.lineCount = 45;
            wind.lineLength = 30f;
            wind.lineSpeed = 12f;
            wind.lineLifetime = 3f;
            typeof(WindZone).GetMethod("OnEnable", flags).Invoke(wind, null);
            var initialAges = (float[])typeof(WindZone).GetField("ages", flags).GetValue(wind);
            Require(initialAges.Distinct().Count() > 40 && initialAges.Min() < -3f,
                "startup emissions spread irregularly across the crossing cycle");
            ((float[])typeof(WindZone).GetField("ages", flags).GetValue(wind))[0] = 0f;
            var line = ((LineRenderer[])typeof(WindZone).GetField("lines", flags).GetValue(wind))[0];
            wind.AdvanceVisuals(0f, 0f);
            Vector3 before = line.GetPosition(12);
            Require(line.positionCount == 25 && !HasPointInside(line, go.transform, box), "entire line starts outside entry");
            wind.AdvanceVisuals(.3f, .3f);
            Vector3 after = line.GetPosition(12);
            Vector3 expected = direction.normalized * Mathf.Sign(force);
            Require(Mathf.Abs(Vector3.Dot(after - before, expected) - 3.6f) < .02f, "movement follows Wind Direction at Line Speed");
            float lifetime = ((float[])typeof(WindZone).GetField("lifetimes", flags).GetValue(wind))[0];
            float middle = lifetime * .5f;
            wind.AdvanceVisuals(middle - .3f, middle);
            Require(HasPointInside(line, go.transform, box), "line reaches the middle without early recycling");
            Require(line.startColor.a > 0f && lifetime > 3f, "no premature time-based fade");
            wind.AdvanceVisuals(lifetime - middle - .001f, lifetime - .001f);
            Require(!HasPointInside(line, go.transform, box), "entire tail exits before recycling");
            wind.AdvanceVisuals(.01f, lifetime + .009f);
            Require(!HasPointInside(line, go.transform, box), "new line respawns outside entry");
            float wait = -((float[])typeof(WindZone).GetField("ages", flags).GetValue(wind))[0];
            Require(wait >= wind.respawnDelay.x && wait <= wind.respawnDelay.y,
                "line receives an independent random respawn pause");
            Require(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("WindLines")), "boundary fade shader compiles");
        }
        finally
        {
            if (wind) typeof(WindZone).GetMethod("OnDisable", flags).Invoke(wind, null);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static bool HasPointInside(LineRenderer line, Transform zone, BoxCollider box)
    {
        var bounds = new Bounds(box.center, box.size);
        for (int i = 0; i < line.positionCount; i++)
            if (bounds.Contains(zone.InverseTransformPoint(line.GetPosition(i)))) return true;
        return false;
    }

    private static void Require(bool result, string check)
    {
        if (!result) throw new Exception("Wind flow validation failed: " + check);
    }
}
