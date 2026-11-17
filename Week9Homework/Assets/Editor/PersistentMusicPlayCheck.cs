using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PersistentMusicPlayCheck
{
    private const string Request = "Temp/PersistentMusicPlayCheck.request";
    private static int stage, samples;
    private static PersistentMusic originalMusic;
    private static double nextCheck;
    private static AudioClip silentClip;

    static PersistentMusicPlayCheck() { EditorApplication.update += Update; }

    private static void Update()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling) return;
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/Start.unity") return;
            if (!UnityEngine.Object.FindFirstObjectByType<PersistentMusic>()) return;
            if (SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            SessionState.SetBool("MusicCheckReturnToEdit", true);
            EditorApplication.isPlaying = true;
            return;
        }
        if (EditorApplication.isPaused || EditorApplication.timeSinceStartup < nextCheck) return;
        try
        {
            var music = PersistentMusic.Instance;
            if (!music) { nextCheck = EditorApplication.timeSinceStartup + .1; return; }
            var source = music.GetComponent<AudioSource>();
            if (stage == 0)
            {
                silentClip = AudioClip.Create("Temporary silent BGM test", 44100 * 8, 1, 44100, false);
                music.musicClip = silentClip;
                music.volume = 0f;
                originalMusic = music;
                stage = 1;
                nextCheck = EditorApplication.timeSinceStartup + .3;
                return;
            }
            if (stage == 1)
            {
                Require(source.isPlaying && source.loop && source.spatialBlend == 0f, "automatic loop playback");
                samples = source.timeSamples;
                SceneManager.LoadScene("Assets/Scenes/0.unity");
                stage = 2;
                nextCheck = EditorApplication.timeSinceStartup + .3;
                return;
            }
            if (stage == 2)
            {
                Require(music == originalMusic && source.isPlaying && source.timeSamples > samples,
                    "same source keeps advancing across scene 0 transition");
                SceneManager.LoadScene("Assets/Scenes/Start.unity");
                stage = 3;
                nextCheck = EditorApplication.timeSinceStartup + .3;
                return;
            }
            Require(music == originalMusic && source.isPlaying &&
                UnityEngine.Object.FindObjectsByType<PersistentMusic>().Length == 1,
                "returning to Start preserves music without duplicates");
            Finish("PASS: silent test clip automatically loops; same source and sample progress retained during Start -> 0; returning to Start does not duplicate or restart music. User's audio slot remains empty in saved scene.");
        }
        catch (Exception exception) { Finish("FAIL: " + exception); }
    }

    private static void Finish(string report)
    {
        File.WriteAllText("Temp/PersistentMusicPlayCheckReport.txt", report);
        File.Delete(Request);
        if (SessionState.GetBool("MusicCheckReturnToEdit", false))
        {
            SessionState.SetBool("MusicCheckReturnToEdit", false);
            EditorApplication.isPlaying = false;
        }
        stage = 0;
    }

    private static void Require(bool value, string check)
    {
        if (!value) throw new Exception("Music check failed: " + check);
    }
}

