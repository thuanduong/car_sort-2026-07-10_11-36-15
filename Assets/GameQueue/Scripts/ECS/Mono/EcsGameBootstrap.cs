using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Splines;

public class EcsGameBootstrap : MonoBehaviour
{
    [Header("Game Settings (From Inspector)")]
    public int queueCount = 5;
    public int maxItemPerQueue = 6;
    public List<QueueAnchor> queueAnchors;
    public float itemSpacingY = 1.2f;
    public float durationMove = 0.3f;
    public float durationFlip = 0.4f;
    public float durationPush = 0.32f;
    public float delayPerRow = 0.1f;
    public float queueSpacingX = 1.5f;
    public Transform dummyPosition;
    public Transform startRowPosition;
    public List<SplineContainer> lines;
    public List<Transform> jumpToPositions;
    
    public float centerLineLength = 9;
    public float minScale = 0.8f;
    public float maxScale = 1.0f;

    public Vector2 startLinePosition = new Vector2(0, 6.55f);
    public float extraLineLength = 0.5f;
    public Vector2 arrowPosition = new Vector2(0, -4.0f);
    public float extraFlagLength = 1.5f;

    public int LayerCarNormal = 20;
    public int LayerCarJump = 32;
    public int LayerCarJumpHigher = 34;

    [Header("Prefabs & Scene References")]
    public GameObject[] itemPrefabs;
    public GameObject queueAnchorPrefab;
    public GameObject dummyAnchorPrefab;
    public GameObject lineObjectPrefab;
    public GameObject arrowObjectPrefab;
    public GameObject flagObjectPrefab;
    public Transform levelRoot;
    public GameController gameController;

    // ECS Core
    private World ecsWorld;
    private EntityManager entityManager;

    // Hybrid Rendering & Input
    private Dictionary<Entity, GameObject> entityGameObjectMap = new Dictionary<Entity, GameObject>();
    private Dictionary<Entity, CarController> entityAnimatorMap = new Dictionary<Entity, CarController>();
    private Dictionary<Entity, bool> entityMovingState = new Dictionary<Entity, bool>(); // true = moving, false = idle
    private Dictionary<Entity, bool> entityPushingState = new Dictionary<Entity, bool>(); // true = pushing, false = not pushing
    private Dictionary<Entity, SpriteRenderer> entityRendererMap = new Dictionary<Entity, SpriteRenderer>();
    private List<GameObject> activeGameObjects = new List<GameObject>();
    private EntityQuery gameWonQuery;
    private EntityQuery moveCompletedQuery;
    private EntityQuery queueCompletedQuery;
    private EntityQuery moveStartedQuery;
    private List<Dictionary<Entity, ItemData>> _moveHistory = new List<Dictionary<Entity, ItemData>>();
    public bool IsPlaying {get; private set;} = false;
    public bool IsUIBlockingInput { get; set; } = false;
    public bool IsSwapSkillActive { get; set; } = false;

    private int itemsPerQueue = 5;
    
    private int _level, _move;
    private int _maxMove = 1000;
    private bool _isGameOverPending = false;
    
    // --- Idle Animation ---
    private float _idleAnimationTimer = 0f;
    private float _idleAnimationInterval = 2.5f; // Thời gian chờ ban đầu
    private Entity _lastAnimatedEntity = Entity.Null;

    void Start()
    {
        IsPlaying = false;
        gameController.OnRestartGame += RestartGame;

        InitializeEcsWorld();
        StartGame().Forget();
    }

    void Update()
    {
        if (ecsWorld != null && ecsWorld.IsCreated && ecsWorld.Systems.Count > 0)
        {
            ecsWorld.Update();
            UpdateGameObjectTransforms();
            CheckForWinCondition();
            CheckForNewMove();
            CheckForMoveStarted();
            CheckForQueueCompleted();
            UpdateIdleCarAnimations();
        }
    }

    void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.OnRestartGame -= RestartGame;
        }

        if (ecsWorld != null && ecsWorld.IsCreated)
        {
            ecsWorld.Dispose();
        }
    }

    private void InitializeEcsWorld()
    {
        ecsWorld = new World("SortQueueWorld");
        entityManager = ecsWorld.EntityManager;

        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(ecsWorld, systems);
        
        ecsWorld.CreateSystem<InputSystem>();
        ecsWorld.CreateSystem<SortLogicSystem>();
        ecsWorld.CreateSystem<PositionUpdateSystem>();
        ecsWorld.CreateSystem<PushAnimationSystem>(); 
        ecsWorld.CreateSystem<MovementSystem>();
        ecsWorld.CreateSystem<WinConditionSystem>();
        ecsWorld.CreateSystem<RedoSystem>();
        ecsWorld.CreateSystem<SwapSkillSystem>(); 
        gameWonQuery = entityManager.CreateEntityQuery(typeof(GameWonEvent));
        moveCompletedQuery = entityManager.CreateEntityQuery(typeof(SortLogicSystem.MoveCompletedEvent));
        queueCompletedQuery = entityManager.CreateEntityQuery(typeof(SortLogicSystem.QueueCompletedEvent));
        moveStartedQuery = entityManager.CreateEntityQuery(typeof(SortLogicSystem.MoveStartedEvent));
    }

    public async UniTask StartGame()
    {
        ClearOldGame();
        _level = gameController.CurrentLevel;
        MapData mapData = LoadMapData(_level);
        this._maxMove = mapData.MaxMove;
        this.queueCount = mapData.NumQueue;
        this.itemsPerQueue = mapData.NumPerRow;

        _move = this._maxMove;
        gameController.UpdateMove(_move);
        await GenerateLevelEntities(mapData);
        ecsWorld.Unmanaged.GetExistingSystemState<WinConditionSystem>().Enabled = true;
        ecsWorld.Unmanaged.GetExistingSystemState<InputSystem>().Enabled = true;
        gameController.RestartGame();
        IsPlaying = true;
    }

    private async UniTask RestartGame()
    {
        await StartGame();
    }

    public void ClearOldGame()
    {
        entityManager.DestroyEntity(entityManager.UniversalQuery);

        foreach (var go in activeGameObjects)
        {
            Destroy(go);
        }
        activeGameObjects.Clear();
        entityGameObjectMap.Clear();
        entityAnimatorMap.Clear();
        entityMovingState.Clear();
        entityPushingState.Clear();
        entityRendererMap.Clear();
        _moveHistory.Clear();
        
        IsSwapSkillActive = false;
        var moveInProgressQuery = entityManager.CreateEntityQuery(typeof(SortLogicSystem.MoveInProgressEvent));
        if (!moveInProgressQuery.IsEmpty)
        {
            entityManager.DestroyEntity(moveInProgressQuery);
        }
        _isGameOverPending = false;
    }

    private async UniTask GenerateLevelEntities(MapData mapData)
    {
        var anchorPositions = new FixedList512Bytes<float3>();
        var splineItemPositions = new FixedList512Bytes<float3>(); 
        var splineItemRotations = new FixedList512Bytes<quaternion>(); 
        var splineItemScalers = new FixedList512Bytes<float>();
        var _jumpToPositions = new FixedList512Bytes<float3>();
        for (int i = 0; i < queueCount; i++)
        {
            var qa = queueAnchors[i];
            qa.queueIndex = i;
            anchorPositions.Add(qa.transform.position);
            for (int j = 0; j < itemsPerQueue; j++)
            {
                var progress = GetEvaluatePositionOnLine(j);
                splineItemPositions.Add(GetItemPositionFromSpline(progress, i, j));
                splineItemRotations.Add(GetItemRotationFromSpline(progress, i, j));
                if (i == 0) 
                    splineItemScalers.Add(GetItemScaler(progress));
            }

            
            _jumpToPositions.Add(jumpToPositions[i].position);
        }
    
        Vector3 dPos = dummyPosition.position;
        GameObject dummyObj = Instantiate(dummyAnchorPrefab, dPos, Quaternion.identity, levelRoot);
        dummyObj.name = "DummyAnchor";
        activeGameObjects.Add(dummyObj);
        Entity settingsEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(settingsEntity, new GameSettings
        {
            QueueCount = this.queueCount,
            ItemsPerQueue = this.itemsPerQueue,
            ItemSpacingY = this.itemSpacingY,
            DurationMove = this.durationMove,
            DurationFlip = this.durationFlip,
            DurationPush = this.durationPush,
            DelayPerRow = this.delayPerRow,
            MaxMove = this._maxMove,
            QueueAnchorPositions = anchorPositions,
            SplineItemPositions = splineItemPositions,
            SplineItemRotations = splineItemRotations,
            SplineItemScalers = splineItemScalers,
            JumpToPositions = _jumpToPositions,
            
            DummyAnchorPosition = dPos
        });

        entityManager.AddComponentObject(settingsEntity, this);

        int poolIdx = 0;
        for (int i = 0; i < mapData.NumQueue; i++)
        {
            for (int j = 0; j < mapData.NumPerRow; j++)
            {
                int currentType = mapData.Map[poolIdx++];
                if (currentType == -1) continue; 
                var progress = GetEvaluatePositionOnLine(j);
                var itemPos = GetItemPositionFromSpline(progress, i, j);
                var itemRot = GetItemRotationFromSpline(progress, i, j);
                var itemScaler = GetItemScaler(progress);
                CreateItemEntityAndGameObject(currentType, i, j, itemPos, itemRot, itemScaler, false); 
            }
            await UniTask.Yield(); 
        }
        int dummyType = mapData.DummyType;
        CreateItemEntityAndGameObject(dummyType, -1, 0, dPos, quaternion.identity, GetItemScaler(1.0f), true);

    }
    
    /// <summary>
    private Vector3 CalculateLayoutStartPosition()
    {
        float totalWidth = (queueCount - 1) * queueSpacingX;
        float totalHeight = (itemsPerQueue - 1) * itemSpacingY;
        return startRowPosition.position - new Vector3(totalWidth / 2f, totalHeight, 0);
    }
    private Vector3 GetItemPosition(float3 anchorPosition, int itemIndexInQueue)
    {
        return (Vector3)anchorPosition + Vector3.up * (itemIndexInQueue * itemSpacingY);
    }

    private float GetEvaluatePositionOnLine(int itemIndexInQueue)
    {
        var offsetY = ((itemsPerQueue - 1) - itemIndexInQueue) * itemSpacingY; 
        return offsetY / centerLineLength;
    }

    private Vector3 GetItemPositionFromSpline(float t, int queueIndex, int itemIndexInQueue)
    {
        var spline = lines[queueIndex];
        var position = spline.EvaluatePosition(t);
        return new Vector3(position.x, position.y, position.z);
    }

    private quaternion GetItemRotationFromSpline(float t, int queueIndex, int itemIndexInQueue)
    {
        return quaternion.identity;
        if (queueIndex < 0 || queueIndex >= lines.Count)
        {
            return quaternion.identity; 
        }
        var spline = lines[queueIndex];
        var tangent  = spline.EvaluateTangent(t);
        float angle = math.atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        return quaternion.Euler(0, 0, math.radians(angle));
    }

    private float GetItemScaler(float progress)
    {
        return 1.0f;
        return Mathf.Lerp(minScale, maxScale, progress);
    }

    private MapData LoadMapData(int level)
    {
        string path = $"Levels/level_{level}";
        TextAsset textAsset = Resources.Load<TextAsset>(path);

        if (textAsset != null)
        {
            Debug.Log($"[EcsGameBootstrap] Đã tìm thấy và tải level từ file: {path}.json");
            return JsonUtility.FromJson<MapData>(textAsset.text);
        }
        else
        {
            Debug.LogWarning($"[EcsGameBootstrap] Không tìm thấy file level tại: Resources/{path}.json. Sẽ tạo level ngẫu nhiên.");
            return GenerateRandomMapData();
        }
    }

    private MapData GenerateRandomMapData()
    {
        var mapData = new MapData
        {
            MapLevel = _level,
            NumQueue = this.queueCount,
            NumPerRow = this.itemsPerQueue,
            Map = new List<int>()
        };

        List<int> pool = Enumerable.Range(1, queueCount).SelectMany(type => Enumerable.Repeat(type, itemsPerQueue)).ToList();
        pool.Add(0); // Add dummy type

        var shuffledPool = pool.OrderBy(x => UnityEngine.Random.value).ToList();
        mapData.DummyType = shuffledPool.Last();
        mapData.Map = shuffledPool.Take(queueCount * itemsPerQueue).ToList();
        return mapData;
    }

    private void CreateItemEntityAndGameObject(int type, int queueIndex, int indexInQueue, Vector3 position, quaternion rotation, float scaler, bool isDummy = false)
    {
        Entity entity = entityManager.CreateEntity();
        entityManager.AddComponentData(entity, new ItemData
        {
            Type = type,
            QueueIndex = queueIndex,
            IndexInQueue = indexInQueue
        });
        
        entityManager.AddComponentData(entity, new LocalTransform { Position = position, Rotation = rotation, Scale = scaler });
        entityManager.AddComponent<LocalToWorld>(entity); 

        if (isDummy)
        {
            entityManager.AddComponent<DummyData>(entity);
        }
        if (type >= 0 && type < itemPrefabs.Length)
        {
            GameObject prefab = itemPrefabs[type]; 
            GameObject itemGO = Instantiate(prefab, position, rotation, levelRoot);
            itemGO.transform.localScale = new Vector3(scaler, scaler, scaler);
            entityGameObjectMap[entity] = itemGO;
            activeGameObjects.Add(itemGO);
            CarController animator = itemGO.GetComponent<CarController>();
            if (animator != null)
            {
                entityAnimatorMap[entity] = animator;
                entityMovingState[entity] = false; 
                entityPushingState[entity] = false; 
                if (isDummy)
                {
                    animator.car_eye.SetBool("is_look", true);
                }
                animator.TurnOffSheen();
                animator.TurnOnShadow();
            }

            SpriteRenderer spriteRenderer = animator.spriteRenderer;
            if (spriteRenderer != null)
            {
                entityRendererMap[entity] = spriteRenderer;
            }
        }
    }

    private void UpdateGameObjectTransforms()
    {
        if (IsPlaying == false) return;
        
        var gameSettingsEntity = entityManager.CreateEntityQuery(typeof(GameSettings)).GetSingletonEntity();
        var gameSettings = entityManager.GetComponentData<GameSettings>(gameSettingsEntity);
        var entities = new NativeArray<Entity>(entityGameObjectMap.Keys.ToArray(), Allocator.Temp);
        foreach (var entity in entities)
        {
            if (!entityManager.Exists(entity) || !entityGameObjectMap.ContainsKey(entity))
            {
                entityGameObjectMap.Remove(entity);
                entityAnimatorMap.Remove(entity);
                entityMovingState.Remove(entity);
                entityPushingState.Remove(entity);
                entityRendererMap.Remove(entity);
                continue;
            }

            GameObject go = entityGameObjectMap[entity];
            if (go == null) continue;
            
            var newTransform = entityManager.GetComponentData<LocalTransform>(entity);
            bool isMovingNow = entityManager.HasComponent<MoveTo>(entity);
            bool isPushingNow = entityManager.HasComponent<SortLogicSystem.PushAnimationRequest>(entity);
            bool wasMoving = entityMovingState.ContainsKey(entity) && entityMovingState[entity];
            bool wasPushing = entityPushingState.ContainsKey(entity) && entityPushingState[entity];

            if (isPushingNow && !wasPushing)
            {
                var pushRequest = entityManager.GetComponentData<SortLogicSystem.PushAnimationRequest>(entity);
                if (entityAnimatorMap.ContainsKey(pushRequest.PushingEntity))
                {
                    entityAnimatorMap[pushRequest.PushingEntity].car_anim.SetTrigger("push");
                    entityAnimatorMap[pushRequest.PushingEntity].car_eye.SetBool("is_open", true);
                }
            }

            if (isMovingNow && !wasMoving)
            {
                var moveTo = entityManager.GetComponentData<MoveTo>(entity);
                
                if (moveTo.MoveType == 1 || moveTo.MoveType == 2)
                {
                    if (entityAnimatorMap.ContainsKey(entity))
                    {
                        entityAnimatorMap[entity].car_anim.SetTrigger("flip");
                        entityAnimatorMap[entity].car_eye.SetBool("is_look", false);
                        entityAnimatorMap[entity].car_eye.SetBool("is_open", false);
                        entityAnimatorMap[entity].TurnOffShadow();
                        V3.Component.SoundComponent.Instance?.PlaySFX("jump");
                    }
                    if (entityRendererMap.ContainsKey(entity))
                    {
                        entityRendererMap[entity].sortingOrder = moveTo.MoveType == 2 ? LayerCarJumpHigher : LayerCarJump;
                    }
                }
                else
                {
                    if (entityAnimatorMap.ContainsKey(entity))
                    {
                        entityAnimatorMap[entity].car_eye.SetBool("is_open", true);
                        entityAnimatorMap[entity].vfx.Play();
                    }
                }
            }
            else if (!isMovingNow && wasMoving)
            {
                if (entityAnimatorMap.ContainsKey(entity))
                {
                    var itemData = entityManager.GetComponentData<ItemData>(entity);
                    entityAnimatorMap[entity].car_anim.SetTrigger("idle");
                    entityAnimatorMap[entity].TurnOnShadow();
                    if (itemData.QueueIndex < 0)
                    {
                        entityAnimatorMap[entity].car_eye.SetBool("is_look", true);
                    }
                    else
                    {
                        entityAnimatorMap[entity].car_eye.SetBool("is_open", false);
                        entityAnimatorMap[entity].car_eye.SetBool("is_look", false);
                    }
                }
                if (entityRendererMap.ContainsKey(entity))
                {
                    entityRendererMap[entity].sortingOrder = LayerCarNormal;
                }
            }
            
            go.transform.position = newTransform.Position;
            go.transform.rotation = newTransform.Rotation;
            entityMovingState[entity] = isMovingNow; 
            entityPushingState[entity] = isPushingNow; 
        }

        if (_isGameOverPending && entityManager.CreateEntityQuery(typeof(MoveTo)).IsEmpty)
        {
            _isGameOverPending = false; 
            IsPlaying = false;
            gameController.FinishGame(false); 
        }
        entities.Dispose();
    }

    private void CheckForWinCondition()
    {
        if (!gameWonQuery.IsEmpty)
        {
            IsPlaying = false;
            ecsWorld.Unmanaged.GetExistingSystemState<InputSystem>().Enabled = false;
            gameController.FinishGame(true);
            entityManager.DestroyEntity(gameWonQuery);
        }
    }

    private void CheckForNewMove()
    {
        if (!moveCompletedQuery.IsEmpty)
        {
            NewMove();
            entityManager.DestroyEntity(moveCompletedQuery);
        }
    }

    private void CheckForQueueCompleted()
    {
        using (var eventEntities = queueCompletedQuery.ToEntityArray(Allocator.TempJob))
        {
            if (eventEntities.Length > 0)
            {
                var gameSettings = entityManager.GetComponentData<GameSettings>(entityManager.CreateEntityQuery(typeof(GameSettings)).GetSingletonEntity());

                foreach (var entity in eventEntities)
                {
                    var eventData = entityManager.GetComponentData<SortLogicSystem.QueueCompletedEvent>(entity);
                    int queueIndex = eventData.QueueIndex;
                    Debug.Log($"Complete Queue {queueIndex}");
                    Vector3 anchorPos = gameSettings.QueueAnchorPositions[queueIndex];
                    Vector3 flagPos = anchorPos - new Vector3(0, extraFlagLength, 0);
                    GameObject flagObj = Instantiate(flagObjectPrefab, flagPos, Quaternion.identity, levelRoot);
                    flagObj.transform.localScale = Vector3.zero;
                    flagObj.transform.DOScale(1, 0.5f).SetEase(Ease.OutBack);
                    activeGameObjects.Add(flagObj);
                    using (var carEntities = entityManager.CreateEntityQuery(typeof(ItemData)).ToEntityArray(Allocator.Temp))
                    {
                        foreach (var carEntity in carEntities)
                        {
                            if (entityManager.GetComponentData<ItemData>(carEntity).QueueIndex == queueIndex && entityAnimatorMap.TryGetValue(carEntity, out var carController))
                            {
                                carController.car_eye.SetBool("is_open", true);
                                carController.car_eye.SetBool("is_happy", true);
                                carController.TurnOnSheen();

                                Sequence headShakeSequence = DOTween.Sequence();
                                headShakeSequence.Append(carController.transform.DORotate(new Vector3(0, 0, 6), 0.25f).SetEase(Ease.Linear))
                                                 .Append(carController.transform.DORotate(new Vector3(0, 0, -6), 0.25f).SetEase(Ease.Linear))
                                                 .SetLoops(8, LoopType.Yoyo); 

                                headShakeSequence.OnComplete(() => carController.transform.DORotate(Vector3.zero, 0.1f));
                            }
                        }
                    }
                }
                V3.Component.SoundComponent.Instance?.PlaySFX("QueueComplete");
                entityManager.DestroyEntity(queueCompletedQuery);
            }
        }
    }
    private void CheckForMoveStarted()
    {
        if (!moveStartedQuery.IsEmpty)
        {
            var currentState = new Dictionary<Entity, ItemData>();
            using (var entities = entityManager.CreateEntityQuery(typeof(ItemData)).ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    currentState[entity] = entityManager.GetComponentData<ItemData>(entity);
                }
            }
            _moveHistory.Add(currentState);
            if (_moveHistory.Count > 20)
            {
                _moveHistory.RemoveAt(0);
            }
            V3.Component.SoundComponent.Instance?.PlaySFX("click");
            entityManager.DestroyEntity(moveStartedQuery);
        }
    }

    public void TriggerRedo()
    {
        if (_moveHistory.Count > 0)
        {
            var redoRequest = entityManager.CreateEntity();
            entityManager.AddComponent<RedoSystem.RedoRequest>(redoRequest);
        }
    }
    
    public void TriggerRedoSwap()
    {
        IsSwapSkillActive = true;
    }

    public void TriggerOpenEye(Entity entity)
    {
        entityAnimatorMap[entity].car_eye.SetTrigger("open_close");
    }

    public void NewMove()
    {
        if (!IsPlaying) return;

        _move--;
        gameController.UpdateMove(_move);
        if (_move <= 0 && _isGameOverPending == false)
        {
            ecsWorld.Unmanaged.GetExistingSystemState<InputSystem>().Enabled = false;
            _isGameOverPending = true;
        }
    }

    public void BackMove()
    {
        _move++;
        gameController.UpdateMove(_move);
    }

    public Dictionary<Entity, ItemData> PopMoveHistory()
    {
        if (_moveHistory.Count > 0)
        {
            var lastState = _moveHistory[_moveHistory.Count - 1];
            _moveHistory.RemoveAt(_moveHistory.Count - 1);
            return lastState;
        }
        return null;
    }

    public Dictionary<Entity, GameObject> GetEntityGameObjectMap()
    {
        return entityGameObjectMap;
    }

    private void UpdateIdleCarAnimations()
    {
        if (!IsPlaying || IsUIBlockingInput || !entityManager.CreateEntityQuery(typeof(SortLogicSystem.MoveInProgressEvent)).IsEmpty)
        {
            _idleAnimationTimer = 0; 
            return;
        }

        _idleAnimationTimer += Time.deltaTime;
        if (_idleAnimationTimer >= _idleAnimationInterval)
        {
            _idleAnimationTimer = 0f;
            _idleAnimationInterval = UnityEngine.Random.Range(1.5f, 3.5f);

            var eligibleCars = new List<Entity>();
            using (var entities = entityManager.CreateEntityQuery(typeof(ItemData)).ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var itemData = entityManager.GetComponentData<ItemData>(entity);
                    if (itemData.QueueIndex >= 0 && entity != _lastAnimatedEntity)
                    {
                        eligibleCars.Add(entity);
                    }
                }
            }

            if (eligibleCars.Count > 0)
            {
                _lastAnimatedEntity = eligibleCars[UnityEngine.Random.Range(0, eligibleCars.Count)];
                TriggerOpenEye(_lastAnimatedEntity);
            }
        }
    }
}
