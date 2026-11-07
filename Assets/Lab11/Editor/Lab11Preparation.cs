using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Лише ручний запуск: не відкриває Unity та не перехоплює керування під час імпорту.
public static class Lab11Preparation
{
    const string Path = "Assets/Scenes/Lab11_WorldSpaceUI.unity";
    [MenuItem("Tools/Lab 11/Finalize prepared interface %&F12")]
    public static void FinalizeInterface()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Зупиніть Play Mode.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Single);
        var controller = UnityEngine.Object.FindAnyObjectByType<Lab11Interface>();
        if (controller == null) throw new InvalidOperationException("Немає Lab11Interface.");
        var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
        if (origin == null) throw new InvalidOperationException("Немає XR Origin.");
        origin.name = "XR Origin • Lab11";
        origin.transform.SetPositionAndRotation(new Vector3(0f, .1f, -38f), Quaternion.identity);
        controller.CreateInterface();
        var menu = controller.GetComponentsInChildren<Canvas>(true).Single(x => x.name.StartsWith("01 •"));
        menu.transform.SetPositionAndRotation(origin.Camera.transform.position + Vector3.forward * 2.2f, Quaternion.identity);

        controller.SetClickSound(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Lab10/Audio/03_Building_chime_085s.wav"));
        var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>();
        if (eventSystems.Length != 1 || eventSystems[0].GetComponent<XRUIInputModule>() == null)
            throw new InvalidOperationException("Потрібен один EventSystem з XR UI Input Module.");
        foreach (var ray in UnityEngine.Object.FindObjectsByType<XRRayInteractor>())
            if (!ray.name.Contains("Teleport")) { ray.enableUIInteraction = true; EditorUtility.SetDirty(ray); }
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Lab11_WorldSpaceUI"))
            AssetDatabase.CreateFolder("Assets/Scenes", "Lab11_WorldSpaceUI");
        if (Lightmapping.lightingDataAsset != null)
        {
            const string target = "Assets/Scenes/Lab11_WorldSpaceUI/LightingData.asset";
            string source = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
            if (source != target)
            {
                if (AssetDatabase.LoadAssetAtPath<LightingDataAsset>(target) == null && !AssetDatabase.CopyAsset(source, target))
                    throw new IOException("Не вдалося скопіювати освітлення.");
                Lightmapping.lightingDataAsset = AssetDatabase.LoadAssetAtPath<LightingDataAsset>(target);
            }
        }
        if (Lightmapping.lightingSettings != null)
        {
            const string target = "Assets/Lab11/Lab11_LightingSettings.lighting";
            string source = AssetDatabase.GetAssetPath(Lightmapping.lightingSettings);
            if (source != target)
            {
                if (AssetDatabase.LoadAssetAtPath<LightingSettings>(target) == null && !AssetDatabase.CopyAsset(source, target))
                    throw new IOException("Не вдалося скопіювати параметри освітлення.");
                Lightmapping.lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(target);
            }
        }
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Path, true) }
            .Concat(EditorBuildSettings.scenes.Where(x => x.path != Path)).ToArray();
        AssetDatabase.SaveAssets();
        var canvases = controller.GetComponentsInChildren<Canvas>(true);
        var buttons = controller.GetComponentsInChildren<Button>(true);
        if (canvases.Length != 3 || buttons.Length != 5 || canvases.Any(x => x.renderMode != RenderMode.WorldSpace || x.GetComponent<TrackedDeviceGraphicRaycaster>() == null))
            throw new InvalidOperationException("Перевірте 3 World Space Canvas та 5 кнопок.");
        foreach (var button in buttons)
            if (button.onClick.GetPersistentEventCount() != 1)
                throw new InvalidOperationException("Подію не підключено: " + button.name);
        File.WriteAllText("Temp/Lab11UnityValidation.txt", "PASS: 3 World Space Canvas, 5 wired buttons, XR UI Input Module.\nRuntime interaction test required.\n");
        Debug.Log("Lab11: інтерфейс збережено, статична перевірка пройшла.");
    }
}
