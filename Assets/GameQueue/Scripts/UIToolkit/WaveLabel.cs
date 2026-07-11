using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WaveLabel : MonoBehaviour
{
    [Header("Cài đặt sóng")]
    [Tooltip("Độ cao của sóng (tính bằng pixel)")]
    public float amplitude = 15f; 
    
    [Tooltip("Tốc độ di chuyển lên xuống")]
    public float speed = 3f;      
    public List<string> _waveName = new List<string>();


    private List<Label> _waveLabels = new List<Label>();

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;
        foreach (var s in _waveName) {
            if (string.IsNullOrEmpty(s.Trim())) continue;
            var _waveLabel = root.Q<Label>(s);
            if (_waveLabel != null)
                _waveLabels.Add(_waveLabel);
        }
    }

    void OnDisable()
    {
        _waveLabels.Clear();
    }

    void Update()
    {
        if (_waveLabels.Count == 0) return;

        float yOffset = amplitude * Mathf.Sin(Time.time * speed);
        foreach (var _waveLabel in _waveLabels){
            _waveLabel.style.translate = new StyleTranslate(new Translate(0, yOffset, 0));
        }
    }
}