using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;

public class WinGameController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private CanvasGroup backgroundWin;
    [SerializeField] private starFxController vfxStar;
    [SerializeField] private GameObject vfxObj;
    [SerializeField] private ParticleSystem vfxPS;
    

    private VisualElement overlay;
    private VisualElement popupContainer;
    
    private Button nextButton;
    private VisualElement loadingIndicator;

    public event Action OnNextAction = ActionUtility.EmptyAction.Instance;
    public event Action OnClosePopup = ActionUtility.EmptyAction.Instance;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        overlay = root.Q<VisualElement>("Overlay");
        popupContainer = root.Q<VisualElement>("PopupContainer");
        
        nextButton = root.Q<Button>("NextButton");
        loadingIndicator = root.Q<VisualElement>("LoadingIndicator");

        nextButton.clicked += OnNextClicked;
        overlay.style.display = DisplayStyle.None;
        SetLoadingState(false);
    }

    private void OnDisable()
    {
        nextButton.clicked -= OnNextClicked;
    }

    public void ShowScreen()
    {
        SetLoadingState(false); // Reset trạng thái loading khi hiện popup
        ShowPopupAsync().Forget();
    }

    private async UniTaskVoid ShowPopupAsync()
    {
        overlay.style.display = DisplayStyle.Flex;
        
        if (backgroundWin != null)
        {
            backgroundWin.gameObject.SetActive(true);
            backgroundWin.alpha = 0f;
        }
        overlay.style.opacity = 0f;
        popupContainer.style.scale = new StyleScale(new Vector2(0.3f, 0.3f));
        V3.Component.SoundComponent.Instance?.PlaySFX("win");
        // Fade in lớp nền và Scale bật nảy popup đồng thời
        await UniTask.WhenAll(
            backgroundWin?.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).ToUniTask() ?? UniTask.CompletedTask,
            DOTween.To(() => overlay.style.opacity.value, x => overlay.style.opacity = x, 1f, 0.3f)
                   .SetEase(Ease.OutQuad).ToUniTask(),
                   
            DOTween.To(() => popupContainer.style.scale.value.value.x, 
                       x => popupContainer.style.scale = new StyleScale(new Vector2(x, x)), 1f, 0.4f)
                   .SetEase(Ease.OutBack).ToUniTask() 
        );

        ShowVFX();
    }

    private void OnNextClicked()
    {
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        Debug.Log("Next game...");
        OnNextAction?.Invoke();
    }

    public async UniTaskVoid HidePopupAsync()
    {
        HideVFX();
        nextButton.SetEnabled(false);

        await UniTask.WhenAll(
            backgroundWin?.DOFade(0f, 0.2f).SetEase(Ease.InQuad).ToUniTask() ?? UniTask.CompletedTask,
            DOTween.To(() => overlay.style.opacity.value, x => overlay.style.opacity = x, 0f, 0.2f)
                   .SetEase(Ease.InQuad).ToUniTask(),
                   
            DOTween.To(() => popupContainer.style.scale.value.value.x, 
                       x => popupContainer.style.scale = new StyleScale(new Vector2(x, x)), 0.5f, 0.2f)
                   .SetEase(Ease.InBack).ToUniTask()
        );

        if (backgroundWin != null)
        {
            backgroundWin.gameObject.SetActive(false);
        }
        overlay.style.display = DisplayStyle.None;
        nextButton.SetEnabled(true);

        OnClosePopup();
    }

    public void SetLoadingState(bool isLoading)
    {
        if (nextButton != null) {
            nextButton.style.display = isLoading ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (loadingIndicator != null)
        {
            loadingIndicator.style.display = isLoading ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void ShowVFX()
    {
        if (vfxStar != null)
        {
            vfxStar.gameObject.SetActive(true);
            vfxStar.Reset();
        }

        if (vfxObj != null)
        {
            vfxObj.gameObject.SetActive(true);
            vfxPS.Play();
        }
    }

    private void HideVFX()
    {
        vfxStar.gameObject.SetActive(false);
        if (vfxObj != null)
        {
            vfxObj.gameObject.SetActive(false);
        }
    }
}