using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;

public class PopupSettingController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement popupOverlay;
    private VisualElement popupContainer;
    
    private Toggle musicToggle;
    private Toggle soundToggle;
    
    private Button continueButton;
    private Button replayButton;
    private Button homeButton;
    private Button backButton;

    public event Action OnClosePopup = ActionUtility.EmptyAction.Instance;
    public event Action OnReplayGame = ActionUtility.EmptyAction.Instance;
    public event Action OnGoHome = ActionUtility.EmptyAction.Instance;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        popupOverlay = root.Q<VisualElement>("PopupOverlay");
        popupContainer = root.Q<VisualElement>("PopupContainer");
        
        musicToggle = root.Q<Toggle>("MusicToggle");
        soundToggle = root.Q<Toggle>("SoundToggle");
        
        continueButton = root.Q<Button>("ContinueButton");
        replayButton = root.Q<Button>("ReplayButton");
        homeButton = root.Q<Button>("HomeButton");
        backButton = root.Q<Button>("BackButton");
        backButton.clicked += OnBackOrContinueClicked;
        continueButton.clicked += OnBackOrContinueClicked;
        replayButton.clicked += OnReplayClicked;
        homeButton.clicked += OnHomeClicked;
        musicToggle.RegisterValueChangedCallback(evt => OnMusicToggled(evt.newValue));
        soundToggle.RegisterValueChangedCallback(evt => OnSoundToggled(evt.newValue));

        // Mặc định ẩn hoàn toàn Overlay lúc khởi tạo để chờ lệnh mở
        popupOverlay.style.display = DisplayStyle.None;

        musicToggle.value = !V3.Component.SoundComponent.Instance.IsMuteMusic();
        soundToggle.value = !V3.Component.SoundComponent.Instance.IsMuteSFX();
    }

    private void OnDisable()
    {
        backButton.clicked -= OnBackOrContinueClicked;
        continueButton.clicked -= OnBackOrContinueClicked;
        replayButton.clicked -= OnReplayClicked;
        homeButton.clicked -= OnHomeClicked;
        
        musicToggle.UnregisterAllRemovableCallbacks();
        soundToggle.UnregisterAllRemovableCallbacks();

    }

    public void OpenPopup(bool showGameplayButtons)
    {
        Debug.Log($"OpenPopup: {showGameplayButtons}");
        ToggleContextualButton(continueButton, showGameplayButtons);
        ToggleContextualButton(replayButton, showGameplayButtons);
        ToggleContextualButton(homeButton, showGameplayButtons);
        ToggleContextualButton(backButton, !showGameplayButtons);
        OpenPopupAsync().Forget();
    }

    private void ToggleContextualButton(Button button, bool visible)
    {
        if (visible)
            button.RemoveFromClassList("hidden-button");
        else
            button.AddToClassList("hidden-button");
    }

    private void OnBackOrContinueClicked()
    {
        CloseAsync().Forget();
    }

    private void OnReplayClicked()
    {
        ReplayGameAsync().Forget();
    }

    private void OnHomeClicked()
    {
        GoHomeAsync().Forget();
    }
    

    private async UniTaskVoid OpenPopupAsync()
    {
        popupOverlay.style.display = DisplayStyle.Flex;
        popupOverlay.style.opacity = 0f;
        popupContainer.style.scale = new StyleScale(new Vector2(0.5f, 0.5f));
        await UniTask.WhenAll(
            DOTween.To(() => popupOverlay.style.opacity.value, x => popupOverlay.style.opacity = x, 1f, 0.3f)
                .SetEase(Ease.OutQuad).ToUniTask(),
                
            DOTween.To(() => popupContainer.style.scale.value.value.x, 
                       x => popupContainer.style.scale = new StyleScale(new Vector2(x, x)), 1f, 0.35f)
                .SetEase(Ease.OutBack).ToUniTask() 
        );
    }

    public async UniTask ClosePopupAsync()
    {
        await UniTask.WhenAll(
            DOTween.To(() => popupOverlay.style.opacity.value, x => popupOverlay.style.opacity = x, 0f, 0.25f)
                .SetEase(Ease.InQuad).ToUniTask(),
                
            DOTween.To(() => popupContainer.style.scale.value.value.x, 
                       x => popupContainer.style.scale = new StyleScale(new Vector2(x, x)), 0.6f, 0.25f)
                .SetEase(Ease.InQuad).ToUniTask()
        );

        popupOverlay.style.display = DisplayStyle.None;
        OnClosePopup?.Invoke();
    }

    public async UniTaskVoid CloseAsync()
    {
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        await ClosePopupAsync();
    }

    public async UniTaskVoid ReplayGameAsync()
    {
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        await ClosePopupAsync();
        OnReplayGame.Invoke();
    }

    public async UniTaskVoid GoHomeAsync()
    {
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        await ClosePopupAsync();
        OnGoHome?.Invoke();
    }

    private void OnMusicToggled(bool value)
    {
        Debug.Log($"Trạng thái nhạc nền: {value}");
        V3.Component.SoundComponent.Instance.MuteMusic(!value);
    }

    private void OnSoundToggled(bool value)
    {
        Debug.Log($"Trạng thái hiệu ứng âm thanh: {value}");
        V3.Component.SoundComponent.Instance.MuteSFX(!value);
    }


}