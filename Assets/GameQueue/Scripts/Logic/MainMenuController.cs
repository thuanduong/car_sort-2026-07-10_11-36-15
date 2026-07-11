using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public UIDocument uiDocument;
    [SerializeField] private PopupSettingController _popupSettingController;

    public string levelMapSceneName = "LevelScene"; // Tên scene bản đồ level

    private Button btnPlay;
    private Button settingsButton;
    private VisualElement popupSettingInstance;

    void Start()
    {
        GameGlobal.ReloadGameValue();
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument is not assigned in MainMenuController.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        btnPlay = root.Q<Button>("PlayButton"); 
        settingsButton = root.Q<Button>("SettingsButton");

        popupSettingInstance = root.Q<VisualElement>("PopupSettingInstance");
        popupSettingInstance?.AddToClassList("popup-closed");
        _popupSettingController.OnClosePopup += OnPopupBackClicked;

        if (btnPlay == null)
        {
            Debug.LogError("Button with name 'PlayButton' not found in the UXML.");
            return;
        }

        if (settingsButton != null)
        {
            settingsButton.clicked += OnSettingsButtonClicked;
        }

        btnPlay.clicked += StartGame;
    }

    private void OnDisable()
    {
        _popupSettingController.OnClosePopup -= OnPopupBackClicked;

        if (settingsButton != null)
        {
            settingsButton.clicked -= OnSettingsButtonClicked;
        }

    }

    private void StartGame()
    {
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        SceneManager.LoadScene(levelMapSceneName);
    }

    private void OnSettingsButtonClicked()
    {
        V3.Component.SoundComponent.Instance?.PlaySFX("click");

        popupSettingInstance?.RemoveFromClassList("popup-closed");
        popupSettingInstance?.AddToClassList("popup-overlay");
        if (_popupSettingController != null)
        {
            _popupSettingController.OpenPopup(false);
        }
    }

    private void OnPopupBackClicked()
    {
        popupSettingInstance?.AddToClassList("popup-closed");
        popupSettingInstance?.RemoveFromClassList("popup-overlay");
    }
}