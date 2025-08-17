using Characters.Controllers;
using Cysharp.Threading.Tasks;
using GameControl.Interface;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using TMPro;
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
        
        [BoxGroup("Debug")] 
        [ShowInInspector, ReadOnly]
        private string _currentStateName;
        [BoxGroup("Debug")] 
        [ShowInInspector, ReadOnly]
        private SO.MapDataSO _currentMapData;
        
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
        
        [BoxGroup("Debug Zone")] [SerializeField] [ShowIf("$debugEnemy")]
        private TMP_Text debugChance;
        
        [BoxGroup("Debug Zone")] [SerializeField] private bool debugMapEvent;
        [ShowInInspector, ReadOnly]
        public float EnemyPoint => _currentEnemyPoint;
        public MapEventController MapEventController => _mapEventController;
        public EnemySpawnerController EnemySpawnerController => _enemySpawnerController;
        public EnemyPatternController EnemyPatternController => _enemyPatternController;
        public ItemSpawnerController ItemSpawnerController => _itemSpawnerController;
        public Transform EnemyParent => enemyParent;
        public Transform ItemParent => itemParent;
        public Vector2 RegionSize => regionSize;
        public float EnemySpawnTimer { get => _currentMapData.defaultEnemySpawnTimer; set => _currentMapData.defaultEnemySpawnTimer = value; }
        public float ItemSpawnTimer { get => _currentMapData.defaultItemSpawnTimer; set => _currentMapData.defaultItemSpawnTimer = value; }

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
            _currentMapData = GameStateController.Instance.CurrentMap;
        }
        
        private void Start()
        {
            SetState(_stopState);

            if (debugEnemy) debugChance.gameObject.SetActive(true);
        }

        private void Update()
        {
            _currentState?.Update(this);
        }
        
        private void LateUpdate()
        {
            if (debugEnemy && debugChance != null && _enemySpawnerController != null)
            {
                var options = _enemySpawnerController.GetEnemyOption();
                if (options == null || options.Count == 0) return;

                string debugText = "Enemy Chance\n";
                foreach (var opt in options)
                {
                    debugText += $"{opt.id} : {opt.Chance:F2}%\n";
                }
                debugChance.text = debugText;
            }
        }

        public void SetState(ISpawnerState newState)
        {
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState?.Enter(this);
            _currentStateName = _currentState?.GetType().Name;
        }

        public async UniTaskVoid SetupMapAndEnemy()
        {
            _enemySpawnerController = new EnemySpawnerController(_currentMapData, this, regionSize, debugEnemy, mainCamera);
            _enemyPatternController = new EnemyPatternController(_currentMapData, this, regionSize, debugPattern);
            _itemSpawnerController = new ItemSpawnerController(_currentMapData, this, itemdropRegionSize);
            _mapEventController = new MapEventController(_currentMapData, this, debugMapEvent);
            
            await UniTask.WaitUntil(() => _enemySpawnerController != null && _enemyPatternController != null && _itemSpawnerController != null);
            
            _currentEnemyPoint = _currentMapData.startEnemyPoint;
            _maxEnemyPoint = _currentMapData.maxEnemyPoint;
            _increaseRateEnemyPoint = _currentMapData.rateIncreaseEnemyPoint;
            
            _itemSpawnerController.PrewarmItem();
            _enemyPatternController.SetEnemySpawner(_enemySpawnerController);
            _enemyPatternController.AddRandomPattern();
            
            //Trigger time will decrease if enable
            GameTimer.Instance.ScheduleLoopingTrigger(_currentMapData.patternDecreaseInterval, GameTimer.Instance.StartTimerNumber, 
                () => _enemyPatternController.UpdateTriggerTime());
            
            //Every 3 minute trigger pattern
            GameTimer.Instance.ScheduleLoopingTrigger(
                _currentMapData.playAllPatternIn,
                GameTimer.Instance.StartTimerNumber,
                () =>
                {
                    UniTask.Void(async () =>
                    {
                        _enemyPatternController.AddRandomPattern();
                        await UniTask.Delay(100);
                        _enemyPatternController.TriggerAllPatterns();
                    });
                });
            
            //Upgrade Max Spawn point every 1 minute
            GameTimer.Instance.ScheduleLoopingTrigger(_currentMapData.intervalIncreaseEnemyPoint, GameTimer.Instance.StartTimerNumber, 
                () => UpgradeMaxSpawnPoint(_increaseRateEnemyPoint));
            
            //Upgrade Chance rate every 30 seconds
            GameTimer.Instance.ScheduleLoopingTrigger(_currentMapData.intervalEnemyChanceUpgrade, GameTimer.Instance.StartTimerNumber, 
                () => _enemySpawnerController.UpgradeEnemyChance());
            
            //Upgrade Spawn Ratio every 30 seconds
            GameTimer.Instance.ScheduleLoopingTrigger(_currentMapData.intervalEnemyPointRatioUpgrade, GameTimer.Instance.StartTimerNumber, 
                () => _enemySpawnerController.UpgradePointRatio());
 
            _mapEventController.ScheduleAllTriggersUpfront(GameTimer.Instance.StartTimerNumber);
        }
        
        public void ClearEnemy()
        {
            _enemySpawnerController?.ReleaseAllEnemies();
            _enemySpawnerController?.ClearAllEnemysCompletely();
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
        
        [Button("Start Spawning" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void DebugStart()
        {
            SetState(_spawningState);
        }

        [Button("Pause Spawning" , ButtonSizes.Large), GUIColor(1, 1, 0)]
        private void DebugPause()
        {
            SetState(_pauseState);
        }

        [Button("Stop Spawnig" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugStop()
        {
            SetState(_stopState);
        }
        
        [Button("Trigger Pattern" , ButtonSizes.Large), GUIColor(1, 1, 0)]
        private void TriggerPattern()
        {
            _enemyPatternController.TriggerAllPatterns();
        }
        
        [Button("Add Pattern" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void TriggerAddPattern()
        {
            _enemyPatternController.AddRandomPattern();
        }
        
        [Button("Clear all Enemy" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugClearEnemy()
        {
            ClearEnemy();
        }
        
        [Button("Clear all Item" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugClearItem()
        {
            ClearItem();
        }
        
        [Button("Add Enemy Point" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void TriggerAddPoint()
        {
            UpgradeMaxSpawnPoint(20f);
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

