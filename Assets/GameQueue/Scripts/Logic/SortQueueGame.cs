using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Linq;

public class SortQueueGame : MonoBehaviour
{
    [Header("2D Graphics Setup")]
    public GameObject itemPrefab;
    public Sprite[] itemSprites; 
    public float itemSpacingY = 1.2f; 
    public float durationMove = 0.3f; 

    [Header("Dynamic Anchor Setup")]
    public GameObject queueAnchorPrefab; // Prefab gốc của Queue (Khay chứa)
    public GameObject dummyAnchorPrefab; // Prefab gốc của Dummy
    public Transform levelRoot;          // Một GameObject trống làm tâm để căn chỉnh các hàng
    public float queueSpacingX = 1.5f;   // Khoảng cách chiều ngang giữa các Queue
    public Vector2 dummyPosition = new Vector2(0, -3.5f); // Vị trí của Dummy (tương đối so với levelRoot)

    [Header("Game Settings")]
    public int queueCount = 4; 
    public int itemsPerQueue = 4;

    // UI Toolkit Elements
    public UIDocument uiDocument;
    [SerializeField] private PopupSettingController _popupSettingController;
    [SerializeField] private LoseGameController _loseGameController;
    [SerializeField] private WinGameController _winGameController;
    [SerializeField] private EndGameController _endGameController; 
    
    private VisualElement popupSettingInstance;
    private VisualElement loseGameInstance;
    private VisualElement winGameInstance;
    private VisualElement endGameInstance;
    private Button settingsButton;

    // Các Transform lưu trữ Anchor được tạo ra bằng code
    private Transform[] queueAnchors;
    private Transform dummyAnchor;

    // Dữ liệu Logic & Đối tượng thực tế
    private List<List<int>> queuesLogic = new List<List<int>>();
    private List<List<GameObject>> queuesGameObjects = new List<List<GameObject>>();
    
    private int dummyTypeLogic;
    private GameObject dummyGameObject;
    
    private bool isPlaying = false;
    private bool isAnimating = false;
    

    private void Awake()
    {
        SetupUIToolkit();
    }

    private void Update()
    {
        if (!isPlaying || isAnimating) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                QueueAnchor anchor = hit.collider.GetComponent<QueueAnchor>();
                if (anchor != null)
                {
                    OnQueueClicked(anchor.queueIndex).Forget();
                }
            }
        }
    }

    private void SetupUIToolkit()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        settingsButton = root.Q<Button>("SettingsButton");
        if (settingsButton != null)
        {
            settingsButton.clicked += OnSettingsButtonClicked;
        }

        //setting
        popupSettingInstance = root.Q<VisualElement>("PopupSettingInstance");
        popupSettingInstance?.AddToClassList("popup-closed");
        _popupSettingController.OnClosePopup += OnPopupBackClicked;

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
        _endGameController.OnClosePopup += OnCloseEndGame;
        _endGameController.OnHomeAction += OnEndGameGoHome;
        

    }

    private void StartGame()
    {        
        GenerateAnchors();
        GenerateLevel();  
        isPlaying = true;
    }

    private void RestartGame()
    {
        ClearSceneObjects();
        StartGame();
    }

    /// <summary>
    /// Hàm mới: Sinh tự động các Anchor và gán index
    /// </summary>
    private void GenerateAnchors()
    {
        // Tính toán tổng chiều rộng để căn giữa các Queue
        float totalWidth = (queueCount - 1) * queueSpacingX;
        Vector3 startPos = levelRoot.position - new Vector3(totalWidth / 2f, 0, 0);

        queueAnchors = new Transform[queueCount];

        for (int i = 0; i < queueCount; i++)
        {
            Vector3 spawnPos = startPos + new Vector3(i * queueSpacingX, 0, 0);
            GameObject anchorObj = Instantiate(queueAnchorPrefab, spawnPos, Quaternion.identity, levelRoot);
            anchorObj.name = $"QueueAnchor_{i}";

            // Tự động gán QueueIndex để đảm bảo logic click không bị sai lệch
            QueueAnchor qa = anchorObj.GetComponent<QueueAnchor>();
            if (qa == null) qa = anchorObj.AddComponent<QueueAnchor>();
            qa.queueIndex = i;
            qa.UpdateColliderSize(itemsPerQueue, itemSpacingY);

            queueAnchors[i] = anchorObj.transform;
        }

        // Sinh Dummy Anchor
        Vector3 dPos = levelRoot.position + (Vector3)dummyPosition;
        GameObject dummyObj = Instantiate(dummyAnchorPrefab, dPos, Quaternion.identity, levelRoot);
        dummyObj.name = "DummyAnchor";
        dummyAnchor = dummyObj.transform;
    }

    private void GenerateLevel()
    {
        queuesLogic.Clear();
        queuesGameObjects.Clear();

        List<int> pool = new List<int> { 0 };
        for (int type = 1; type <= queueCount; type++)
        {
            for (int j = 0; j < itemsPerQueue; j++)
            {
                pool.Add(type);
            }
        }

        // Shuffle
        for (int i = 0; i < pool.Count; i++)
        {
            int temp = pool[i];
            int rand = Random.Range(i, pool.Count);
            pool[i] = pool[rand];
            pool[rand] = temp;
        }

        int poolIdx = 0;
        for (int i = 0; i < queueCount; i++)
        {
            queuesLogic.Add(new List<int>());
            queuesGameObjects.Add(new List<GameObject>());

            for (int j = 0; j < itemsPerQueue; j++)
            {
                int currentType = pool[poolIdx++];
                queuesLogic[i].Add(currentType);

                GameObject itemObj = Instantiate(itemPrefab, GetItemWorldPosition(i, j), Quaternion.identity, levelRoot);
                UpdateSpriteVisual(itemObj, currentType);
                queuesGameObjects[i].Add(itemObj);
            }
        }

        dummyTypeLogic = pool[poolIdx];
        dummyGameObject = Instantiate(itemPrefab, dummyAnchor.position, Quaternion.identity, levelRoot);
        UpdateSpriteVisual(dummyGameObject, dummyTypeLogic);
    }

    private async UniTaskVoid OnQueueClicked(int queueIndex)
    {
        List<int> targetLogic = queuesLogic[queueIndex];
        List<GameObject> targetObjects = queuesGameObjects[queueIndex];

        if (dummyTypeLogic == 0 && targetLogic[0] == 0) return;

        isAnimating = true;

        int poppedType = targetLogic[0];
        GameObject poppedObj = targetObjects[0];
        targetLogic.RemoveAt(0);
        targetObjects.RemoveAt(0);

        targetLogic.Add(dummyTypeLogic);
        targetObjects.Add(dummyGameObject);

        List<UniTask> moveTweens = new List<UniTask>();

        moveTweens.Add(poppedObj.transform.DOMove(dummyAnchor.position, durationMove).SetEase(Ease.OutQuad).ToUniTask());

        Vector3 tailWorldPos = GetItemWorldPosition(queueIndex, itemsPerQueue - 1);
        moveTweens.Add(dummyGameObject.transform.DOMove(tailWorldPos, durationMove).SetEase(Ease.OutQuad).ToUniTask());

        for (int j = 0; j < targetObjects.Count - 1; j++) 
        {
            Vector3 nextWorldPos = GetItemWorldPosition(queueIndex, j);
            moveTweens.Add(targetObjects[j].transform.DOMove(nextWorldPos, durationMove).SetEase(Ease.Linear).ToUniTask());
        }

        await UniTask.WhenAll(moveTweens);

        dummyGameObject = poppedObj;
        dummyTypeLogic = poppedType;

        UpdateSpriteVisual(dummyGameObject, dummyTypeLogic);
        for (int j = 0; j < targetObjects.Count; j++)
        {
            UpdateSpriteVisual(targetObjects[j], targetLogic[j]);
        }

        isAnimating = false;
        CheckWinCondition();
    }

    private Vector3 GetItemWorldPosition(int queueIndex, int itemIndex)
    {
        return queueAnchors[queueIndex].position + Vector3.up * (itemIndex * itemSpacingY);
    }

    private void UpdateSpriteVisual(GameObject targetObj, int type)
    {
        SpriteRenderer sRenderer = targetObj.GetComponent<SpriteRenderer>();
        if (sRenderer != null)
        {
            if (type < itemSprites.Length) sRenderer.sprite = itemSprites[type];
        }
    }

    private void CheckWinCondition()
    {
        if (dummyTypeLogic != 0) return;

        foreach (var queue in queuesLogic)
        {
            int checkType = queue[0];
            foreach (var itemType in queue)
            {
                if (itemType != checkType) return;
            }
        }

        isPlaying = false;

        OnOpenWinGame();
    }

    private void ClearSceneObjects()
    {
        // Xóa phần tử gameplay
        if (dummyGameObject != null) Destroy(dummyGameObject);
        foreach (var qList in queuesGameObjects)
        {
            foreach (var obj in qList)
            {
                if (obj != null) Destroy(obj);
            }
        }
        
        // Xóa các Anchor động để tạo lại cái mới khi Restart (phòng trường hợp đổi số lượng Queue)
        if (dummyAnchor != null) Destroy(dummyAnchor.gameObject);
        if (queueAnchors != null)
        {
            foreach (var anchor in queueAnchors)
            {
                if (anchor != null) Destroy(anchor.gameObject);
            }
        }
    }

    #region  Other Views
    private void OnSettingsButtonClicked()
    {
        if (popupSettingInstance != null) {
            popupSettingInstance.RemoveFromClassList("popup-closed");
            popupSettingInstance.AddToClassList("popup-overlay");
        }
        if (_popupSettingController != null)
        {
            _popupSettingController.OpenPopup(false);
        }
    }

    private void OnPopupBackClicked()
    {
        if (popupSettingInstance != null) {
            popupSettingInstance.AddToClassList("popup-closed");
            popupSettingInstance.RemoveFromClassList("popup-overlay");
        }
    }

    private void OnOpenLoseGame()
    {
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
        if (loseGameInstance != null) {
            loseGameInstance.AddToClassList("popup-closed");
            loseGameInstance.RemoveFromClassList("popup-overlay");
        }
    }
    private void OnLoseGameReplay()
    {
        
    }
    private void OnLoseGameWatchAds()
    {
        
    }

    private void OnOpenWinGame()
    {
        if (loseGameInstance != null) {
            loseGameInstance.RemoveFromClassList("popup-closed");
            loseGameInstance.AddToClassList("popup-overlay");
        }
        if (_loseGameController != null)
        {
            _loseGameController.ShowScreen();
        }
    }
    private void OnCloseWinGame()
    {
        if (loseGameInstance != null) {
            loseGameInstance.AddToClassList("popup-closed");
            loseGameInstance.RemoveFromClassList("popup-overlay");
        }
    }
    private void OnWinGameNextLevel()
    {
        
    }

    private void OnOpenEndGame()
    {
        if (loseGameInstance != null) {
            loseGameInstance.RemoveFromClassList("popup-closed");
            loseGameInstance.AddToClassList("popup-overlay");
        }
        if (_loseGameController != null)
        {
            _loseGameController.ShowScreen();
        }
    }
    private void OnCloseEndGame()
    {
        if (loseGameInstance != null) {
            loseGameInstance.AddToClassList("popup-closed");
            loseGameInstance.RemoveFromClassList("popup-overlay");
        }
    }
    private void OnEndGameGoHome()
    {
        
    }

    #endregion
}