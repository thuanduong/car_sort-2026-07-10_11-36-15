using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Hệ thống xử lý chức năng "Redo" (làm lại nước đi).
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct RedoSystem : ISystem
{
    public struct RedoRequest : IComponentData { }
    public struct RedoMoveRequest : IComponentData { }
    private EntityQuery _redoRequestQuery;
    private EntityQuery _itemQuery;

    public void OnCreate(ref SystemState state)
    {
        _redoRequestQuery = state.GetEntityQuery(typeof(RedoRequest));
        _itemQuery = state.GetEntityQuery(typeof(ItemData));
        state.RequireForUpdate(_redoRequestQuery);
    }

    public void OnDestroy(ref SystemState state) { }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var ecsBootstrap = SystemAPI.ManagedAPI.GetSingleton<EcsGameBootstrap>();

        var lastState = ecsBootstrap.PopMoveHistory();
        if (lastState == null)
        {
            // Không có gì để Redo, hủy request và thoát
            ecb.DestroyEntity(_redoRequestQuery, EntityQueryCaptureMode.AtPlayback);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return;
        }

        // Khôi phục lại số lượt đi
        ecsBootstrap.BackMove();

        // Khôi phục trạng thái
        foreach (var pair in lastState)
        {
            Entity entity = pair.Key;
            ItemData previousItemData = pair.Value;

            if (state.EntityManager.Exists(entity))
            {
                state.EntityManager.SetComponentData(entity, previousItemData);

                // Yêu cầu cập nhật lại vị trí và các component khác
                ecb.AddComponent<PositionUpdateRequest>(entity);
                ecb.AddComponent<RedoMoveRequest>(entity); // Thêm component đánh dấu
                if (previousItemData.QueueIndex == -1)
                    ecb.AddComponent<DummyData>(entity);
                else
                    ecb.RemoveComponent<DummyData>(entity);
            }
        }

        // Hủy request sau khi xử lý
        ecb.DestroyEntity(_redoRequestQuery, EntityQueryCaptureMode.AtPlayback);
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}