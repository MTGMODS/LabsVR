using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Allows the socket to accept only the designated sample object.</summary>
public sealed class Lab7SocketTagFilter : MonoBehaviour, IXRSelectFilter
{
    [SerializeField] string requiredTag = "SocketItem";

    public bool canProcess => isActiveAndEnabled;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactable == null)
            return false;

        var target = interactable.transform;
        return target != null && target.CompareTag(requiredTag);
    }
}
