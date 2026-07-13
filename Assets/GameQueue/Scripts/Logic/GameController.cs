using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;

public class GameController : MonoBehaviour
{
    public UIDocument uiDocument;
    [SerializeField] private PopupSettingController _popupSettingController;
    [SerializeField] private LoseGameController _loseGameController;
    [SerializeField] private WinGameController _winGameController;
    [SerializeField] private EndGameController _endGameController; 
    [SerializeField] private GameSkillController _gameSkillController;
    [SerializeField] private EcsGameBootstrap _ecsBootstrap;
    

    private VisualElement popupSettingInstance;
    private VisualElement loseGameInstance;
    private VisualElement winGameInstance;
    private VisualElement endGameInstance;
    private VisualElement gameSkillInstance;
    private VisualElement gameInstance;
    private Button settingsButton, quickPassButton;

    private Label levelLabel, moveLabel;

    public int CurrentLevel { get; private set;}
    public int CurrentMove {get; private set;}

    public Func<UniTask> OnRestartGame = () => UniTask.CompletedTask;
    

    void Awake()
    {
        GameGlobal.ReloadGameValue();
        Main.Instance?.UICamera.gameObject.SetActive(false);
    }
    void OnDestroy()
    {
        Main.Instance?.UICamera.gameObject.SetActive(true);
    }
    
    void OnEnable()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        CurrentLevel = GameGlobal.SelectedLevel;

        gameInstance = root.Q<VisualElement>("GameUI");

        settingsButton = root.Q<Button>("SettingsButton");
        if (settingsButton != null)
        {
            settingsButton.clicked += OnSettingsButtonClicked;
        }

        quickPassButton = root.Q<Button>("QuickPass");
        if (quickPassButton != null)
        {
            quickPassButton.clicked += OnQuickPassClicked;
        }

        levelLabel = root.Q<Label>("level");
        moveLabel = root.Q<Label>("move");

        //setting
        popupSettingInstance = root.Q<VisualElement>("PopupSettingInstance");
        popupSettingInstance?.AddToClassList("popup-closed");
        _popupSettingController.OnClosePopup += OnPopupBackClicked;
        _popupSettingController.OnReplayGame += OnPopupBackReplayClicked;
        _popupSettingController.OnGoHome += OnEndGameGoHome;

        //lose game
        loseGameInstance = root.Q<VisualElement>("LoseGameInstance");
        loseGameInstance?.AddToClassList("popup-closed");
        _loseGameController.OnClosePopup += OnCloseLoseGame;
        _loseGameController.OnReplayAction += OnLoseGameReplay;
        _loseGameController.OnViewAdsAction += OnLoseGameWatchAds;
        //win game
        winGameInstance = root.Q<VisualElement>("WinGameInstance");
        winGameInstance?.AddToClassList("popup-closed");
        _winGameController.OnClosePopup += OnCloseWinGame;
        _winGameController.OnNextAction += OnWinGameNextLevel;
        //end game
        endGameInstance = root.Q<VisualElement>("EndGameInstance");
        endGameInstance?.AddToClassList("popup-closed");
        // _endGameController.OnClosePopup += OnCloseEndGame;
        _endGameController.OnHomeAction += OnEndGameGoHome;

        //skill
        gameSkillInstance = root.Q<VisualElement>("GameSkillInstance");
        gameSkillInstance?.AddToClassList("popup-closed");


        if (levelLabel != null)
        {
            levelLabel.text = $"{CurrentLevel}";
        }
        if (moveLabel != null)
        {
            moveLabel.text = $"{CurrentMove}";
        }

        ShowSkillPanel();
        
    }

    public void RestartGame()
    {
        showGameUI();
        ShowSkillPanel();
    }


    public void UpdateMove(int value)
    {
        CurrentMove = value;
        if (moveLabel != null)
        {
            moveLabel.text = $"{CurrentMove}";
        }
    }

    public void UpdateLevel()
    {
        if (levelLabel != null)
        {
            levelLabel.text = $"{CurrentLevel}";
        }
    }

    void OnDisable()
    {
        if (settingsButton != null)
        {
            settingsButton.clicked -= OnSettingsButtonClicked;
        }

        //setting
        if (_popupSettingController != null) {
            _popupSettingController.OnClosePopup -= OnPopupBackClicked;
            _popupSettingController.OnReplayGame -= OnPopupBackReplayClicked;
            _popupSettingController.OnGoHome -= OnEndGameGoHome;
        }

        //lose game
        if (_loseGameController != null) {
            _loseGameController.OnClosePopup -= OnCloseLoseGame;
            _loseGameController.OnReplayAction -= OnLoseGameReplay;
            _loseGameController.OnViewAdsAction -= OnLoseGameWatchAds;
        }
        //win game
        if (_winGameController != null) {
            _winGameController.OnClosePopup -= OnCloseWinGame;
            _winGameController.OnNextAction -= OnWinGameNextLevel;
        }
        //end game
        if (_endGameController != null) {
            _endGameController.OnClosePopup -= OnCloseEndGame;
            _endGameController.OnHomeAction -= OnEndGameGoHome;
        }
    }

    public async void FinishGame(bool isCompleted)
    {
        hideGameUI();

        if (isCompleted)
        {
            _ecsBootstrap.IsUIBlockingInput = true; // Khóa input ngay khi thắng
            await UniTask.Delay(TimeSpan.FromSeconds(3)); // Chờ 3 giây

            bool kq = GameGlobal.CompleteLevel(GameGlobal.SelectedLevel);
            OnOpenWinGame();
            if (kq)
                _gameSkillController.OnWinGameResultSkill();
        }
        else
        {
            OnOpenLoseGame();
        }
    }
    
    #region  UI Toolkit
    private void OnSettingsButtonClicked()
    {
        _ecsBootstrap.IsUIBlockingInput = true;
        if (popupSettingInstance != null) {
            popupSettingInstance.RemoveFromClassList("popup-closed");
            popupSettingInstance.AddToClassList("popup-overlay");
        }
        if (_popupSettingController != null)
        {
            _popupSettingController.OpenPopup(true);
        }
    }
    private void OnPopupBackClicked()
    {
        _ecsBootstrap.IsUIBlockingInput = false;
        if (popupSettingInstance != null) {
            popupSettingInstance.AddToClassList("popup-closed");
            popupSettingInstance.RemoveFromClassList("popup-overlay");
        }
    }

    private void OnPopupBackReplayClicked()
    {
        OnPopupBackClicked();
        OnRestartGame();
    }

    private void OnOpenLoseGame()
    {
        _ecsBootstrap.IsUIBlockingInput = true;
        if (loseGameInstance != null) {
            loseGameInstance.RemoveFromClassList("popup-closed");
            loseGameInstance.AddToClassList("popup-overlay");
        }
        if (_loseGameController != null)
        {
            _loseGameController.ShowScreen();
        }
    }
    private void OnCloseLoseGame()
    {
        _ecsBootstrap.IsUIBlockingInput = false;
        if (loseGameInstance != null) {
            loseGameInstance.AddToClassList("popup-closed");
            loseGameInstance.RemoveFromClassList("popup-overlay");
        }
    }
    private void OnLoseGameReplay()
    {
        OnRestartGame();
    }
    private void OnLoseGameWatchAds()
    {
        OnRestartGame();
    }

    private void OnOpenWinGame()
    {
        _ecsBootstrap.IsUIBlockingInput = true;
        if (winGameInstance != null) {
            winGameInstance.RemoveFromClassList("popup-closed");
            winGameInstance.AddToClassList("popup-overlay");
        }
        if (_winGameController != null)
        {
            _winGameController.ShowScreen();
        }
    }
    private void OnCloseWinGame()
    {
        _ecsBootstrap.IsUIBlockingInput = false;
        if (winGameInstance != null) {
            winGameInstance.AddToClassList("popup-closed");
            winGameInstance.RemoveFromClassList("popup-overlay");
        }
    }
    private async void OnWinGameNextLevel()
    {
        if (GameGlobal.SelectedLevel + 1 > GameGlobal.MaxLevel){   
            _winGameController.HidePopupAsync().Forget();         
            OnOpenEndGame();
        }
        else {
            _winGameController.SetLoadingState(true); // Bật trạng thái chờ
            GameGlobal.NextLevel();
            CurrentLevel = GameGlobal.SelectedLevel;
            UpdateLevel();
            await OnRestartGame(); // Chờ cho game restart xong
            _winGameController.HidePopupAsync().Forget(); // Sau khi game đã sẵn sàng, mới ẩn popup
        }
    }

    private void OnOpenEndGame()
    {
        _ecsBootstrap.IsUIBlockingInput = true;
        if (endGameInstance != null) {
            endGameInstance.RemoveFromClassList("popup-closed");
            endGameInstance.AddToClassList("popup-overlay");
        }
        if (_endGameController != null)
        {
            _endGameController.ShowScreen();
        }
    }
    private void OnCloseEndGame()
    {
        _ecsBootstrap.IsUIBlockingInput = false;
        if (endGameInstance != null) {
            endGameInstance.AddToClassList("popup-closed");
            endGameInstance.RemoveFromClassList("popup-overlay");
        }
    }
    private void OnEndGameGoHome()
    {
        OnCloseEndGame();
        //_ecsBootstrap.ClearOldGame();
        SceneManager.LoadScene("MainMenuScene");
    }

    private void showGameUI()
    {
        gameInstance.RemoveFromClassList("hide-layout");   
    }

    private void hideGameUI()
    {
        gameInstance.AddToClassList("hide-layout");
    }

    private void OnQuickPassClicked()
    {
        OnWinGameNextLevel();
    }

    #endregion

    #region Skill

    private void ShowSkillPanel()
    {
        if (gameSkillInstance != null) {
            gameSkillInstance.RemoveFromClassList("popup-closed");
            gameSkillInstance.AddToClassList("popup-overlay");
            _gameSkillController.InitializeSkills();
        }
    }

    private void HideSkillPanel()
    {
        if (gameSkillInstance != null) {
            gameSkillInstance.AddToClassList("popup-closed");
            gameSkillInstance.RemoveFromClassList("popup-overlay");
        }
    }


    #endregion
    
}   