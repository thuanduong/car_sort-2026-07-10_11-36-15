using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem;
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

        m_MovingItemsQuery = GetEntityQuery(typeof(MoveTo));
        m_Bootstrap = Object.FindAnyObjectByType<EcsGameBootstrap>();
        m_MoveInProgressQuery = GetEntityQuery(typeof(SortLogicSystem.MoveInProgressEvent));
        _queueLayerMask = (1 << 8);
        _carLayerMask = (1 << 9);
    }

    protected override void OnUpdate()
    {
        if (m_Bootstrap != null && m_Bootstrap.IsUIBlockingInput) { 
            _pressedThisFrame = false;
            _releasedThisFrame = false;
            return;
        }

        if (!_isPressing && _releasedThisFrame) 
        {

            _screenPosition = _inputActions.GameActionMap.Position.ReadValue<Vector2>();
            if (!m_MoveInProgressQuery.IsEmpty)
            {
                Debug.Log("Moving");
                return;
            }
            if (Camera.main == null) return;
            
            Vector3 inputPosition = new Vector3(_screenPosition.x, _screenPosition.y, -10.0f);
            
            if (m_Bootstrap != null && m_Bootstrap.IsSwapSkillActive)
            {
                RaycastHit2D carHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(inputPosition), Vector2.zero, Mathf.Infinity, _carLayerMask);
                if (carHit.collider != null && carHit.collider.TryGetComponent<CarController>(out var carController))
                {
                    foreach (var pair in m_Bootstrap.GetEntityGameObjectMap())
                    {
                        if (pair.Value == carHit.collider.gameObject)
                        {
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