using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>Visible response for a ray-activated demonstration target.</summary>
public sealed class Lab7RayFeedback : MonoBehaviour
{
    public enum Response { CycleColor, ToggleBeacon, Pulse }

    static readonly Color PulseColor = new Color(1f, 0.05f, 0.8f, 1f);

    [SerializeField] Response response;
    [SerializeField] Renderer targetRenderer;
    [SerializeField] Light beacon;
    [SerializeField] Color[] colors = { new Color(0.12f, 0.75f, 1f), new Color(1f, 0.55f, 0.12f), new Color(0.3f, 1f, 0.45f) };

    Vector3 initialScale;
    Color initialColor;
    int colorIndex = -1;
    bool beaconOn;
    bool pulsed;

    void Awake()
    {
        initialScale = transform.localScale;
        if (targetRenderer != null)
        {
            initialColor = targetRenderer.material.color;
            if (response == Response.Pulse)
                targetRenderer.material.color = PulseColor;
        }
        if (beacon != null)
            beaconOn = beacon.enabled;
    }

    /// <summary>Assigned to XR Simple Interactable's Select Entered event.</summary>
    public void Activate(SelectEnterEventArgs _)
    {
        switch (response)
        {
            case Response.CycleColor:
                if (targetRenderer == null || colors == null || colors.Length == 0) return;
                colorIndex = (colorIndex + 1) % colors.Length;
                targetRenderer.material.color = colors[colorIndex];
                break;
            case Response.ToggleBeacon:
                if (beacon == null) return;
                beaconOn = !beaconOn;
                beacon.enabled = beaconOn;
                if (targetRenderer != null)
                    targetRenderer.material.color = beaconOn ? new Color(1f, 0.25f, 0.02f, 1f) : initialColor;
                break;
            case Response.Pulse:
                pulsed = !pulsed;
                transform.localScale = initialScale * (pulsed ? 1.5f : 1f);
                break;
        }
    }
}
