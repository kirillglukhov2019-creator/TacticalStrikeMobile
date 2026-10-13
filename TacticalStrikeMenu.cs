using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TacticalStrikeMenu
{
    const string ScenePath = "Assets/Scenes/Demo.unity";

    [MenuItem("Tactical Strike/Create Demo Scene")]
    public static void CreateDemoScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Tactical Strike Bootstrap").AddComponent<TacticalStrikeBootstrap>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorUtility.DisplayDialog("Tactical Strike", "Демо-сцена создана. Нажми Play для запуска.", "OK");
    }

    [MenuItem("Tactical Strike/Build Android APK")]
    public static void BuildAndroidApk()
    {
        if (!File.Exists(ScenePath))
        {
            CreateDemoScene();
            if (!File.Exists(ScenePath)) return;
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            EditorUtility.DisplayDialog(
                "Не установлен Android Build Support",
                "Открой Unity Hub → Installs → меню Unity → Add modules и установи Android Build Support, Android SDK & NDK Tools и OpenJDK.",
                "OK");
            return;
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "Tactical Strike Mobile";
        PlayerSettings.companyName = "Indie Prototype";
        PlayerSettings.applicationIdentifier = "com.indie.tacticalstrikemobile";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = MinimumAndroidSdk();
        PlayerSettings.Android.forceInternetPermission = false;

        string outputDir = Path.GetFullPath("Build");
        Directory.CreateDirectory(outputDir);
        string outputPath = Path.Combine(outputDir, "TacticalStrike.apk");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Готово!", "APK создан:\n" + outputPath, "Открыть папку");
            EditorUtility.RevealInFinder(outputPath);
        }
        else
        {
            EditorUtility.DisplayDialog("Сборка не завершена",
                "Unity не смогла собрать APK. Проверь Console и установку Android Build Support.",
                "OK");
        }
    }

    static AndroidSdkVersions MinimumAndroidSdk()
    {
        return AndroidSdkVersions.AndroidApiLevel23;
    }
}
