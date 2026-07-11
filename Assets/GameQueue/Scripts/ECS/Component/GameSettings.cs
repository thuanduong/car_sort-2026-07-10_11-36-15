using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Component singleton chứa các thiết lập và dữ liệu cấu hình của game.
/// Các system sẽ đọc từ component này thay vì truy cập trực tiếp vào MonoBehaviour.
/// </summary>
public struct GameSettings : IComponentData
{
    // Game settings
    public int QueueCount;
    public int ItemsPerQueue;
    public float ItemSpacingY;
    public float DurationMove;
    public float DurationFlip;
    public float DurationPush;
    public float DelayPerRow;
    public int MaxMove;

    // Dữ liệu vị trí của các anchor, được khởi tạo bởi bootstrap
    public FixedList512Bytes<float3> QueueAnchorPositions;
    public FixedList512Bytes<float3> SplineItemPositions; 
    public FixedList512Bytes<float> SplineItemScalers; 
    public FixedList512Bytes<quaternion> SplineItemRotations; 
    public FixedList512Bytes<float3> JumpToPositions;

    
    public float3 DummyAnchorPosition;
}