using Unity.Entities;
using Unity.Mathematics;

public struct PointerInputComponent : IComponentData
{
    public float2 ScreenPosition;
    public bool IsPressing;
    public bool PressedThisFrame;
    public bool ReleasedThisFrame;
}
