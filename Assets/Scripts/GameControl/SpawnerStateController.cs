using Cysharp.Threading.Tasks;
using GameControl.Interface;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameControl
{
    public class SpawnerStateController : MMSingleton<SpawnerStateController>
    {
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
        [BoxGroup("Setting")] [SerializeField] private Transform enemyParent;
        [BoxGroup("Setting")] [SerializeField] private Vector2 regionSize = Vector2.zero;
        
        public EnemySpawnerController EnemySpawnerController => _enemySpawnerController;
        public Transform EnemyParent => enemyParent;
        public Vector2 RegionSize => regionSize;
        public float SpawnTimer { get => _currentMapData.defaultEnemySpawnTimer; set => _currentMapData.defaultEnemySpawnTimer = value; }

        public float CurrentEnemyPoint
        {
            get => _currentEnemyPoint;
            set => _currentEnemyPoint = Mathf.Clamp(value, 0, _maxEnemyPoint);
        }
        
        private void Awake()
        {
            _stopState = new SpawnerState.StopState();
            _spawningState = new SpawnerState.SpawningState();
            _pauseState = new SpawnerState.PauseState();
            _currentMapData = GameStateController.Instance.CurrentMap;
        }
        
        
        private void Start()
        {
            SetState(_stopState);
        }

        private void Update()
        {
            _currentState?.Update(this);
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
            _enemySpawnerController = new EnemySpawnerController(_currentMapData, this, regionSize);
            await UniTask.WaitUntil(() => _enemySpawnerController != null);
            _enemySpawnerController.PrewarmEnemy();
            
            _currentEnemyPoint = _currentMapData.startEnemyPoint;
            _maxEnemyPoint = _currentMapData.maxEnemyPoint;
            _increaseRateEnemyPoint = _currentMapData.increaseRateEnemyPoint;
        }
        
        public bool CanSpawn()
        {
            if (CurrentEnemyPoint <= 0) return false;
            return true;
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
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, regionSize);
        }
    }
}

