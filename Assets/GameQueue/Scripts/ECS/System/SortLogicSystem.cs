using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SortLogicSystem : ISystem
{
    public struct MoveCompletedEvent : IComponentData
    {
    }
    
    public struct MoveStartedEvent : IComponentData
    {
    }

    public struct MoveInProgressEvent : IComponentData
    {
    }

    public struct PushAnimationRequest : IComponentData
    {
        public Entity PushingEntity; // Entity thực hiện hành động push
        public float Duration;
        public float ElapsedTime;
    }

    public struct PreSwapMoveRequest : IComponentData
    {
        public int ClickedQueueIndex;
    }

    public struct PushSequenceRequest : IComponentData
    {
        public int ClickedQueueIndex;
    }

    public struct ExecuteSwapEvent : IComponentData
    {
        public int ClickedQueueIndex;
    }

    public struct QueueCompletedEvent : IComponentData
    {
        public int QueueIndex;
    }

    public struct QueueCompletedTag : IComponentData
    {
    }

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
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<SortRequest, PreSwapMoveRequest, PushSequenceRequest, ExecuteSwapEvent>().Build());
        state.RequireForUpdate<DummyData>();
        state.RequireForUpdate<GameSettings>();
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

        int firstItemTypeInQueue = -1;
        int itemsInThisQueue = 0;
        bool isMixedType = false; 

        foreach (var itemData in SystemAPI.Query<RefRO<ItemData>>())
        {
            if (itemData.ValueRO.QueueIndex == clickedQueueIndex)
            {
                if (itemData.ValueRO.Type == 0) continue;

                if (firstItemTypeInQueue == -1)
                {
                    firstItemTypeInQueue = itemData.ValueRO.Type;
                }
                else if (itemData.ValueRO.Type != firstItemTypeInQueue)
                {
                    isMixedType = true; 
                }
                itemsInThisQueue++;
            }
        }
        if (itemsInThisQueue == gameSettings.ItemsPerQueue && !isMixedType && firstItemTypeInQueue != -1)
        {
            ecb.DestroyEntity(sortRequestEntity); 
            return;
        }
        var dummyEntity = SystemAPI.GetSingletonEntity<DummyData>();
        var dummyData = SystemAPI.GetComponent<ItemData>(dummyEntity);
        Entity topItemEntity = FindTopItem(ref state, clickedQueueIndex);
        if (topItemEntity == Entity.Null)
        {
            UnityEngine.Debug.Log("[HandleSortRequest] Clicked on an empty queue.");
            ecb.DestroyEntity(sortRequestEntity);
            return;
        }
        ecb.AddComponent<PositionUpdateRequest>(dummyEntity);
        var newEntity = ecb.CreateEntity(preSwapMoveArchetype);
        ecb.AddComponent(newEntity, new PreSwapMoveRequest { ClickedQueueIndex = clickedQueueIndex });
        ecb.CreateEntity(moveInProgressArchetype);
        ecb.CreateEntity(moveStartedArchetype);
        ecb.DestroyEntity(sortRequestEntity);
    }

    private void HandlePushSequence(ref SystemState state, ref EntityCommandBuffer ecb, GameSettings gameSettings, int clickedQueueIndex)
    {
        Entity botItemEntity = FindBotItem(ref state, clickedQueueIndex);
        Entity dummyEntity = SystemAPI.GetSingletonEntity<DummyData>();

        if (botItemEntity != Entity.Null)
        {
            ecb.AddComponent(botItemEntity, new PushAnimationRequest { PushingEntity = dummyEntity, Duration = gameSettings.DurationPush, ElapsedTime = 0f });
        }
    }

    private void ExecuteSwap(ref SystemState state, ref EntityCommandBuffer ecb, GameSettings gameSettings, int clickedQueueIndex)
    {
        Entity dummyEntity = Entity.Null;
        ItemData dummyItemData = default;

        foreach (var (itemData, entity) in SystemAPI.Query<RefRO<ItemData>>().WithAll<DummyData>().WithEntityAccess())
        {
            dummyEntity = entity;
            dummyItemData = itemData.ValueRO;
            break; 
        }

        Entity topItemEntity = FindTopItem(ref state, clickedQueueIndex);

        if (dummyEntity == Entity.Null || topItemEntity == Entity.Null)
        {
            return;
        }

        var topItemData = SystemAPI.GetComponent<ItemData>(topItemEntity);
        if (dummyItemData.Type == 0 && topItemData.Type == 0)
        {
            return;
        }

        ecb.SetComponent(topItemEntity, new ItemData { Type = topItemData.Type, QueueIndex = -1, IndexInQueue = 0 });
        ecb.AddComponent<DummyData>(topItemEntity);
        ecb.AddComponent<PositionUpdateRequest>(topItemEntity);
        ecb.RemoveComponent<DummyData>(dummyEntity);
        ecb.SetComponent(dummyEntity, new ItemData { Type = dummyItemData.Type, QueueIndex = clickedQueueIndex, IndexInQueue = gameSettings.ItemsPerQueue - 1 });
        ecb.AddComponent<PositionUpdateRequest>(dummyEntity);
        foreach (var (itemData, entity) in SystemAPI.Query<RefRW<ItemData>>().WithEntityAccess())
        {
            if (itemData.ValueRO.QueueIndex == clickedQueueIndex && entity != topItemEntity && entity != dummyEntity)
            {
                itemData.ValueRW.IndexInQueue--;
                ecb.AddComponent<PositionUpdateRequest>(entity); 
            }
        }

        ecb.CreateEntity(moveCompletedArchetype);
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