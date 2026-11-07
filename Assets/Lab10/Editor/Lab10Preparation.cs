using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Виконується лише вручну. Не перемикає сцену та не запускає звук після компіляції.
public static class Lab10Preparation
{
    const string ScenePath = "Assets/Scenes/Lab10_SpatialAudio.unity";
    const string AudioFolder = "Assets/Lab10/Audio/";

    [MenuItem("Tools/Lab 10/Finalize prepared audio scene")]
    public static void FinalizeScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Спочатку зупиніть Play Mode.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureImport("01_Plaza_ambience_24s.wav", AudioClipLoadType.Streaming, .75f);
        ConfigureImport("02_Fountain_water_6s.wav", AudioClipLoadType.CompressedInMemory, .8f);
        ConfigureImport("03_Building_chime_085s.wav", AudioClipLoadType.DecompressOnLoad, .85f);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        // Зберігаємо через API Unity також посилання на кліпи після міграції серіалізації AudioSource.
        var sources = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<AudioSource>(true)).ToArray();
        foreach (var source in sources)
        {
            string file = source.name.StartsWith("01 • Фон площі") ? "01_Plaza_ambience_24s.wav"
                : source.name.StartsWith("Audio Source • Water") ? "02_Fountain_water_6s.wav"
                : source.name.StartsWith("Audio Source • Chime") ? "03_Building_chime_085s.wav" : null;
            if (file == null) continue;
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + file);
            source.spatialBlend = file.StartsWith("01_") ? 0f : 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = 0f;
            source.spread = 0f;
            source.reverbZoneMix = 0f;
            source.playOnAwake = true;
            source.loop = !file.StartsWith("03_");
        }
        Camera camera = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Camera>(true))
            .Single(x => x.CompareTag("MainCamera") && x.gameObject.activeInHierarchy);
        var listeners = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<AudioListener>(true)).ToArray();
        foreach (var listener in listeners)
            if (listener.gameObject != camera.gameObject) UnityEngine.Object.DestroyImmediate(listener);
        AudioListener head = camera.GetComponent<AudioListener>();
        if (head == null) head = camera.gameObject.AddComponent<AudioListener>();
        head.enabled = true;
        foreach (var point in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Lab10PointAudio>(true)))
        {
            var serialized = new SerializedObject(point);
            serialized.FindProperty("listener").objectReferenceValue = camera.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        // Невеликі нові конструкції мають колізії, але не змінюють запечене GI Lab9.
        Transform group = scene.GetRootGameObjects().Single(x => x.name.StartsWith("10 • Просторовий звук")).transform;
        foreach (var renderer in group.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.name.Contains("Water") || renderer.GetComponent<Collider>() != null) continue;
            renderer.gameObject.AddComponent<BoxCollider>();
        }
        IsolateBakedLighting(scene);
        var existing = EditorBuildSettings.scenes.Where(x => x.path != ScenePath).ToList();
        existing.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = existing.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        ValidateScene();
    }

    static void ConfigureImport(string filename, AudioClipLoadType type, float quality)
    {
        var importer = AssetImporter.GetAtPath(AudioFolder + filename) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("Не імпортовано " + filename);
        importer.forceToMono = true;
        importer.loadInBackground = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = type;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = quality;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = type != AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    static void IsolateBakedLighting(Scene scene)
    {
        // Окрема копія LightingData: майбутнє запікання Lab10 не повинно змінити Lab9.
        const string folder = "Assets/Scenes/Lab10_SpatialAudio";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Scenes", "Lab10_SpatialAudio");
        if (Lightmapping.lightingDataAsset != null)
        {
            string source = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
            string target = folder + "/LightingData.asset";
            if (source != target)
            {
                if (AssetDatabase.LoadAssetAtPath<LightingDataAsset>(target) == null && !AssetDatabase.CopyAsset(source, target))
                    throw new IOException("Не вдалося скопіювати LightingData.");
                Lightmapping.lightingDataAsset = AssetDatabase.LoadAssetAtPath<LightingDataAsset>(target);
            }
        }
        if (Lightmapping.lightingSettings != null)
        {
            string source = AssetDatabase.GetAssetPath(Lightmapping.lightingSettings);
            const string target = "Assets/Lab10/Lab10_LightingSettings.lighting";
            if (source != target)
            {
                if (AssetDatabase.LoadAssetAtPath<LightingSettings>(target) == null && !AssetDatabase.CopyAsset(source, target))
                    throw new IOException("Не вдалося скопіювати Lighting Settings.");
                Lightmapping.lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(target);
            }
        }
    }

    [MenuItem("Tools/Lab 10/Validate current audio scene")]
    public static void ValidateScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Відкрийте Lab10_SpatialAudio.");
        var objects = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var listeners = objects.SelectMany(x => x.GetComponents<AudioListener>()).Where(x => x.enabled && x.gameObject.activeInHierarchy).ToArray();
        if (listeners.Length != 1 || listeners[0].GetComponent<Camera>() == null)
            throw new InvalidOperationException("Потрібен один Audio Listener на VR-камері.");
        if (objects.Any(x => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject) > 0))
            throw new InvalidOperationException("У сцені є Missing Script.");
        var audio = objects.SelectMany(x => x.GetComponents<AudioSource>()).ToArray();
        var ambient = audio.Single(x => x.name.StartsWith("01 • Фон площі"));
        if (ambient.clip == null || !ambient.loop || !ambient.playOnAwake || ambient.spatialBlend != 0f)
            throw new InvalidOperationException("Невірні налаштування фонового звуку.");
        var points = objects.SelectMany(x => x.GetComponents<Lab10PointAudio>()).Select(x => x.GetComponent<AudioSource>()).ToArray();
        if (points.Length < 2) throw new InvalidOperationException("Потрібно два 3D-джерела.");
        foreach (var p in points)
            if (p.clip == null || p.spatialBlend != 1f || p.rolloffMode != AudioRolloffMode.Logarithmic || p.minDistance < 1f || p.minDistance > 3f || p.maxDistance < 10f || p.maxDistance > 20f)
                throw new InvalidOperationException("Невірні налаштування " + p.name);
        foreach (var clip in audio.Select(x => x.clip).Where(x => x != null).Distinct())
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            var settings = importer.defaultSampleSettings;
            var expected = clip.length > 10f ? AudioClipLoadType.Streaming : clip.length >= 1f ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            if (!importer.forceToMono || settings.loadType != expected || settings.compressionFormat != AudioCompressionFormat.Vorbis || settings.quality < .7f || settings.quality > .9f)
                throw new InvalidOperationException("Невірний імпорт " + clip.name);
        }
        string report = "Lab10: static Unity validation PASSED\nAudio Listener: " + listeners[0].name + "\n";
        report += string.Join("\n", audio.Select(x => $"{x.name}: {x.clip?.length:F2} s, blend={x.spatialBlend}, volume={x.volume}, min={x.minDistance}, max={x.maxDistance}, rolloff={x.rolloffMode}"));
        report += "\nRuntime listening, stereo recording, locomotion and Console check still required.\n";
        File.WriteAllText("Temp/Lab10UnityValidation.txt", report);
        Debug.Log(report);
    }
}
