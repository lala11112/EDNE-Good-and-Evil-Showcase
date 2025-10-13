using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace EDNE.UI
{
    /// <summary>
    /// 공통 버튼 컴포넌트
    /// - 사운드 재생
    /// - 애니메이션
    /// - 일관된 동작
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour
    {
        [Header("References")]
        private Button button;
        [SerializeField] private TextMeshProUGUI buttonText;

        [Header("Settings")]
        [SerializeField] private string text = "Button";
        [SerializeField] private bool playSound = true;

        [Header("Events")]
        public UnityEvent OnClick;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
            
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }

        private void HandleClick()
        {
            if (playSound)
            {
                PlayClickSound();
            }

            OnClick?.Invoke();
        }

        public void SetText(string newText)
        {
            text = newText;
            if (buttonText != null)
            {
                buttonText.text = newText;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void PlayClickSound()
        {
            // AudioManager나 MMSoundManager 사용
            // MMSoundManager.PlaySound(buttonClickSound);
        }

        // Inspector에서 텍스트 변경 시 즉시 반영
        private void OnValidate()
        {
            if (buttonText != null && !string.IsNullOrEmpty(text))
            {
                buttonText.text = text;
            }
        }
    }
}

