using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Hệ thống này theo dõi các item đang trong trạng thái "push" animation.
/// Khi animation kết thúc, nó sẽ tạo ra một ExecuteSwapEvent.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SortLogicSystem))]
public partial struct PushAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SortLogicSystem.PushAnimationRequest>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Duyệt qua các entity có yêu cầu push animation
        foreach (var (pushRequest, itemData, entity) in SystemAPI.Query<RefRW<SortLogicSystem.PushAnimationRequest>, RefRO<ItemData>>().WithEntityAccess())
        {
            pushRequest.ValueRW.ElapsedTime += deltaTime;

            // Nếu hết thời gian animation
            if (pushRequest.ValueRO.ElapsedTime >= pushRequest.ValueRO.Duration)
            {
                // Tạo sự kiện để thực hiện việc hoán đổi
                var swapEvent = ecb.CreateEntity();
                ecb.AddComponent(swapEvent, new SortLogicSystem.ExecuteSwapEvent
                {
                    ClickedQueueIndex = itemData.ValueRO.QueueIndex
                });

                // Xóa component request để không xử lý lại
                ecb.RemoveComponent<SortLogicSystem.PushAnimationRequest>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
