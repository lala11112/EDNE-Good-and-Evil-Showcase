using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace EDNE.UI
{
    /// <summary>
    /// 공통 팝업 (확인/취소 다이얼로그)
    /// UIPanel을 상속받아 다이얼로그 기능 추가
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPopup : UIPanel
    {
        [Header("Popup UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private UIButton confirmButton;
        [SerializeField] private UIButton cancelButton;

        [Header("Popup Settings")]
        [SerializeField] private bool closeOnConfirm = true;
        [SerializeField] private bool closeOnCancel = true;

        private UnityAction onConfirmCallback;
        private UnityAction onCancelCallback;

        protected override void Awake()
        {
            base.Awake(); // UIPanel의 Awake 호출

            if (confirmButton != null)
                confirmButton.OnClick.AddListener(OnConfirm);

            if (cancelButton != null)
                cancelButton.OnClick.AddListener(OnCancel);
        }

        /// <summary>
        /// 팝업 표시
        /// </summary>
        public void Show(string title, string message, UnityAction onConfirm = null, UnityAction onCancel = null)
        {
            if (titleText != null)
                titleText.text = title;

            if (messageText != null)
                messageText.text = message;

            onConfirmCallback = onConfirm;
            onCancelCallback = onCancel;

            // UIPanel의 Open 메서드 호출
            Open();
        }

        /// <summary>
        /// 버튼 하나만 있는 알림 팝업
        /// </summary>
        public void ShowAlert(string title, string message, UnityAction onConfirm = null)
        {
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(false);

            Show(title, message, onConfirm);
        }

        /// <summary>
        /// 확인/취소 팝업
        /// </summary>
        public void ShowConfirm(string title, string message, UnityAction onConfirm, UnityAction onCancel = null)
        {
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(true);

            Show(title, message, onConfirm, onCancel);
        }

        private void OnConfirm()
        {
            onConfirmCallback?.Invoke();

            if (closeOnConfirm)
                Close(); // UIPanel의 Close 메서드
        }

        private void OnCancel()
        {
            onCancelCallback?.Invoke();

            if (closeOnCancel)
                Close(); // UIPanel의 Close 메서드
        }
    }
}


