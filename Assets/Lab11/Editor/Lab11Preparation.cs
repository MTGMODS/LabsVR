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
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

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
        controller.ApplyVideoRequirements();
        var menu = controller.GetComponentsInChildren<Canvas>(true).Single(x => x.name.StartsWith("01 •"));
        menu.transform.SetPositionAndRotation(new Vector3(origin.transform.position.x, 1.5f, origin.transform.position.z + 2f), Quaternion.identity);

        controller.SetClickSound(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Lab10/Audio/03_Building_chime_085s.wav"));
        var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>();
        if (eventSystems.Length != 1 || eventSystems[0].GetComponent<XRUIInputModule>() == null)
            throw new InvalidOperationException("Потрібен один EventSystem з XR UI Input Module.");
        foreach (var ray in UnityEngine.Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include))
            if (!ray.name.Contains("Teleport"))
            {
                ray.enableUIInteraction = true;
                EditorUtility.SetDirty(ray);
                PrefabUtility.RecordPrefabInstancePropertyModifications(ray);
                var visual = ray.GetComponent<XRInteractorLineVisual>();
                if (visual != null)
                {
                    EditorUtility.SetDirty(visual);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                }
            }
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
        var rect = (RectTransform)menu.transform;
        if (Vector2.Distance(rect.sizeDelta, new Vector2(800, 600)) > .01f ||
            Vector3.Distance(rect.localScale, Vector3.one * .001f) > .00001f ||
            Mathf.Abs(menu.transform.position.y - 1.5f) > .01f ||
            Mathf.Abs(menu.transform.position.z - origin.transform.position.z - 2f) > .01f)
            throw new InvalidOperationException("Розмір або розташування головного меню не відповідає вимогам.");
        foreach (var ray in UnityEngine.Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include))
            if (!ray.name.Contains("Teleport") && (!ray.enableUIInteraction || Mathf.Abs(ray.maxRaycastDistance - 10f) > .01f))
                throw new InvalidOperationException("Перевірте UI-промінь: " + ray.name);
        File.WriteAllText("Temp/Lab11UnityValidation.txt", "PASS: 3 World Space Canvas, 5 wired buttons, XR UI Input Module.\nPASS: Main menu 800x600, scale 0.001 = 0.8x0.6 m, height 1.5 m, distance 2 m.\nPASS: UI rays enabled, distance 10 m.\nRuntime interaction test required.\n");
        Debug.Log("Lab11: інтерфейс збережено, статична перевірка пройшла.");
    }
}
