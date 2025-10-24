using Cysharp.Threading.Tasks;
using GameControl.Interface;
using GameControl.SO;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;

namespace GameControl.Controller
{
    public class SpawnerStateController : MMSingleton<SpawnerStateController>
    {
        [SerializeField] private Camera mainCamera;
        private ISpawnerState _currentState;
        
        private SpawnerState.StopState _stopState;
        private SpawnerState.SpawningState _spawningState;
        private SpawnerState.PauseState _pauseState;
        
        private float _currentEnemyPoint;
        private float _maxEnemyPoint;
        private float _increaseRateEnemyPoint;
        private float _defaultEnemySpawnTimer;
        
        [BoxGroup("Debug")] 
        [ShowInInspector, ReadOnly]
        private string _currentStateName;
        [BoxGroup("Debug")] 
        [ShowInInspector, ReadOnly]
        public float EnemyPoint => _currentEnemyPoint;
        [BoxGroup("Debug")] 
        [ShowInInspector, ReadOnly]
        private MapDataSO CurrentMap => GameStateController.Instance != null ? GameStateController.Instance.CurrentMap : null;

        
        [BoxGroup("Setting")] [SerializeField] private EnemySpawnerController _enemySpawnerController;
        [BoxGroup("Setting")] [SerializeField] private EnemyPatternController _enemyPatternController;
        [BoxGroup("Setting")] [SerializeField] private ItemSpawnerController _itemSpawnerController;
        [BoxGroup("Setting")] [SerializeField] private MapEventController _mapEventController;
        [BoxGroup("Setting")] [Required] [SerializeField] private Transform enemyParent;
        [BoxGroup("Setting")] [Required] [SerializeField] private Transform itemParent;
        [BoxGroup("Setting")] [SerializeField] private Vector2 regionSize = Vector2.zero;
        [BoxGroup("Setting")] [SerializeField] private Vector2 itemdropRegionSize = Vector2.zero;
        
        [BoxGroup("Debug Zone")] [SerializeField] private bool debugPattern;
        [BoxGroup("Debug Zone")] [SerializeField] private bool debugEnemy;
        [BoxGroup("Debug Zone")] [SerializeField] private bool debugMapEvent;
        
        [BoxGroup("Enable")] [SerializeField] private bool notSpawnEnemyOnStart = false;
        
        public MapEventController MapEventController => _mapEventController;
        public EnemySpawnerController EnemySpawnerController => _enemySpawnerController;
        public EnemyPatternController EnemyPatternController => _enemyPatternController;
        public ItemSpawnerController ItemSpawnerController => _itemSpawnerController;
        public Transform EnemyParent => enemyParent;
        public bool DisableEnemySpawn => notSpawnEnemyOnStart;
        public Transform ItemParent => itemParent;
        public Vector2 RegionSize => regionSize;
        public float EnemySpawnTimer { get => _defaultEnemySpawnTimer; set => _defaultEnemySpawnTimer = value; }
        public float ItemSpawnTimer
        {
            get => CurrentMap != null ? CurrentMap.defaultItemSpawnTimer : 0f;
            set { if (CurrentMap != null) CurrentMap.defaultItemSpawnTimer = value; }
        }

        public float CurrentEnemyPoint
        {
            get => _currentEnemyPoint;
            set => _currentEnemyPoint = Mathf.Clamp(value, 0, _maxEnemyPoint);
        }
        
        protected override void Awake()
        {
            base.Awake();
            _stopState = new SpawnerState.StopState();
            _spawningState = new SpawnerState.SpawningState();
            _pauseState = new SpawnerState.PauseState();
        }
        
        private void Start()
        {
            SetState(_stopState);
        }

        private void Update()
        {
            _currentState?.Update(this);
        }
        
        private void OnGUI()
        {
            if (!debugEnemy || _enemySpawnerController == null) return;
            
            var options = _enemySpawnerController.GetEnemyOption();
            if (options == null || options.Count == 0) return;

            int width = 250;
            int height = options.Count * 35;
            int x = 80;
            int y = Screen.height - height;

            GUILayout.BeginArea(new Rect(x, y, width, height));
            GUILayout.Label("<b><size=14>Enemy Chances</size></b>");
            foreach (var opt in options)
            {
                GUILayout.Label($"{opt.id} : {opt.Chance:F2}%");
            }
            GUILayout.EndArea();
        }

        public void SetState(ISpawnerState newState)
        {
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState?.Enter(this);
            _currentStateName = _currentState?.GetType().Name;
        }

        private void BindCancelToken()
        {
            var gsc = GameStateController.Instance;
            if (gsc?.sceneCts != null)
            {
                _enemyPatternController.BindCancellationToken(gsc.sceneCts.Token);
            }
        }

        public async UniTaskVoid SetupMapAndEnemy()
        {
            var gsc = GameStateController.Instance;
            _enemySpawnerController = new EnemySpawnerController(CurrentMap, this, regionSize, debugEnemy, mainCamera);
            _enemyPatternController = new EnemyPatternController(CurrentMap, this, regionSize, debugPattern);
            _itemSpawnerController = new ItemSpawnerController(CurrentMap, this, itemdropRegionSize);
            _mapEventController = new MapEventController(CurrentMap, this, debugMapEvent);
            _defaultEnemySpawnTimer = CurrentMap.defaultEnemySpawnTimer;
            
            await UniTask.WaitUntil(() => _enemySpawnerController != null && _enemyPatternController != null && _itemSpawnerController != null, cancellationToken: gsc.sceneCts.Token);
            
            BindCancelToken();
            _currentEnemyPoint = CurrentMap.startEnemyPoint;
            _maxEnemyPoint = CurrentMap.maxEnemyPoint;
            _increaseRateEnemyPoint = CurrentMap.rateIncreaseEnemyPoint;
            
            _itemSpawnerController.PrewarmItem();
            _enemyPatternController.SetEnemySpawner(_enemySpawnerController);
            _enemyPatternController.AddRandomPattern();
            
            RescheduleAllFromNow(CurrentMap);
            GameTimer.Instance.ScheduleOnceAtRemaining(62, () => PopupUIManager.Instance.ShowPopup("Warning", 2.0f, bypassStack: true));
            GameStateController.Instance.ScheduleRush();
        }
        
        public void OnMapModified()
        {
            var map = CurrentMap;
            if (map == null) return;
            
            _defaultEnemySpawnTimer = map.defaultEnemySpawnTimer;
            _maxEnemyPoint         = map.maxEnemyPoint;
            _increaseRateEnemyPoint= map.rateIncreaseEnemyPoint;
            _currentEnemyPoint     = Mathf.Min(_currentEnemyPoint, _maxEnemyPoint);
            
            _enemySpawnerController?.ReloadOptions(map.EnemyOptions);
            _enemyPatternController?.ReloadPatterns(map.PatternOptions);
            _enemyPatternController?.SetEnemySpawner(_enemySpawnerController);
            RescheduleAllFromNow(map);
            _enemyPatternController?.AddRandomPatterns((int)map.patternMax);
        }
        
        private void RescheduleAllFromNow(MapDataSO map)
        {
            var timer = GameTimer.Instance;
            if (timer == null || map == null) return;
            
            timer.CancelGroup("PATTERN::DECREASE");
            timer.CancelGroup("PATTERN::TRIGGER");
            timer.CancelGroup("PATTERN::ADD");
            timer.CancelGroup("SPAWNER::INTERVAL");
            timer.CancelGroup("SPAWNER::POINT");
            timer.CancelGroup("SPAWNER::CHANCE");
            timer.CancelGroup("SPAWNER::RATIO");

            // ---- PATTERN ----
            if (map.triggerTimeCanDecrease && map.patternDecreaseInterval > 0f)
                timer.ScheduleLoopingFromNow("PATTERN::DECREASE", CurrentMap.endlessMode , map.patternDecreaseInterval, () => _enemyPatternController.UpdateTriggerTime());

            if (map.playAllPatternIn > 0f)
                timer.ScheduleLoopingFromNow("PATTERN::TRIGGER", CurrentMap.endlessMode, map.playAllPatternIn, () => _enemyPatternController.TriggerAllPatterns(), triggerWhenSkip:false);

            if (map.addPatternInterval > 0f)
                timer.ScheduleLoopingFromNow("PATTERN::ADD", CurrentMap.endlessMode, map.addPatternInterval, () => _enemyPatternController.AddRandomPatterns(map.amountToAdd));

            // ---- SPAWNER ----
            if (map.decreaseInterval > 0f)
                timer.ScheduleLoopingFromNow("SPAWNER::INTERVAL", CurrentMap.endlessMode, map.decreaseInterval, () => UpdateDefaultSpawnInterval(), triggerWhenSkip:false);

            if (map.intervalIncreaseEnemyPoint > 0f)
                timer.ScheduleLoopingFromNow("SPAWNER::POINT", CurrentMap.endlessMode, map.intervalIncreaseEnemyPoint, () => UpgradeMaxSpawnPoint(_increaseRateEnemyPoint));

            if (map.intervalEnemyChanceUpgrade > 0f)
                timer.ScheduleLoopingFromNow("SPAWNER::CHANCE", CurrentMap.endlessMode, map.intervalEnemyChanceUpgrade, () => _enemySpawnerController.UpgradeEnemyChance());

            if (map.intervalEnemyPointRatioUpgrade > 0f)
                timer.ScheduleLoopingFromNow("SPAWNER::RATIO", CurrentMap.endlessMode, map.intervalEnemyPointRatioUpgrade, () => _enemySpawnerController.UpgradePointRatio());
            
            _mapEventController?.ReloadAllEvents();
        }

        private void UpdateDefaultSpawnInterval()
        {
            _defaultEnemySpawnTimer = Mathf.Clamp(
                _defaultEnemySpawnTimer - CurrentMap.decreaseAmount,
                CurrentMap.decreaseMinimum,
                CurrentMap.defaultEnemySpawnTimer
            );
        }
        
        public void ClearEnemy()
        {
            _enemySpawnerController?.ReleaseAllEnemies();
            _enemySpawnerController?.ClearAllEnemysCompletely();
            
            Transform parentTransform = enemyParent;
            for (int i = parentTransform.childCount - 1; i >= 0; i--)
            {
                GameObject child = parentTransform.GetChild(i).gameObject;
                Destroy(child); 
            }
        }

        public void ClearPatternAsync()
        {
            _enemyPatternController?.StopProcessing();
        }
        
        public void ClearItem()
        {
            _itemSpawnerController?.ClearAllItemsCompletely();
            _itemSpawnerController?.ReleaseAllItem();
        }
        
        public bool EnemyCanSpawn()
        {
            if (CurrentEnemyPoint <= 0) return false;
            return true;
        }

        public void UpgradeMaxSpawnPoint(float increasePoint)
        {
            _currentEnemyPoint += increasePoint;
            Debug.Log("Point: " + _maxEnemyPoint);
        }
        
        [FoldoutGroup("Spawner Control")]
        [Button("Start Spawning" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void DebugStart()
        {
            SetState(_spawningState);
        }

        [FoldoutGroup("Spawner Control")]
        [Button("Pause Spawning" , ButtonSizes.Large), GUIColor(1, 1, 0)]
        private void DebugPause()
        {
            SetState(_pauseState);
        }

        [FoldoutGroup("Spawner Control")]
        [Button("Stop Spawnig" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugStop()
        {
            SetState(_stopState);
        }
        
        [FoldoutGroup("Spawner Control")]
        [Button("Trigger Pattern" , ButtonSizes.Large), GUIColor(1, 1, 0)]
        private void TriggerPattern()
        {
            _enemyPatternController.TriggerAllPatterns();
        }
        
        [FoldoutGroup("Spawner Control")]
        [Button("Add Pattern" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void TriggerAddPattern(int amount)
        {
            _enemyPatternController.AddRandomPatterns(amount);
        }
        
        [FoldoutGroup("Spawner Control")]
        [Button("Clear all Enemy" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugClearEnemy()
        {
            ClearEnemy();
        }
        
        [FoldoutGroup("Spawner Control")]
        [Button("Clear all Item" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugClearItem()
        {
            ClearItem();
        }
        
        [FoldoutGroup("Spawner Control")]
        [Button("Add Enemy Point" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void TriggerAddPoint()
        {
            UpgradeMaxSpawnPoint(20f);
        }
        
        [FoldoutGroup("Spawner Control")]
        [Button("Random Map Event" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void TriggerMapEvent()
        {
            _mapEventController.PlayRandomCategory();
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, regionSize);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, itemdropRegionSize);
        }
    }
}

