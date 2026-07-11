using UnityEngine;
using UnityEngine.UIElements;

public class UIBackgroundScroller : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    
    [Tooltip("Hướng và tốc độ. (-50, 50) là chéo trái dưới. (50, -50) là chéo phải trên")]
    public Vector2 scrollSpeed = new Vector2(-50f, 50f); 

    private VisualElement _carBg;
    private static Vector2 _currentOffset;

    void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        
        var root = uiDocument.rootVisualElement;
        
        // Tìm element dựa vào class name mà bạn đã định nghĩa trong USS
        _carBg = root.Q<VisualElement>(className: "car-bg");

        if (_carBg != null)
        {
            // Lên lịch chạy mỗi 16ms (tương đương ~60fps)
            _carBg.schedule.Execute(state =>
            {
                // state.deltaTime trả về thời gian giữa các lần gọi (tính bằng mili-giây)
                float dt = state.deltaTime / 1000f; 
                
                _currentOffset += scrollSpeed * dt;

                // Reset giá trị về 0 khi vượt mức để tránh tràn biến float sau thời gian dài
                if (Mathf.Abs(_currentOffset.x) > 10000f) _currentOffset.x = 0;
                if (Mathf.Abs(_currentOffset.y) > 10000f) _currentOffset.y = 0;

                // Cập nhật tọa độ của background trực tiếp qua style
                _carBg.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(_currentOffset.x, LengthUnit.Pixel));
                _carBg.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, new Length(_currentOffset.y, LengthUnit.Pixel));
                
            }).Every(16);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy VisualElement nào có class là 'car-bg'!");
        }
    }
}