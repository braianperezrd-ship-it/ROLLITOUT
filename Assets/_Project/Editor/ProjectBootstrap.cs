using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-time setup that runs the first time the project is opened in Unity.
/// Creates the URP asset with the 2D Renderer, assigns it as the active pipeline,
/// and creates a Main scene with an orthographic camera and a global 2D light.
/// Every step is skipped if its asset already exists, so it is safe to leave in the project.
/// Re-run manually via Tools > ROLL IT OUT > Run Project Setup.
/// </summary>
[InitializeOnLoad]
public static class ProjectBootstrap
{
    const string SettingsFolder = "Assets/_Project/Settings";
    const string RendererPath = SettingsFolder + "/Renderer2D.asset";
    const string PipelinePath = SettingsFolder + "/URP-2D.asset";
    const string ScenePath = "Assets/_Project/Scenes/Main.unity";

    static ProjectBootstrap()
    {
        if (Application.isBatchMode)
            return;

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(PipelinePath) || !File.Exists(ScenePath) || GraphicsSettings.defaultRenderPipeline == null)
                Run();
        };
    }

    [MenuItem("Tools/ROLL IT OUT/Run Project Setup")]
    public static void Run()
    {
        var pipeline = EnsurePipeline();
        if (GraphicsSettings.defaultRenderPipeline != pipeline)
            GraphicsSettings.defaultRenderPipeline = pipeline;

        EnsureMainScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[ROLL IT OUT] Project setup complete: URP 2D renderer active, Main scene ready.");
    }

    static UniversalRenderPipelineAsset EnsurePipeline()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline != null)
            return pipeline;

        Directory.CreateDirectory(SettingsFolder);

        var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        pipeline = UniversalRenderPipelineAsset.Create(renderer);
        AssetDatabase.CreateAsset(pipeline, PipelinePath);
        return pipeline;
    }

    static void EnsureMainScene()
    {
        if (File.Exists(ScenePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
        cameraGo.AddComponent<AudioListener>();
        cameraGo.AddComponent<UniversalAdditionalCameraData>();

        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}
