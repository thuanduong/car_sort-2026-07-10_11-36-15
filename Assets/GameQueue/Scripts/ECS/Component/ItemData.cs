
using Unity.Entities;

public struct ItemData : IComponentData
{
    public int Type;         
    public int QueueIndex;   
    public int IndexInQueue; 
}
