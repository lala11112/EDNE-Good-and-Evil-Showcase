using UnityEngine;
using System.Collections;

namespace EDNE.UI
{
    /// <summary>
    /// 공통 패널 컴포넌트
    /// - 열기/닫기 애니메이션
    /// - 페이드 효과
    /// - ESC 키 닫기
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private bool fadeAnimation = true;
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("References")]
        protected CanvasGroup canvasGroup;

        protected bool isOpen = false;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (closeOnEscape && isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        /// <summary>
        /// 패널 열기
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            isOpen = true;

            if (fadeAnimation)
            {
                StartCoroutine(FadeIn());
            }
            else
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void Close()
        {
            isOpen = false;

            if (fadeAnimation)
            {
                StartCoroutine(FadeOut());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 토글
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            canvasGroup.interactable = false;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 즉시 열기 (애니메이션 없이)
        /// </summary>
        public void OpenImmediate()
        {
            gameObject.SetActive(true);
            isOpen = true;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// 즉시 닫기 (애니메이션 없이)
        /// </summary>
        public void CloseImmediate()
        {
            isOpen = false;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}

