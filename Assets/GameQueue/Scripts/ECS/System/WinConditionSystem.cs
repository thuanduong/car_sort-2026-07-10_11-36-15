using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Hệ thống kiểm tra điều kiện thắng game.
/// Nó sẽ chạy khi không còn item nào đang di chuyển.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
public partial struct WinConditionSystem : ISystem
{
    private EntityQuery m_MovingItemsQuery;
    private EntityQuery m_ItemDataQuery;
    private EntityQuery m_MoveInProgressQuery;
    private EntityArchetype m_GameWonArchetype;
    private EntityArchetype m_QueueCompletedArchetype;

    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_MovingItemsQuery = state.GetEntityQuery(ComponentType.ReadOnly<MoveTo>());
        m_ItemDataQuery = state.GetEntityQuery(ComponentType.ReadOnly<ItemData>());
        m_MoveInProgressQuery = state.GetEntityQuery(ComponentType.ReadOnly<SortLogicSystem.MoveInProgressEvent>());
        
        m_GameWonArchetype = state.EntityManager.CreateArchetype(ComponentType.ReadWrite<GameWonEvent>());
        m_QueueCompletedArchetype = state.EntityManager.CreateArchetype(typeof(SortLogicSystem.QueueCompletedEvent));
        state.RequireForUpdate<SortLogicSystem.CheckWinConditionRequest>(); // Chỉ chạy khi có yêu cầu
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Thay vì vô hiệu hóa system, chúng ta chỉ cần kiểm tra xem game đã thắng chưa.
        // Nếu đã có sự kiện GameWonEvent, không cần làm gì thêm.
        if (!SystemAPI.QueryBuilder().WithAll<GameWonEvent>().Build().IsEmpty)
        {
            return;
        }

        if (m_MovingItemsQuery.IsEmpty && !m_MoveInProgressQuery.IsEmpty)
        {
            // Dùng EntityCommandBuffer để hủy entity một cách an toàn.
            var cleanupEcb = new EntityCommandBuffer(Allocator.Temp);
            cleanupEcb.DestroyEntity(m_MoveInProgressQuery.GetSingletonEntity());
            cleanupEcb.Playback(state.EntityManager);
            cleanupEcb.Dispose();
        }

        // Nếu vẫn còn bất kỳ item nào đang di chuyển, hệ thống sẽ không làm gì cả.
        if (!m_MovingItemsQuery.IsEmpty)
        {
            return;
        }
        
        // Hủy yêu cầu kiểm tra ngay sau khi bắt đầu xử lý để không chạy lại ở frame sau
        var checkRequestEntity = SystemAPI.GetSingletonEntity<SortLogicSystem.CheckWinConditionRequest>();
        state.EntityManager.DestroyEntity(checkRequestEntity);
        
        // Lấy entity dummy một cách an toàn. Nếu không tồn tại, không thể thắng.
        if (!SystemAPI.TryGetSingletonEntity<DummyData>(out var dummyEntity))
        {
            return;
        }
        
        // Kiểm tra xem dummy item có phải là loại "trống" (type 0) không
        // if (SystemAPI.GetComponent<ItemData>(dummyEntity).Type != 0) return; // << ĐÃ LOẠI BỎ
        
        var gameSettings = SystemAPI.GetSingleton<GameSettings>();
        // Sử dụng query đã được cache trong OnCreate và chuyển nó thành một NativeArray
        var itemDataArray = m_ItemDataQuery.ToComponentDataArray<ItemData>(Allocator.Temp);

        // Kiểm tra tất cả các hàng đợi
        bool allQueuesAreFullAndHomogeneous = true;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        // UnityEngine.Debug.Log($"Start Check queue");
        for (int i = 0; i < gameSettings.QueueCount; i++)
        {
            int firstItemType = -1;
            int itemsInThisQueueCount = 0;
            bool isHomogeneous = true;
            bool isAlreadyCompleted = false;

            foreach (var item in itemDataArray)
            {
                if (item.QueueIndex != i) continue;

                // Kiểm tra xem hàng đợi này đã được đánh dấu hoàn thành chưa.
                // Chỉ cần kiểm tra một item bất kỳ trong hàng là đủ.
                var firstEntityInQueue = FindFirstEntityInQueue(ref state, i);
                if (firstEntityInQueue != Entity.Null && SystemAPI.HasComponent<SortLogicSystem.QueueCompletedTag>(firstEntityInQueue)) {
                    isAlreadyCompleted = true;
                    break; // Nếu đã hoàn thành, bỏ qua kiểm tra hàng này
                }
                // Bỏ qua các item trống (Type 0)
                if (item.Type == 0) continue;

                if (firstItemType == -1)
                {
                    firstItemType = item.Type;
                }

                if (item.Type != firstItemType)
                {
                    isHomogeneous = false;
                }
                itemsInThisQueueCount++;
            }

            if (isAlreadyCompleted) continue;

            // Điều kiện để chưa thắng: hàng đợi không trống, không đầy, hoặc đầy nhưng không đồng nhất
            if ((itemsInThisQueueCount > 0 && itemsInThisQueueCount < gameSettings.ItemsPerQueue) || (itemsInThisQueueCount == gameSettings.ItemsPerQueue && !isHomogeneous))
            {
                allQueuesAreFullAndHomogeneous = false;
            }
            else if (itemsInThisQueueCount == gameSettings.ItemsPerQueue && isHomogeneous && firstItemType != -1)
            {
                // Hàng đợi vừa được hoàn thành trong lượt này
                var queueCompletedEntity = ecb.CreateEntity(m_QueueCompletedArchetype);
                ecb.AddComponent(queueCompletedEntity, new SortLogicSystem.QueueCompletedEvent { QueueIndex = i });

                // Thêm tag vào tất cả các item trong hàng đợi này
                foreach (var (itemData, entity) in SystemAPI.Query<RefRO<ItemData>>().WithEntityAccess())
                {
                    if (itemData.ValueRO.QueueIndex == i)
                    {
                        ecb.AddComponent<SortLogicSystem.QueueCompletedTag>(entity);
                    }
                }
            }
        }

        // Chỉ playback nếu có lệnh được thêm vào
        if (!ecb.IsEmpty)
        {
            ecb.Playback(state.EntityManager);
        }
        ecb.Dispose();

        // Nếu tất cả kiểm tra đều qua -> Thắng!
        if (allQueuesAreFullAndHomogeneous)
        {
            state.EntityManager.CreateEntity(m_GameWonArchetype);
            // Không cần vô hiệu hóa system nữa. Nó sẽ tự dừng lại ở lần kiểm tra tiếp theo.
        }
    }

    private Entity FindFirstEntityInQueue(ref SystemState state, int queueIndex)
    {
        foreach (var (itemData, entity) in SystemAPI.Query<RefRO<ItemData>>().WithEntityAccess())
        {
            if (itemData.ValueRO.QueueIndex == queueIndex)
            {
                return entity; // Trả về entity đầu tiên tìm thấy trong hàng
            }
        }
        return Entity.Null;
    }
}