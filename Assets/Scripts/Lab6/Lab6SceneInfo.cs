using UnityEngine;

namespace LabsVR
{
    /// <summary>References used to explain the scene during the laboratory defence.</summary>
    public sealed class Lab6SceneInfo : MonoBehaviour
    {
        [TextArea(3, 6)] public string description;
        public Transform[] buildings;
        public Material[] materialExamples;
        public Transform playerStart;
    }
}
