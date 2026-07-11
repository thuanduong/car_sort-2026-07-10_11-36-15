using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct GameSettings : IComponentData
{
    public int QueueCount;
    public int ItemsPerQueue;
    public float ItemSpacingY;
    public float DurationMove;
    public float DurationFlip;
    public float DurationPush;
    public float DelayPerRow;
    public int MaxMove;
    public FixedList512Bytes<float3> QueueAnchorPositions;
    public FixedList512Bytes<float3> SplineItemPositions; 
    public FixedList512Bytes<float> SplineItemScalers; 
    public FixedList512Bytes<quaternion> SplineItemRotations; 
    public FixedList512Bytes<float3> JumpToPositions;

    public float3 DummyAnchorPosition;
}