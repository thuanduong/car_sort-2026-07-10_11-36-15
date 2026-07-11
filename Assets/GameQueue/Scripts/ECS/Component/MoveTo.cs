using Unity.Entities;
using Unity.Mathematics;

public struct MoveTo : IComponentData
{
    public float3 StartPosition;
    public float3 EndPosition;
    public float Duration;
    public float Delay;
    public float ElapsedTime;
    public int MoveType; //0: normal, 1 : flip
}
