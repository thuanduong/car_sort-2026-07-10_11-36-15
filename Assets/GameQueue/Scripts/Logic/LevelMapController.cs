using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// 1. Cấu trúc dữ liệu
public enum LevelStateType { Passed, Locked }

public class SortLevelData
{
    public int LevelId;
    public string Name;
    public Vector2 Position;
    public LevelStateType StateType;
}

public class LevelMapController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PopupSettingController _popupSettingController;
    [SerializeField] private int currentLevelToPlay = 6;
    [SerializeField] private int totalLevels = 50;
    [SerializeField] private float ySpacing = 120f; // Khoảng cách Y giữa 2 level
    [SerializeField] private string gameSceneName = "GameScene"; // Tên scene game chính

    private ScrollView mapScrollView;
    private VisualElement mapContent;
    
    private Button playButton;
    private Button settingsButton;
    private int _selectedLevelId;
    private List<SortLevelData> levelDatas;
    private float mapHeight;

    private List<VisualElement> nodeElements = new List<VisualElement>();
    private List<VisualElement> lineElements = new List<VisualElement>();

    private ScrollViewDragManipulator dragManipulator;
    private VisualElement popupSettingInstance;

    void Awake()
    {
        GameGlobal.ReloadGameValue();
        currentLevelToPlay = GameGlobal.CompletedLevel;
    }

    private void OnEnable()
    {
        
        
        var root = uiDocument.rootVisualElement;
        mapScrollView = root.Q<ScrollView>("MapScrollView");
        mapContent = root.Q<VisualElement>("MapContent");
        playButton = root.Q<Button>("PlayButton");
        settingsButton = root.Q<Button>("SettingsButton");

        popupSettingInstance = root.Q<VisualElement>("PopupSettingInstance");
        popupSettingInstance?.AddToClassList("popup-closed");
        _popupSettingController.OnClosePopup += OnPopupBackClicked;

        if (playButton != null)
        {
            playButton.clicked += OnPlayButtonClicked;
        }

        _selectedLevelId = currentLevelToPlay; // Mặc định chọn level cao nhất

        if (settingsButton != null)
        {
            settingsButton.clicked += OnSettingsButtonClicked;
        }

        dragManipulator = new ScrollViewDragManipulator(mapScrollView);
        mapScrollView.AddManipulator(dragManipulator);

        InitMapFlow();
    }

    private void OnDisable()
    {
        _popupSettingController.OnClosePopup -= OnPopupBackClicked;
        
        if (mapScrollView != null && dragManipulator != null)
        {
            mapScrollView.RemoveManipulator(dragManipulator);
        }

        if (playButton != null)
        {
            playButton.clicked -= OnPlayButtonClicked;
        }

        if (settingsButton != null)
        {
            settingsButton.clicked -= OnSettingsButtonClicked;
        }

    }

    private void InitMapFlow()
    {
        // Bước 1: Giả lập đọc/tạo dữ liệu Level
        GenerateLevelData();

        // Bước 2: Thiết lập chiều cao tổng cho Map (để đi từ dưới lên)
        // Y của level cao nhất + 1 khoảng padding
        mapHeight = levelDatas[levelDatas.Count - 1].Position.y + 300f; 
        mapContent.style.height = mapHeight;

        // Bước 3: Vẽ các đường nối (vẽ trước để nó nằm dưới Node)
        DrawLines();

        // Bước 4: Vẽ các Node
        DrawNodes();

        // Bước 5: Đăng ký một callback để thực hiện các tác vụ phụ thuộc vào layout
        // (như căn giữa và cuộn) sau khi UI đã được vẽ và có kích thước cụ thể.
        mapScrollView.RegisterCallback<GeometryChangedEvent>(OnGeometryReady);
    }

    private void OnGeometryReady(GeometryChangedEvent evt)
    {
        // Hủy đăng ký để không bị gọi lại nhiều lần
        mapScrollView.UnregisterCallback<GeometryChangedEvent>(OnGeometryReady);

        // Bước 6: Căn giữa các node và đường kẻ theo chiều ngang
        UpdateHorizontalPositions();

        // Bước 7: Cuộn đến Level hiện tại
        FocusOnCurrentLevel();
    }

    private void GenerateLevelData()
    {
        levelDatas = new List<SortLevelData>();
        
        // Tạo đường zigzag từ dưới lên
        for (int i = 1; i <= totalLevels; i++)
        {
            float containerWidth = mapScrollView.resolvedStyle.width;
            float xPos = containerWidth / 2f;;
            // Level 1 ở thấp nhất (Y nhỏ nhất), level tăng dần Y
            float yPos = i * ySpacing;

            LevelStateType state = LevelStateType.Locked;
            string nodeName = i.ToString();

            if (i <= currentLevelToPlay)
            {
                state = LevelStateType.Passed;
            }

            levelDatas.Add(new SortLevelData()
            {
                LevelId = i,
                Name = nodeName,
                Position = new Vector2(xPos, yPos),
                StateType = state
            });
        }
    }

    private void DrawLines()
    {
        for (int i = 0; i < levelDatas.Count - 1; i++)
        {
            Vector2 p1 = GetUIPosition(levelDatas[i].Position);
            Vector2 p2 = GetUIPosition(levelDatas[i + 1].Position);

            VisualElement line = new VisualElement();
            line.AddToClassList("map-line");
            
            // Đặt vị trí điểm đầu
            line.style.left = p1.x;
            line.style.top = p1.y;

            // Tính toán khoảng cách (độ dài) và góc xoay (sẽ cập nhật lại sau)
            float distance = Vector2.Distance(p1, p2);
            float angle = Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg;

            line.style.width = distance;
            // UI Toolkit sử dụng Rotate để xoay VisualElement
            line.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(angle)));

            lineElements.Add(line);

            mapContent.Add(line);
        }
    }

    private void DrawNodes()
    {
        foreach (var data in levelDatas)
        {
            VisualElement node = new VisualElement();
            node.AddToClassList("node-base");
            
            // Định vị trí
            Vector2 uiPos = GetUIPosition(data.Position);
            node.style.left = uiPos.x;
            node.style.top = uiPos.y;

            // Add class dựa theo State
            switch (data.StateType)
            {
                case LevelStateType.Passed: 
                    if (data.LevelId == _selectedLevelId) 
                        node.AddToClassList("node-current"); 
                    else 
                        node.AddToClassList("node-passed"); 
                    break;
                case LevelStateType.Locked: node.AddToClassList("node-locked"); break;
            }

            // Gắn Text
            Label textLabel = new Label(data.Name);
            textLabel.AddToClassList("node-text");
            node.Add(textLabel);

            if (data.StateType != LevelStateType.Locked)
            {
                node.RegisterCallback<ClickEvent>(ev => OnNodeClicked(data.LevelId));
            }

            nodeElements.Add(node);
            mapContent.Add(node);
        }
    }

    private void UpdateNodeSelection()
    {
        for (int i = 0; i < nodeElements.Count; i++)
        {
            var node = nodeElements[i];
            var data = levelDatas[i];

            // Xóa các class trạng thái cũ
            node.RemoveFromClassList("node-current");
            node.RemoveFromClassList("node-passed");
            node.RemoveFromClassList("node-locked");

            // Thêm lại class chính xác dựa trên logic mới
            if (data.LevelId == _selectedLevelId)
            {
                node.AddToClassList("node-current");
            }
            else if (data.StateType == LevelStateType.Passed)
            {
                node.AddToClassList("node-passed"); 
            }
            else 
                node.AddToClassList("node-locked");
        }
    }

    /// <summary>
    /// Cập nhật lại vị trí X của các node và đường kẻ để chúng được căn giữa.
    /// </summary>
    private void UpdateHorizontalPositions()
    {
        float containerWidth = mapScrollView.resolvedStyle.width;
        float pathWidth = 150f; // Khoảng cách ngang giữa 2 cột node
        float pathCenter = containerWidth / 2f;

        // Cập nhật vị trí X cho từng node
        for (int i = 0; i < levelDatas.Count; i++)
        {
            var data = levelDatas[i];
            // float xPos = (data.LevelId % 2 == 0)
            //     ? pathCenter + pathWidth / 2f
            //     : pathCenter - pathWidth / 2f;

            // data.Position.x = xPos;
            data.Position.x = pathCenter;
            
            Vector2 uiPos = GetUIPosition(data.Position);
            nodeElements[i].style.left = uiPos.x;
            nodeElements[i].style.top = uiPos.y;
        }

        // Cập nhật lại các đường kẻ nối
        for (int i = 0; i < lineElements.Count; i++)
        {
            Vector2 p1 = GetUIPosition(levelDatas[i].Position);
            Vector2 p2 = GetUIPosition(levelDatas[i + 1].Position);
            
            lineElements[i].style.left = p1.x;
            lineElements[i].style.top = p1.y;
            lineElements[i].style.width = Vector2.Distance(p1, p2);
            lineElements[i].style.rotate = new StyleRotate(new Rotate(Angle.Degrees(Mathf.Atan2(p2.y - p1.y, p2.x - p1.x) * Mathf.Rad2Deg)));
        }
    }

    // Chuyển đổi toạ độ: (0,0) của data là đáy, nhưng UI Toolkit tính Top từ trên xuống
    private Vector2 GetUIPosition(Vector2 dataPosition)
    {
        return new Vector2(dataPosition.x, mapHeight - dataPosition.y);
    }

    private void FocusOnCurrentLevel()
    {
        var currentLvlData = levelDatas.Find(x => x.LevelId == _selectedLevelId);
        if (currentLvlData != null)
        {
            Vector2 uiPos = GetUIPosition(currentLvlData.Position);
            
            // Tính toán mục tiêu cuộn sao cho node nằm giữa màn hình
            float viewportHeight = mapScrollView.resolvedStyle.height;
            float targetY = uiPos.y - (viewportHeight / 2f);

            // Giới hạn giá trị cuộn để không bị lố
            float maxScroll = Mathf.Max(0, mapHeight - viewportHeight);
            targetY = Mathf.Clamp(targetY, 0, maxScroll);

            // Cuộn ngay lập tức đến vị trí mục tiêu
            mapScrollView.scrollOffset = new Vector2(0, targetY);
        }
    }

    private void OnNodeClicked(int levelId)
    {
        // Chỉ cho phép chọn các level đã mở khóa
        var clickedData = levelDatas.Find(l => l.LevelId == levelId);
        if (clickedData != null && clickedData.StateType == LevelStateType.Locked)
        {
            return; // Không làm gì nếu click vào level bị khóa
        }

        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        _selectedLevelId = levelId;
        UpdateNodeSelection();
        Debug.Log($"Selected Level: {_selectedLevelId}");
    }

    private void OnPlayButtonClicked()
    {
        V3.Component.SoundComponent.Instance?.PlaySFX("click");
        Debug.Log($"Playing Level: {_selectedLevelId}");
        GameGlobal.SelectedLevel = _selectedLevelId;
        SceneManager.LoadScene(gameSceneName);
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