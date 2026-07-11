using Unity.Entities;

/// <summary>
/// Một component "tag" sự kiện, được tạo ra bởi WinConditionSystem
/// khi điều kiện thắng game được thỏa mãn.
/// Lớp Bootstrap (SortQueueGame) có thể lắng nghe sự kiện này để hiển thị UI.
/// </summary>
public struct GameWonEvent : IComponentData { }