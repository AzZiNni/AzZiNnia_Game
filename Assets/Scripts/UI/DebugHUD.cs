using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Simple HUD that shows debug info; expects references via inspector.
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private TextMeshProUGUI buildModeText;

        public void SetDebug(string text)
        {
            if (debugText != null) debugText.text = text;
        }

        public void SetBuildMode(bool isBuild)
        {
            if (buildModeText == null) return;
            buildModeText.text = isBuild ? "Режим: Будівництво" : "Режим: Копання";
            buildModeText.gameObject.SetActive(true);
        }
    }
}


