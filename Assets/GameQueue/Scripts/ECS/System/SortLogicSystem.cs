using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Hệ thống xử lý logic chính của game.
/// Nó sẽ tìm các sự kiện click, thực hiện hoán đổi item và cập nhật trạng thái các item khác.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SortLogicSystem : ISystem
{
    // Component tag để báo hiệu một nước đi đã hoàn thành
    public struct MoveCompletedEvent : IComponentData
    {
    }
    
    // Component tag để báo hiệu một nước đi đã bắt đầu (và hợp lệ)
    public struct MoveStartedEvent : IComponentData
    {
    }

    // Component tag để báo hiệu một lượt di chuyển đang trong quá trình xử lý (dùng để khóa input)
    public struct MoveInProgressEvent : IComponentData
    {
    }

    // Component tag để yêu cầu chạy animation "push"
    public struct PushAnimationRequest : IComponentData
    {
        public Entity PushingEntity; // Entity thực hiện hành động push
        public float Duration;
        public float ElapsedTime;
    }

    // Component yêu cầu di chuyển Dummy Item đến vị trí chờ trước khi hoán đổi
    public struct PreSwapMoveRequest : IComponentData
    {
        public int ClickedQueueIndex;
    }

    // Component yêu cầu bắt đầu chuỗi animation push
    public struct PushSequenceRequest : IComponentData
    {
        public int ClickedQueueIndex;
    }

    // Component chứa dữ liệu để thực hiện hoán đổi sau khi animation push kết thúc
    public struct ExecuteSwapEvent : IComponentData
    {
        public int ClickedQueueIndex;
    }

    // Component tag để báo hiệu một hàng đã được hoàn thành
    public struct QueueCompletedEvent : IComponentData
    {
        public int QueueIndex;
    }

    // Component tag để đánh dấu một hàng đã hoàn thành và không cần kiểm tra nữa
    public struct QueueCompletedTag : IComponentData
    {
    }

    // Component tag để yêu cầu WinConditionSystem chạy kiểm tra
    public struct CheckWinConditionRequest : IComponentData
    {
    }
    
    private EntityArchetype moveCompletedArchetype;
    private EntityArchetype moveStartedArchetype;
    private EntityArchetype preSwapMoveArchetype;
    private EntityArchetype pushSequenceArchetype;
    private EntityArchetype moveInProgressArchetype;

    private EntityArchetype checkWinConditionArchetype;
    public void OnCreate(ref SystemState state)
    {
        // Hệ thống sẽ chạy khi có SortRequest HOẶC ExecuteSwapEvent
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<SortRequest, PreSwapMoveRequest, PushSequenceRequest, ExecuteSwapEvent>().Build());
        state.RequireForUpdate<DummyData>();
        state.RequireForUpdate<GameSettings>();
        
        // Public các archetype để system khác có thể truy cập
        moveCompletedArchetype = state.EntityManager.CreateArchetype(typeof(MoveCompletedEvent)); 
        moveStartedArchetype = state.EntityManager.CreateArchetype(typeof(MoveStartedEvent)); 
        preSwapMoveArchetype = state.EntityManager.CreateArchetype(typeof(PreSwapMoveRequest));
        pushSequenceArchetype = state.EntityManager.CreateArchetype(typeof(PushSequenceRequest));
        moveInProgressArchetype = state.EntityManager.CreateArchetype(typeof(MoveInProgressEvent));
        checkWinConditionArchetype = state.EntityManager.CreateArchetype(typeof(CheckWinConditionRequest));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var gameSettings = SystemAPI.GetSingleton<GameSettings>();

        if (SystemAPI.QueryBuilder().WithAll<PreSwapMoveRequest>().Build().IsEmpty == false)
        {
            var preSwapEntity = SystemAPI.GetSingletonEntity<PreSwapMoveRequest>();
            var preSwapData = SystemAPI.GetComponent<PreSwapMoveRequest>(preSwapEntity);
            
            if (!SystemAPI.HasComponent<MoveTo>(SystemAPI.GetSingletonEntity<DummyData>()))
            {
                ecb.DestroyEntity(preSwapEntity);
                var newEntity = ecb.CreateEntity(pushSequenceArchetype);
                ecb.AddComponent(newEntity, new PushSequenceRequest { ClickedQueueIndex = preSwapData.ClickedQueueIndex });
            }
        }
        else if (SystemAPI.QueryBuilder().WithAll<PushSequenceRequest>().Build().IsEmpty == false)
        {
            var pushSeqEntity = SystemAPI.GetSingletonEntity<PushSequenceRequest>();
            var pushSeqData = SystemAPI.GetComponent<PushSequenceRequest>(pushSeqEntity);

            HandlePushSequence(ref state, ref ecb, gameSettings, pushSeqData.ClickedQueueIndex);

            ecb.DestroyEntity(pushSeqEntity);
        }

        else if (!SystemAPI.QueryBuilder().WithAll<ExecuteSwapEvent>().Build().IsEmpty)
        {
            var swapEventEntity = SystemAPI.GetSingletonEntity<ExecuteSwapEvent>();
            var swapEventData = SystemAPI.GetComponent<ExecuteSwapEvent>(swapEventEntity);
            
            ExecuteSwap(ref state, ref ecb, gameSettings, swapEventData.ClickedQueueIndex);
            
            ecb.DestroyEntity(swapEventEntity);
        }
        else if (!SystemAPI.QueryBuilder().WithAll<SortRequest>().Build().IsEmpty)
        {
            HandleSortRequest(ref state, ref ecb, gameSettings);
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    private void HandleSortRequest(ref SystemState state, ref EntityCommandBuffer ecb, GameSettings gameSettings)
    {
        var sortRequestEntity = SystemAPI.GetSingletonEntity<SortRequest>();
        var clickedQueueIndex = SystemAPI.GetComponent<ClickedQueueEvent>(sortRequestEntity).QueueIndex;

        // --- START: Logic mới để kiểm tra hàng đợi đã hoàn chỉnh chưa ---
        int firstItemTypeInQueue = -1;
        int itemsInThisQueue = 0;
        bool isMixedType = false; 

        // 1. Duyệt qua tất cả item để kiểm tra trạng thái của hàng đợi được click
        foreach (var itemData in SystemAPI.Query<RefRO<ItemData>>())
        {
            if (itemData.ValueRO.QueueIndex == clickedQueueIndex)
            {
                // Bỏ qua các ô trống
                if (itemData.ValueRO.Type == 0) continue;

                if (firstItemTypeInQueue == -1)
                {
                    firstItemTypeInQueue = itemData.ValueRO.Type;
                }
                else if (itemData.ValueRO.Type != firstItemTypeInQueue)
                {
                    isMixedType = true; // Tìm thấy item khác loại -> hàng chưa đồng nhất
                }
                itemsInThisQueue++;
            }
        }

        // 2. Nếu hàng đợi đã đầy (itemsInThisQueue == ItemsPerQueue) và không bị trộn lẫn loại (isMixedType == false) -> không cho di chuyển
        if (itemsInThisQueue == gameSettings.ItemsPerQueue && !isMixedType && firstItemTypeInQueue != -1)
        {
            ecb.DestroyEntity(sortRequestEntity); 
            return;
        }
        // --- END: Logic mới ---

        // Lấy dummy entity
        var dummyEntity = SystemAPI.GetSingletonEntity<DummyData>();
        var dummyData = SystemAPI.GetComponent<ItemData>(dummyEntity);

        // Lấy top item của hàng đợi được click
        Entity topItemEntity = FindTopItem(ref state, clickedQueueIndex);
        
        // Nếu hàng đợi trống (không có item nào), không làm gì cả
        if (topItemEntity == Entity.Null)
        {
            UnityEngine.Debug.Log("[HandleSortRequest] Clicked on an empty queue.");
            ecb.DestroyEntity(sortRequestEntity);
            return;
        }
        
        // Chỉ cho phép di chuyển nếu dummy trống hoặc loại của dummy trùng với top item
        // if (dummyData.Type != 0 && dummyData.Type != topItemData.Type)
        // {
        //     UnityEngine.Debug.Log("[HandleSortRequest] 2.");
        //     ecb.DestroyEntity(sortRequestEntity);
        //     return;
        // }

        // Bắt đầu Phase 1: Yêu cầu di chuyển Dummy Item
        ecb.AddComponent<PositionUpdateRequest>(dummyEntity);
        var newEntity = ecb.CreateEntity(preSwapMoveArchetype);
        ecb.AddComponent(newEntity, new PreSwapMoveRequest { ClickedQueueIndex = clickedQueueIndex });
        
        // Tạo sự kiện đánh dấu lượt di chuyển đang diễn ra để khóa input
        ecb.CreateEntity(moveInProgressArchetype);
        ecb.CreateEntity(moveStartedArchetype);

        // Hủy entity sự kiện sau khi đã xử lý xong
        ecb.DestroyEntity(sortRequestEntity);
    }

    private void HandlePushSequence(ref SystemState state, ref EntityCommandBuffer ecb, GameSettings gameSettings, int clickedQueueIndex)
    {
        // Phase 2: Yêu cầu animation "push" cho item dưới cùng (botItemEntity)
        Entity botItemEntity = FindBotItem(ref state, clickedQueueIndex);
        Entity dummyEntity = SystemAPI.GetSingletonEntity<DummyData>();

        if (botItemEntity != Entity.Null)
        {
            // Dummy entity sẽ là entity "đẩy"
            ecb.AddComponent(botItemEntity, new PushAnimationRequest { PushingEntity = dummyEntity, Duration = gameSettings.DurationPush, ElapsedTime = 0f });
        }
    }

    private void ExecuteSwap(ref SystemState state, ref EntityCommandBuffer ecb, GameSettings gameSettings, int clickedQueueIndex)
    {
        // Tạo sự kiện báo hiệu một nước đi hợp lệ đã bắt đầu
        // ecb.CreateEntity(moveStartedArchetype);

        Entity dummyEntity = Entity.Null;
        ItemData dummyItemData = default;

        // 1. Tìm entity đang giữ item ở vị trí chờ (dummy)
        foreach (var (itemData, entity) in SystemAPI.Query<RefRO<ItemData>>().WithAll<DummyData>().WithEntityAccess())
        {
            dummyEntity = entity;
            dummyItemData = itemData.ValueRO;
            break; 
        }

        // Tìm lại topItemEntity của hàng đợi được click, vì nó không được lưu trong event nữa
        Entity topItemEntity = FindTopItem(ref state, clickedQueueIndex);

        if (dummyEntity == Entity.Null || topItemEntity == Entity.Null)
        {
            // Nếu có lỗi xảy ra ở bước này, chỉ return, không hủy event vì nó đã được lên lịch hủy
            // ecb.DestroyEntity(clickedQueueEntity); // Hủy event và không làm gì cả
            // ecb.Playback(state.EntityManager);
            // ecb.Dispose();
            return;
        }

        // Lấy dữ liệu của topItemEntity vừa tìm được
        var topItemData = SystemAPI.GetComponent<ItemData>(topItemEntity);
        if (dummyItemData.Type == 0 && topItemData.Type == 0)
        {
            return;
        }

        // 3. Bắt đầu hoán đổi
        // Hoán đổi dữ liệu giữa topItem và dummyItem.
        // Entity không đổi, chỉ có dữ liệu bên trong chúng thay đổi.
        // 1. Cấu hình lại topItemEntity để trở thành Dummy mới
        ecb.SetComponent(topItemEntity, new ItemData { Type = topItemData.Type, QueueIndex = -1, IndexInQueue = 0 });
        ecb.AddComponent<DummyData>(topItemEntity);
        ecb.AddComponent<PositionUpdateRequest>(topItemEntity);

        // 2. Cấu hình lại dummyEntity cũ để trở thành item mới trong hàng đợi
        ecb.RemoveComponent<DummyData>(dummyEntity);
        ecb.SetComponent(dummyEntity, new ItemData { Type = dummyItemData.Type, QueueIndex = clickedQueueIndex, IndexInQueue = gameSettings.ItemsPerQueue - 1 });
        ecb.AddComponent<PositionUpdateRequest>(dummyEntity);

        //UnityEngine.Debug.Log($"[SortLogic] Set Current Dummy Type found: {dummyItemData.Type} and top is {topItemData.Type}");

        // 4. Dịch chuyển các item còn lại trong hàng đợi lên một bậc
        foreach (var (itemData, entity) in SystemAPI.Query<RefRW<ItemData>>().WithEntityAccess())
        {
            if (itemData.ValueRO.QueueIndex == clickedQueueIndex && entity != topItemEntity && entity != dummyEntity)
            {
                itemData.ValueRW.IndexInQueue--;
                ecb.AddComponent<PositionUpdateRequest>(entity); // Yêu cầu cập nhật vị trí
            }
        }

        // Tạo sự kiện báo hiệu một nước đi đã hoàn thành
        ecb.CreateEntity(moveCompletedArchetype);

        // Yêu cầu kiểm tra điều kiện thắng/hoàn thành hàng đợi
        ecb.CreateEntity(checkWinConditionArchetype);
    }

    private Entity FindTopItem(ref SystemState state, int queueIndex)
    {
        Entity topItemEntity = Entity.Null;
        int lowestIndex = int.MaxValue;
        foreach (var (item, entity) in SystemAPI.Query<RefRO<ItemData>>().WithEntityAccess())
        {
            if (item.ValueRO.QueueIndex == queueIndex)
            {
                if (item.ValueRO.IndexInQueue < lowestIndex)
                {
                    lowestIndex = item.ValueRO.IndexInQueue;
                    topItemEntity = entity;
                }
            }
        }
        return topItemEntity;
    }

    private Entity FindBotItem(ref SystemState state, int queueIndex)
    {
        Entity botItemEntity = Entity.Null;
        int highestIndex = -1;
        foreach (var (item, entity) in SystemAPI.Query<RefRO<ItemData>>().WithEntityAccess())
        {
            if (item.ValueRO.QueueIndex == queueIndex)
            {
                // Bỏ qua các ô trống
                if (item.ValueRO.Type == 0) continue;

                if (item.ValueRO.IndexInQueue > highestIndex)
                {
                    highestIndex = item.ValueRO.IndexInQueue;
                    botItemEntity = entity;
                }
            }
        }
        return botItemEntity;
    }
}