using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Hệ thống xử lý logic cho kỹ năng "Swap".
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SwapSkillSystem : ISystem
{
    public struct SwapRequestEvent : IComponentData
    {
        public Entity ClickedCarEntity;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SwapRequestEvent>();
        state.RequireForUpdate<DummyData>();
        state.RequireForUpdate<GameSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var gameSettings = SystemAPI.GetSingleton<GameSettings>();
        var swapRequestEntity = SystemAPI.GetSingletonEntity<SwapRequestEvent>();
        var swapRequest = SystemAPI.GetComponent<SwapRequestEvent>(swapRequestEntity);

        Entity clickedCarEntity = swapRequest.ClickedCarEntity;
        Entity dummyEntity = SystemAPI.GetSingletonEntity<DummyData>();

        // Nếu không tìm thấy 1 trong 2 entity, hủy request và thoát
        if (clickedCarEntity == Entity.Null || dummyEntity == Entity.Null)
        {
            ecb.DestroyEntity(swapRequestEntity);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return;
        }

        var clickedCarData = SystemAPI.GetComponent<ItemData>(clickedCarEntity);
        var dummyData = SystemAPI.GetComponent<ItemData>(dummyEntity);

        var clickedCarTransform = SystemAPI.GetComponent<LocalTransform>(clickedCarEntity);
        var dummyTransform = SystemAPI.GetComponent<LocalTransform>(dummyEntity);

        // --- Bắt đầu hoán đổi vai trò ---

        // 1. Xe được click trở thành Dummy mới
        ecb.SetComponent(clickedCarEntity, new ItemData { Type = clickedCarData.Type, QueueIndex = -1, IndexInQueue = 0 });
        ecb.AddComponent<DummyData>(clickedCarEntity);
        ecb.AddComponent(clickedCarEntity, new MoveTo {
            StartPosition = clickedCarTransform.Position,
            EndPosition = dummyTransform.Position,
            Duration = gameSettings.DurationFlip,
            MoveType = 2
        });

        // 2. Dummy cũ trở thành xe mới trong hàng đợi
        ecb.RemoveComponent<DummyData>(dummyEntity);
        ecb.SetComponent(dummyEntity, new ItemData { Type = dummyData.Type, QueueIndex = clickedCarData.QueueIndex, IndexInQueue = clickedCarData.IndexInQueue });
        ecb.AddComponent(dummyEntity, new MoveTo {
            StartPosition = dummyTransform.Position,
            EndPosition = clickedCarTransform.Position,
            Duration = gameSettings.DurationFlip,
            MoveType = 1
        });

        var moveStartedEntity = ecb.CreateEntity();
        ecb.AddComponent<SortLogicSystem.MoveStartedEvent>(moveStartedEntity);
        var moveCompletedEntity = ecb.CreateEntity();
        ecb.AddComponent<SortLogicSystem.MoveCompletedEvent>(moveCompletedEntity);

        // Yêu cầu kiểm tra điều kiện thắng sau khi swap
        var checkWinConditionEntity = ecb.CreateEntity();
        ecb.AddComponent<SortLogicSystem.CheckWinConditionRequest>(checkWinConditionEntity);

        // Hủy entity request sau khi xử lý
        ecb.DestroyEntity(swapRequestEntity);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}