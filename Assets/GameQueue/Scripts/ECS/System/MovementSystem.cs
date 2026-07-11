using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Hệ thống này cập nhật vị trí của các entity có component MoveTo,
/// tạo ra hiệu ứng di chuyển mượt mà theo thời gian.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PositionUpdateSystem))]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Dùng .WithEntityAccess() để có thể xóa component khi di chuyển xong
        foreach (var (transform, moveTo, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<MoveTo>>().WithEntityAccess())
        {
            moveTo.ValueRW.ElapsedTime += deltaTime;
            if (moveTo.ValueRO.ElapsedTime < moveTo.ValueRO.Delay)
                continue;

            float t = math.saturate((moveTo.ValueRO.ElapsedTime - moveTo.ValueRO.Delay)/ moveTo.ValueRO.Duration);

            // Nội suy vị trí (Lerp)
            transform.ValueRW.Position = math.lerp(moveTo.ValueRO.StartPosition, moveTo.ValueRO.EndPosition, t);

            // Nếu đã đến nơi, xóa component MoveTo
            if (t >= 1.0f)
            {
                ecb.RemoveComponent<MoveTo>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}