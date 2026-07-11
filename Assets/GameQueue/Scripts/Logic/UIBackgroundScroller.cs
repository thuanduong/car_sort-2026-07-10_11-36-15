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
        _carBg = root.Q<VisualElement>(className: "car-bg");

        if (_carBg != null)
        {
            _carBg.schedule.Execute(state =>
            {
                float dt = state.deltaTime / 1000f; 
                
                _currentOffset += scrollSpeed * dt;
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