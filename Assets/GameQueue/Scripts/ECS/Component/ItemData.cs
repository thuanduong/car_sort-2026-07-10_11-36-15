
using Unity.Entities;

public struct ItemData : IComponentData
{
    public int Type;         // Loại item (tương ứng với sprite)
    public int QueueIndex;   // ID của hàng đợi mà item này thuộc về (-1 nếu là dummy)
    public int IndexInQueue; // Vị trí trong hàng đợi (0 là trên cùng)
}
