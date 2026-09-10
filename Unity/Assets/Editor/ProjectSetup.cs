using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ProjectSetup
{
    private const string ScenePath = "Assets/Scenes/Bootstrap.unity";

    public static void Create()
    {
        PlayerSettings.companyName = "OctOpus";
        PlayerSettings.productName = "OctOpus";
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultScreenWidth = 960;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Server, ScriptingImplementation.Mono2x);
        EditorSettings.serializationMode = SerializationMode.ForceText;
        UnityEditor.VersionControlSettings.mode = "Visible Meta Files";
        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrap").AddComponent<ClientBootstrap>();
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(8, 10, -12);
            cameraObject.transform.LookAt(Vector3.zero);
            camera.orthographic = true;
            camera.orthographicSize = 7;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.17f, 0.22f);
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.gray;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Test Ground";

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, false, "../Builds/WindowsClient/OctOpus.exe");
    }

    private static void Build(BuildTarget target, bool server, string output)
    {
        Create();
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            target = target,
            subtarget = (int)(server ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player),
            locationPathName = output,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Build failed: " + report.summary.result);
        Debug.Log("[OctOpus] Build succeeded: " + Path.GetFullPath(output));
    }
}
