using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;

public class LoseGameController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement overlay;
    private VisualElement popupContainer;
    
    private Button replayButton;
    private Button adButton;


    public event Action OnClosePopup = ActionUtility.EmptyAction.Instance;
    public event Action OnReplayAction = ActionUtility.EmptyAction.Instance;
    public event Action OnViewAdsAction = ActionUtility.EmptyAction.Instance;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        overlay = root.Q<VisualElement>("Overlay");
        popupContainer = root.Q<VisualElement>("PopupContainer");
        
        replayButton = root.Q<Button>("LoseGameReplayButton");
        adButton = root.Q<Button>("AdButton");

        // Gắn sự kiện click
        replayButton.clicked += OnReplayClicked;
        adButton.clicked += OnAdButtonClicked;

        // Ẩn ban đầu
        overlay.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        replayButton.clicked -= OnReplayClicked;
        adButton.clicked -= OnAdButtonClicked;
    }

    /// <summary>
    /// Gọi hàm này khi người chơi hết nước đi (Out of moves)
    /// </summary>
    public void ShowScreen()
    {
        ShowPopupAsync().Forget();
    }

    private async UniTaskVoid ShowPopupAsync()
    {
        overlay.style.display = DisplayStyle.Flex;
        
        // Đặt giá trị khởi tạo cho animation
        overlay.style.opacity = 0f;
        popupContainer.style.scale = new StyleScale(new Vector2(0.3f, 0.3f));
        V3.Component.SoundComponent.Instance?.PlaySFX("lose");
        // Fade in lớp nền và Scale bật nảy popup đồng thời
        await UniTask.WhenAll(
            DOTween.To(() => overlay.style.opacity.value, x => overlay.style.opacity = x, 1f, 0.3f)
                   .SetEase(Ease.OutQuad).ToUniTask(),
                   
            DOTween.To(() => popupContainer.style.scale.value.value.x, 
                       x => popupContainer.style.scale = new StyleScale(new Vector2(x, x)), 1f, 0.4f)
                   .SetEase(Ease.OutBack).ToUniTask() // Ease.OutBack tạo hiệu ứng nảy (bounce nhẹ)
        );
    }

    private void OnReplayClicked()
    {
        Debug.Log("Replay game...");
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        // TODO: Chạy logic load lại Scene hoặc Reset Board ở đây
        OnReplayAction();
        HidePopupAsync().Forget();
    }

    private void OnAdButtonClicked()
    {
        Debug.Log("Đang xem quảng cáo ...");
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        OnViewAdsAction();
        HidePopupAsync().Forget();
    }

    private async UniTaskVoid HidePopupAsync()
    {
        // Vô hiệu hóa nút bấm để tránh double-click trong lúc đang đóng
        replayButton.SetEnabled(false);
        adButton.SetEnabled(false);

        await UniTask.WhenAll(
            DOTween.To(() => overlay.style.opacity.value, x => overlay.style.opacity = x, 0f, 0.2f)
                   .SetEase(Ease.InQuad).ToUniTask(),
                   
            DOTween.To(() => popupContainer.style.scale.value.value.x, 
                       x => popupContainer.style.scale = new StyleScale(new Vector2(x, x)), 0.5f, 0.2f)
                   .SetEase(Ease.InBack).ToUniTask()
        );

        overlay.style.display = DisplayStyle.None;
        
        // Bật lại nút cho lần mở sau
        replayButton.SetEnabled(true);
        adButton.SetEnabled(true);

        OnClosePopup();
    }
}