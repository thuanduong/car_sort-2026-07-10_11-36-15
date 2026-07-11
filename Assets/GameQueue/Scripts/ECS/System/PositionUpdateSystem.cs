using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Hệ thống này phản ứng với các yêu cầu cập nhật vị trí (PositionUpdateRequest).
/// Nó tính toán vị trí mục tiêu mới và thêm component MoveTo để kích hoạt animation di chuyển.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SortLogicSystem))]
public partial struct PositionUpdateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PositionUpdateRequest>();
        state.RequireForUpdate<GameSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var gameSettings = SystemAPI.GetSingleton<GameSettings>();

        // Duyệt qua tất cả các entity có yêu cầu cập nhật vị trí
        foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PositionUpdateRequest>().WithEntityAccess())
        {
            var itemData = SystemAPI.GetComponent<ItemData>(entity);
            float3 startPos = transform.ValueRO.Position;
            float3 endPos;
            float delay = 0;
            float duration = 0;
            bool isRedo = SystemAPI.HasComponent<RedoSystem.RedoMoveRequest>(entity);
            int moveType = 0;

            //if (SystemAPI.HasComponent<DummyData>(entity) && !SystemAPI.QueryBuilder().WithAll<SortLogicSystem.PreSwapMoveRequest>().Build().IsEmpty)
            if (!isRedo && SystemAPI.HasComponent<DummyData>(entity) && !SystemAPI.QueryBuilder().WithAll<SortLogicSystem.PreSwapMoveRequest>().Build().IsEmpty)
            {
                // Phase 1: Dummy di chuyển đến vị trí dưới cùng của hàng đợi được click
                var preSwapData = SystemAPI.GetSingleton<SortLogicSystem.PreSwapMoveRequest>();
                float3 anchorPos = gameSettings.QueueAnchorPositions[preSwapData.ClickedQueueIndex];
                // endPos = anchorPos + new float3(0, gameSettings.ItemsPerQueue * gameSettings.ItemSpacingY, 0);
                endPos = gameSettings.JumpToPositions[preSwapData.ClickedQueueIndex];
                duration = gameSettings.DurationFlip;
                moveType = 1;
            }
            else if (itemData.QueueIndex < 0) // Đây là dummy item ở vị trí mặc định
            {
                // Di chuyển về vị trí chờ mặc định
                endPos = gameSettings.DummyAnchorPosition;
                delay = (gameSettings.ItemsPerQueue - itemData.IndexInQueue) * gameSettings.DelayPerRow;
                duration = gameSettings.DurationMove;
                if (isRedo)
                {
                    ecb.RemoveComponent<RedoSystem.RedoMoveRequest>(entity);
                    moveType = 1;
                    duration = gameSettings.DurationFlip;
                }
            }
            else // Đây là item trong hàng đợi
            {
                // Lấy vị trí đã được tính toán trước từ GameSettings
                int positionIndex = itemData.QueueIndex * gameSettings.ItemsPerQueue + itemData.IndexInQueue;
                endPos = gameSettings.SplineItemPositions[positionIndex];

                delay = (gameSettings.ItemsPerQueue - itemData.IndexInQueue) * gameSettings.DelayPerRow;
                duration = gameSettings.DurationMove;
                if (isRedo)
                {
                    ecb.RemoveComponent<RedoSystem.RedoMoveRequest>(entity);
                    moveType = 0;
                    duration += 0.2f;
                }
            }
            
            // Thêm component MoveTo để MovementSystem xử lý
            ecb.AddComponent(entity, new MoveTo
            {
                StartPosition = startPos,
                EndPosition = endPos,
                Duration = duration,
                Delay = delay,
                ElapsedTime = 0f,
                MoveType = moveType
            });

            ecb.RemoveComponent<PositionUpdateRequest>(entity); // Xóa yêu cầu sau khi xử lý
            
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}