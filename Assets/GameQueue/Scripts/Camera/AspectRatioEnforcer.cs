using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
   
    [Tooltip("Target aspect ratio for normal screens (e.g., 9:16).")]
    public Vector2 normalAspectRatio = new Vector2(9, 16);
    [Tooltip("Orthographic size for normal aspect ratio.")]
    public float normalSize = 8.0f;
    [Tooltip("Target aspect ratio for long screens (e.g., 9:19.5).")]
    public Vector2 longAspectRatio = new Vector2(9, 19.5f);
    [Tooltip("Orthographic size for long aspect ratio.")]
    public float longSize = 9.0f;
    private Camera cam;
    

    private int lastWidth = 0;
    private int lastHeight = 0;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateAspectRatio();
    }

    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            UpdateAspectRatio();
        }
    }

    void UpdateAspectRatio()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float targetNormalAspect = normalAspectRatio.x / normalAspectRatio.y;
        float targetLongAspect = longAspectRatio.x / longAspectRatio.y;

        float currentAspect = (float)lastWidth / lastHeight;

        float t = Mathf.InverseLerp(targetNormalAspect, targetLongAspect, currentAspect);

        cam.orthographicSize = Mathf.Lerp(normalSize, longSize, t);
    }

}