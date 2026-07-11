using UnityEngine;
using Singleton;
using UnityEngine.Rendering.Universal;

public class Main : Singleton<Main>
{
    public Camera UICamera;

    void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void AddOverlayCamera(Camera camera)
    {
        if (UICamera == null)
        {
            Debug.LogWarning("UICamera is not assigned in Main. Cannot add overlay camera.");
            return;
        }

        var uiCameraData = UICamera.GetUniversalAdditionalCameraData();
        if (uiCameraData.renderType != CameraRenderType.Base)
        {
            Debug.LogWarning($"UICamera ({UICamera.name}) is not a Base camera. Cannot add overlay cameras to it.");
            return;
        }

        var overlayCameraData = camera.GetUniversalAdditionalCameraData();
        if (overlayCameraData.renderType != CameraRenderType.Overlay)
        {
            Debug.LogWarning($"Camera ({camera.name}) is not an Overlay camera. Cannot add it to the UICamera stack.");
            return;
        }

        if (!uiCameraData.cameraStack.Contains(camera))
        {
            uiCameraData.cameraStack.Add(camera);
            Debug.Log($"Added {camera.name} to UICamera stack.");
        }
    }

    public void RemoveOverlayCamera(Camera camera)
    {
        if (UICamera == null)
        {
            Debug.LogWarning("UICamera is not assigned in Main. Cannot remove overlay camera.");
            return;
        }

        var uiCameraData = UICamera.GetUniversalAdditionalCameraData();
        if (uiCameraData.renderType != CameraRenderType.Base)
        {
            Debug.LogWarning($"UICamera ({UICamera.name}) is not a Base camera. It does not have an overlay camera stack.");
            return;
        }

        if (uiCameraData.cameraStack.Contains(camera))
        {
            uiCameraData.cameraStack.Remove(camera);
            Debug.Log($"Removed {camera.name} from UICamera stack.");
        }
    }
}

