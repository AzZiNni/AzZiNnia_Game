using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PauseUI : MonoBehaviour
    {
        [Header("UI Елементи")]
        [SerializeField] private Canvas pauseCanvas;
        public Button resumeButton;
        public Button mainMenuButton;
        
        void Awake()
        {
            if (pauseCanvas != null)
                pauseCanvas.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (pauseCanvas != null)
                pauseCanvas.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (pauseCanvas != null)
                pauseCanvas.gameObject.SetActive(false);
        }
    }
}