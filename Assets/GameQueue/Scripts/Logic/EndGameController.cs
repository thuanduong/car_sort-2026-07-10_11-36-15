using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;

public class EndGameController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement overlay;
    private VisualElement popupContainer;
    
    private Button homeButton;

    public event Action OnHomeAction = ActionUtility.EmptyAction.Instance;
    public event Action OnClosePopup = ActionUtility.EmptyAction.Instance;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        overlay = root.Q<VisualElement>("EndGameOverlay");
        popupContainer = root.Q<VisualElement>("EndGamePopupContainer");
        
        homeButton = root.Q<Button>("EndGameHomeButton");

        homeButton.clicked += OnHomeClicked;
        overlay.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        homeButton.clicked -= OnHomeClicked;
    }

    public void ShowScreen()
    {
        ShowPopupAsync().Forget();
    }

    private async UniTaskVoid ShowPopupAsync()
    {
        overlay.style.display = DisplayStyle.Flex;
        overlay.style.opacity = 0f;
        popupContainer.style.scale = new StyleScale(new Vector2(0.3f, 0.3f));

        V3.Component.SoundComponent.Instance?.PlaySFX("win");

        // Fade in lớp nền và Scale bật nảy popup đồng thời
        await UniTask.WhenAll(
            DOTween.To(() => overlay.style.opacity.value, x => overlay.style.opacity = x, 1f, 0.3f)
                   .SetEase(Ease.OutQuad).ToUniTask(),
                   
            DOTween.To(() => popupContainer.style.scale.value.value.x, 
                       x => popupContainer.style.scale = new StyleScale(new Vector2(x, x)), 1f, 0.4f)
                   .SetEase(Ease.OutBack).ToUniTask() 
        );
    }

    private void OnHomeClicked()
    {
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        Debug.Log("Go Home...");
        HidePopupAsync().Forget();
        
    }

    private async UniTaskVoid HidePopupAsync()
    {
        homeButton.SetEnabled(false);

        await UniTask.WhenAll(
            DOTween.To(() => overlay.style.opacity.value, x => overlay.style.opacity = x, 0f, 0.2f)
                   .SetEase(Ease.InQuad).ToUniTask(),
                   
            DOTween.To(() => popupContainer.style.scale.value.value.x, 
                       x => popupContainer.style.scale = new StyleScale(new Vector2(x, x)), 0.5f, 0.2f)
                   .SetEase(Ease.InBack).ToUniTask()
        );

        overlay.style.display = DisplayStyle.None;
        homeButton.SetEnabled(true);

        OnHomeAction();
    }
}