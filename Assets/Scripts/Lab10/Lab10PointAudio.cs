using UnityEngine;

/// <summary>Залишає стандартне 3D-затухання та плавно прибирає звук на Max Distance.</summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class Lab10PointAudio : MonoBehaviour
{
    [Tooltip("Камера VR-гравця. Якщо поле порожнє, використовується Main Camera.")]
    [SerializeField] Transform listener;
    [Tooltip("Ширина плавного приглушення перед Max Distance, у метрах.")]
    [SerializeField, Min(0.1f)] float fadeDistance = 2f;
    [Tooltip("0 — без повторних запусків; для короткого дзвону — інтервал у секундах.")]
    [SerializeField, Min(0f)] float repeatInterval = 0f;
    AudioSource source;
    float configuredVolume;
    float nextPlay;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        configuredVolume = source.volume;
        nextPlay = Time.time + repeatInterval;
        if (listener == null && Camera.main != null)
            listener = Camera.main.transform;
        UpdateDistance();
    }

    void Update()
    {
        UpdateDistance();
        if (repeatInterval > 0f && Time.time >= nextPlay)
        {
            nextPlay = Time.time + Mathf.Max(repeatInterval, source.clip != null ? source.clip.length : 0f);
            if (source.volume > 0f && !source.isPlaying && source.clip != null)
                source.Play();
        }
    }

    void UpdateDistance()
    {
        if (listener == null)
        {
            if (Camera.main != null) listener = Camera.main.transform;
            else return;
        }
        float distance = Vector3.Distance(transform.position, listener.position);
        float width = Mathf.Min(fadeDistance, source.maxDistance - source.minDistance);
        float fade = Mathf.InverseLerp(source.maxDistance, source.maxDistance - Mathf.Max(0.01f, width), distance);
        source.volume = configuredVolume * Mathf.SmoothStep(0f, 1f, fade);
    }

    void OnDisable()
    {
        if (source != null) source.volume = configuredVolume;
    }
}
