using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using System;


public class LoadingController : MonoBehaviour
{
    public UIDocument uiDocument;
    public float loadingDuration = 5f; // Thời gian tải game là 5 giây
    public string mainMenuSceneName = "MainMenuScene"; // Tên scene menu chính

    private ProgressBar loadingBar;
    private Label loadingLabel;
    private string _text = "Loading";

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument is not assigned in LoadingController.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        loadingBar = root.Q<ProgressBar>("LoadingBar");
        loadingLabel = root.Q<Label>("LoadingText");


        if (loadingBar == null)
        {
            Debug.LogError("ProgressBar with name 'LoadingBar' not found in the UXML.");
            return;
        }

        V3.Component.SoundComponent.Instance?.PlayMusic("background");

        StartLoadingAnimation().Forget();
    }

    private async UniTaskVoid StartLoadingAnimation()
    {
        await AnimateLoadingBarAsync();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private async UniTask AnimateLoadingBarAsync()
    {
        await DOTween.To(() => loadingBar.value, x =>
                     {
                         loadingBar.value = x;
                         if (loadingLabel != null)
                         {
                             loadingLabel.text = $"{Mathf.FloorToInt(x)}%";
                         }
                     }, 100f, loadingDuration)
                     .SetEase(Ease.Linear)
                     .ToUniTask();
    }
}