using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Applies the lighting pipeline while the Lab9 scene is loaded.</summary>
[DisallowMultipleComponent]
public sealed class Lab9RenderSettings : MonoBehaviour
{
    [SerializeField] RenderPipelineAsset lightingPipeline;
    RenderPipelineAsset previousPipeline;
    bool applied;

    void OnEnable()
    {
        if (lightingPipeline == null || applied)
            return;

        previousPipeline = QualitySettings.renderPipeline;
        QualitySettings.renderPipeline = lightingPipeline;
        applied = true;
    }

    void OnDisable()
    {
        if (applied && QualitySettings.renderPipeline == lightingPipeline)
            QualitySettings.renderPipeline = previousPipeline;
        applied = false;
    }
}
