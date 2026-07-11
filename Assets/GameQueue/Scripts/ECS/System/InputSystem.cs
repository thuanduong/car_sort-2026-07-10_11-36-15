using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem;

/// <summary>
/// Hệ thống này chạy trên Main Thread để bắt sự kiện input từ người dùng.
/// Khi một QueueAnchor được click, nó sẽ tạo ra một entity sự kiện ClickedQueueEvent.
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))] 
public partial class InputSystem : SystemBase
{
    private EntityQuery m_MovingItemsQuery;
    private EntityQuery m_MoveInProgressQuery;
    private EcsGameBootstrap m_Bootstrap;
    private int _queueLayerMask;
    private int _carLayerMask;

    private GameInput _inputActions;
    private float2 _screenPosition;
    private bool _isPressing;
    private bool _pressedThisFrame;
    private bool _releasedThisFrame;

    protected override void OnCreate()
    {
        _inputActions = new GameInput();
        _inputActions.GameActionMap.Click.performed += OnGlobalClickStarted;
        _inputActions.GameActionMap.Click.canceled += OnGlobalClickEnded;
        _inputActions.Enable();

        // Tạo một query để kiểm tra các item đang di chuyển.
        m_MovingItemsQuery = GetEntityQuery(typeof(MoveTo));
        // Tìm bootstrap trong scene để có thể tương tác với các đối tượng MonoBehavior
        m_Bootstrap = Object.FindAnyObjectByType<EcsGameBootstrap>();
        m_MoveInProgressQuery = GetEntityQuery(typeof(SortLogicSystem.MoveInProgressEvent));
        // Layer 8 cho Queue, Layer 9 cho Car
        _queueLayerMask = (1 << 8);
        _carLayerMask = (1 << 9);
    }

    protected override void OnUpdate()
    {
        // 2. Xử lý input từ người dùng
        // Nếu có popup đang hiển thị, không xử lý input game
        if (m_Bootstrap != null && m_Bootstrap.IsUIBlockingInput) { 
            _pressedThisFrame = false;
            _releasedThisFrame = false;
            return;
        }

        // if (!Input.GetMouseButtonDown(0)) return;
        if (!_isPressing && _releasedThisFrame) 
        {

            _screenPosition = _inputActions.GameActionMap.Position.ReadValue<Vector2>();
            // Only get input when it release
            // if (!input.ReleasedThisFrame) return;
            // Debug.Log($"inputPosition {_screenPosition}");

            // Chỉ xử lý click mới khi không có lượt di chuyển nào đang diễn ra.
            // Đây là cách kiểm tra chính xác nhất, bao gồm cả các khoảng nghỉ giữa các phase.
            if (!m_MoveInProgressQuery.IsEmpty)
            {
                Debug.Log("Moving");
                return;
            }
            if (Camera.main == null) return;
            
            Vector3 inputPosition = new Vector3(_screenPosition.x, _screenPosition.y, -10.0f);
            

            // Logic cho Skill Swap
            if (m_Bootstrap != null && m_Bootstrap.IsSwapSkillActive)
            {
                // Khi skill swap active, raycast vào layer của Car
                RaycastHit2D carHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(inputPosition), Vector2.zero, Mathf.Infinity, _carLayerMask);
                if (carHit.collider != null && carHit.collider.TryGetComponent<CarController>(out var carController))
                {
                    // Tìm entity tương ứng với GameObject của xe
                    foreach (var pair in m_Bootstrap.GetEntityGameObjectMap())
                    {
                        if (pair.Value == carHit.collider.gameObject)
                        {
                            // Tạo event yêu cầu swap
                            var swapRequestEntity = EntityManager.CreateEntity();
                            EntityManager.AddComponentData(swapRequestEntity, new SwapSkillSystem.SwapRequestEvent { ClickedCarEntity = pair.Key });
                            
                            m_Bootstrap.IsSwapSkillActive = false;
                            return; 
                        }
                    }
                }
            }
            else 
            { 
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(inputPosition), Vector2.zero, Mathf.Infinity, _queueLayerMask);
                if (hit.collider != null && hit.collider.TryGetComponent<QueueAnchor>(out var anchor))
                {
                    var requestEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData(requestEntity, new ClickedQueueEvent { QueueIndex = anchor.queueIndex });
                    EntityManager.AddComponent<SortRequest>(requestEntity);
                }
            }
        }
        _pressedThisFrame = false;
        _releasedThisFrame = false;
    }

    protected override void OnDestroy()
    {
        if (_inputActions != null)
        {
            _inputActions.Disable();
            _inputActions.GameActionMap.Click.performed -= OnGlobalClickStarted;
            _inputActions.GameActionMap.Click.canceled -= OnGlobalClickEnded;
        }
    }

    private void OnGlobalClickStarted(InputAction.CallbackContext context)
    {
        _isPressing = true;
        _pressedThisFrame = true;
    }

    private void OnGlobalClickEnded(InputAction.CallbackContext context)
    {
        _isPressing = false;
        _releasedThisFrame = true;
    }
}