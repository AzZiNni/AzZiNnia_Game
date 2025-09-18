using UnityEngine;

namespace Azurin.Core.Configs
{
    /// <summary>
    /// Holds references to input assets and default bindings if needed.
    /// </summary>
    [CreateAssetMenu(menuName = "Azurin/Configs/InputConfig", fileName = "InputConfig")]
    public class InputConfig : ScriptableObject
    {
        [Tooltip("Optional reference to the PlayerControls input actions asset")]
        public TextAsset inputActionsJson;
    }
}


