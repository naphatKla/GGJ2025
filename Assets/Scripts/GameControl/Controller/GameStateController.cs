using System;
using System.Collections.Generic;
using System.Threading;
using Challenge;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameControl.GameState;
using GameControl.Interface;
using GameControl.SO;
using MoreMountains.Tools;
using PermanentUpgrade;
using Player;
using UnityEngine;
using Sirenix.OdinInspector;
using UI;
using UI.MapSelection;
using UnityEngine.SceneManagement;

namespace GameControl.Controller
{
    public enum EndResult
    {
        None,
        Completed,
        Failed,
    }
    
    public enum MapState
    {
        None,
        Normal,
        Rush,
        Endless
    }
    
    [RequireComponent(typeof(SpawnerStateController), typeof(GameTimer))]
    public class GameStateController : MMSingleton<GameStateController>
    {
        private IGameState _currentState;
        
        private TutorialState _tutorialState;
        private PrestartState _prestartState;
        private StartState _startState;
        private EndState _endState;
        private SummaryState _summaryState;
        private MapState _mapstate;
        public CancellationTokenSource sceneCts;
        
        private GameObject _mapEnvironment;

        [BoxGroup("Debug")]
        [ShowInInspector, ReadOnly]
        private string _currentStateName;
        
        [BoxGroup("Debug")]
        [ShowInInspector, ReadOnly]
        private MapDataSO _currentMapDataRuntime;
        
        [BoxGroup("Debug")]
        [SerializeField] private MapSelectionDataContainer mapContainer;
        [BoxGroup("Debug")]
        [Tooltip("The index of the current map in the mapData list.")]
        [SerializeField] private int currentMapIndex;
        [BoxGroup("Debug")] public EndResult gameResult;
        private List<MapDataSO> MapDataList =>
            mapContainer != null && mapContainer.mapSelectionList != null
                ? mapContainer.mapSelectionList
                : new List<MapDataSO>();
        
        public MapDataSO CurrentMap => _currentMapDataRuntime;
        public IGameState CurrentState => _currentState;
        public MapState MapState { get => _mapstate; set => _mapstate = value; }
        private MapSelectionSender Sender => MapSelectionSender.Instance;
        
        protected override void Awake()
        {
            base.Awake();
            sceneCts = new CancellationTokenSource();
            
            _tutorialState = new TutorialState();
            _prestartState = new PrestartState();
            _startState = new StartState();
            _endState = new EndState();
            _summaryState = new SummaryState();
            
            _currentMapDataRuntime = null;
        }
        
        private void OnEnable()
        {
            _currentState?.OnEnable(this);
        }
        
        private void OnDisable()
        {
            _currentState?.OnDisable(this);
            try { sceneCts?.Cancel(); } catch { }
        }
        
        private void OnDestroy()
        {
            try { sceneCts?.Cancel(); } catch { }
            sceneCts?.Dispose();
            sceneCts = null;
            
            if (_currentMapDataRuntime != null) Destroy(_currentMapDataRuntime);
        }

        private void Start()
        {
            SetupGameSelection().Forget();
        }

        private void Update()
        {
            _currentState?.Update(this);
        }
        
        private MapDataSO MakeRuntimeCopy(MapDataSO src)
        {
            if (src == null) return null;
            var copy = Instantiate(src);
            copy.name = src.name + " (Runtime)";
            copy.hideFlags = HideFlags.DontSave;
            return copy;
        }
        
        private RushDataSO MakeRushStateCopy(RushDataSO src)
        {
            if (src == null) return null;
            var copy = Instantiate(src);
            copy.name = src.name + " (Runtime)";
            copy.hideFlags = HideFlags.DontSave;
            return copy;
        }

        private void AssignMapRuntime(MapDataSO asset)
        {
            if (_currentMapDataRuntime != null) Destroy(_currentMapDataRuntime);
            //Normal
            var copyMapData = MakeRuntimeCopy(asset);
            ModifyAllDataBeforeStart(copyMapData);
            
            //Rush
            var copyRushData = MakeRushStateCopy(copyMapData.rushData);
            ModifyAllRushDataFromChallenge(copyRushData);
            copyMapData.rushData = copyRushData;
            
            _currentMapDataRuntime = copyMapData;
            if (_currentMapDataRuntime.endlessMode)
            {
                MapState = MapState.Endless;
            }
            else
            {
                MapState = MapState.Normal;
            }
            
            //Map Environment
            _mapEnvironment = Instantiate(copyMapData.mapEnvironment);
        }

        private void ModifyAllDataBeforeStart(MapDataSO mapData)
        {
            if (mapData == null || mapData.EnemyOptions == null || Sender == null) return;
            var snap = Sender.challengeData;
            
            //Stage Modify
            mapData.mapGlobalTime += snap.TimeModify;
            
            //Player Modify
            var playerDataSo = mapData.playerData.CopyInstance(); // Create Player DataSO
            var player = PlayerController.Instance;
            var profileData = ActiveProfileService.Instance?.CurrentProfile;
            var permanentConfig = ActiveProfileService.Instance?.PermanentConfig;
            //Modify Permanent Upgrade
            playerDataSo.AddPlayerStats(PlayerDataStats.MaxHealth
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.MaxHealth, permanentConfig, playerDataSo.MaxHealth));
            playerDataSo.AddPlayerStats(PlayerDataStats.Speed
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.Speed, permanentConfig, playerDataSo.BaseSpeed));
            playerDataSo.AddPlayerStats(PlayerDataStats.Damage
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.Damage, permanentConfig, playerDataSo.BaseDamage));
            
            playerDataSo.AddPlayerStats(PlayerDataStats.CritDamage
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.CritDamage, permanentConfig, playerDataSo.BaseCriDamage));
            playerDataSo.AddPlayerStats(PlayerDataStats.CritRate
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.CritRate, permanentConfig, playerDataSo.BaseCriRate));
            
            playerDataSo.AddPlayerStats(PlayerDataStats.LifeStealChance
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.LifeStealChance, permanentConfig, playerDataSo.BaseLifeStealPercent));
            playerDataSo.AddPlayerStats(PlayerDataStats.LifeStealEffective
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.LifeStealEffective, permanentConfig, playerDataSo.BaseLifeStealEffective));

            player.LevelSystem.AddExpMultiplyer(profileData.ApplyPermanentUpgrade(PermanentUpgradeType.ExpMultiply, permanentConfig, player.LevelSystem.ExpMultiplyer));
            
            playerDataSo.AddPlayerStats(PlayerDataStats.PickupRadius
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.PickupRadius, permanentConfig, playerDataSo.PullItemRadius));
            playerDataSo.AddPlayerStats(PlayerDataStats.HurtIFrame
                , profileData.ApplyPermanentUpgrade(PermanentUpgradeType.HurtIFrame, permanentConfig, playerDataSo.InvincibleTimePerHit));
            
            //Modify Challenge
            
            //Exp
            float expMultiplyer = 1f + snap.Player.GetAdd(PlayerAdditiveStat.ExpGain) / 100f;
            player.LevelSystem.AddExpMultiplyer(expMultiplyer - 1);
            //Stats
            
            //Set
            playerDataSo.SetPlayerStats(PlayerDataStats.Damage, snap.Player.GetSet(PlayerSetStat.BaseDamage, playerDataSo.BaseDamage));
            playerDataSo.SetPlayerStats(PlayerDataStats.Speed, snap.Player.GetSet(PlayerSetStat.MoveSpeed, playerDataSo.BaseSpeed));
            playerDataSo.SetPlayerStats(PlayerDataStats.MaxHealth, snap.Player.GetSet(PlayerSetStat.MaxHP, playerDataSo.MaxHealth));

            //Additive
            playerDataSo.MutiplyPlayerStats(PlayerDataStats.Damage, 1f + snap.Player.GetAdd(PlayerAdditiveStat.BaseDamage) / 100f);
            playerDataSo.MutiplyPlayerStats(PlayerDataStats.Speed, 1f + snap.Player.GetAdd(PlayerAdditiveStat.MoveSpeed) / 100f);
            playerDataSo.MutiplyPlayerStats(PlayerDataStats.MaxHealth, 1f + snap.Player.GetAdd(PlayerAdditiveStat.MaxHP) / 100f);
            
            player.AssignCharacterData(playerDataSo); //Assign Player DataSO
            
            //Score Modify
            PlayerController.Instance.ScoreSystem.SetScoreMultiplyer(Mathf.Clamp(snap.OverallScoreMultiplier, 1f, snap.OverallScoreMultiplier));
            
            //Enemy Modify
            foreach (var opt in mapData.EnemyOptions)
            {
                var e = snap.GetEnemy((opt.id ?? "").Trim()); 
                ApplyEnemyOption(opt, e);
            }
        }
        
        private void ModifyAllRushDataFromChallenge(RushDataSO rushData)
        {
            if (rushData == null || rushData.enemyOptions == null || Sender == null) return;
            var snap = Sender.challengeData;
            
            //Enemy Modify
            foreach (var opt in rushData.enemyOptions)
            {
                var e = snap.GetEnemy((opt.id ?? "").Trim()); 
                ApplyRushEnemyOption(opt, e);
            }
        }
        
        private static void ApplyEnemyOption(MapDataSO.EnemyOption opt, in Challenge.Challenge.EnemySnapshot eSnap)
        {
            float hp      = eSnap.Get(EnemyStat.MaxHP);
            float dmg     = eSnap.Get(EnemyStat.Damage);
            float mspd    = eSnap.Get(EnemyStat.MoveSpeed);
            float chanceP = eSnap.Get(EnemyStat.SpawnChance);
            if (float.IsFinite(chanceP))
            {
                var newGrowthChance = opt.enemyChanceGrowthRate * (1f + (chanceP / 100f));
                opt.enemyChanceGrowthRate  = Mathf.Max(0f, newGrowthChance);
            }

            if (opt.enemyData != null)
            {
                var modified = opt.enemyData.CopyInstance(hp, dmg, mspd);
                opt.enemyData = modified;
                opt.modifyNewData = true;
            }
        }
        
        private static void ApplyRushEnemyOption(MapDataSO.EnemyOption opt, in Challenge.Challenge.EnemySnapshot eSnap)
        {
            float hp      = eSnap.Get(EnemyStat.RushMaxHP);
            float dmg     = eSnap.Get(EnemyStat.RushDamage);
            float mspd    = eSnap.Get(EnemyStat.RushMoveSpeed);
            float chanceP = eSnap.Get(EnemyStat.RushSetChance);
            if (float.IsFinite(chanceP))
            {
                opt.chance = chanceP;
            }

            if (opt.enemyData != null)
            {
                var modified = opt.enemyData.CopyInstance(hp, dmg, mspd);
                opt.enemyData = modified;
                opt.modifyNewData = true;
            }
        }

        
        private void EnterRush()
        {
            var m = CurrentMap;
            if (m?.rushData == null) return;
            MapState = MapState.Rush;
            m.rushData.ApplyInto(m); 
            SpawnerStateController.Instance?.OnMapModified();
        }
        
        public void ScheduleRush()
        {
            var m = CurrentMap;
            if (m?.rushData == null) return;
            GameTimer.Instance.ScheduleOnceAtRemaining(m.rushTime, EnterRush);
        }

        public void SetState(IGameState newState)
        {
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState?.Enter(this);
            _currentStateName = _currentState?.GetType().Name;
        }

        private async UniTask SetupGameSelection()
        {
            await UniTask.Yield(PlayerLoopTiming.Initialization);
            EnsureSenderContainerSync();
            var list = GetActiveList();
            if (list == null || list.Count == 0)
            {
                _currentMapDataRuntime = null;
                return;
            }

            var initIdx = Sender != null ? Sender.currentMapSelectionIndex : currentMapIndex;
            initIdx = NormalizeIndex(initIdx, list.Count, true, false);

            if (_currentMapDataRuntime != null && initIdx == currentMapIndex)
            {
                SetState(_tutorialState);
                return;
            }

            currentMapIndex = initIdx;
            ApplyInSceneIndex(currentMapIndex);
        }


        public void RestartMap()
        {
            try { sceneCts?.Cancel(); } catch { }
            SpawnerStateController.Instance.ClearPatternAsync();
            DOTween.KillAll();
            UIManager.Instance.LoadToGamePlayScene();
            //SetState(_prestartState);
        }
        
        [FoldoutGroup("Game State")]
        [Button("Restart" , ButtonSizes.Large), GUIColor(1, 1, 1)]
        private void DebugRestart()
        {
            RestartMap();
        }
        
        [FoldoutGroup("Game State")]
        [Button("PreStart" , ButtonSizes.Large), GUIColor(1, 1, 0)]
        private void DebugPreStart()
        {
            SetState(_prestartState);
        }

        [FoldoutGroup("Game State")]
        [Button("Start Game" , ButtonSizes.Large), GUIColor(0, 1, 0)]
        private void DebugStartGame()
        {
            SetState(_startState);
        }

        [FoldoutGroup("Game State")]
        [Button("End Game" , ButtonSizes.Large), GUIColor(1, 0, 0)]
        private void DebugEndGame()
        {
            gameResult = EndResult.Completed;
            SetState(_endState);
        }

        [FoldoutGroup("Game State")]
        [Button("Summary" , ButtonSizes.Large), GUIColor(0, 1, 1)]
        private void DebugSummary()
        {
            SetState(_summaryState);
        }

        [FoldoutGroup("Map Button")] [Button(ButtonSizes.Large)] [GUIColor(0, 1, 1)]
        public void SetMap(int mapIndex, bool clamp = true)
        {
            SetIndexAbsolute(mapIndex, clamp);
        }
        
        [FoldoutGroup("Map Button"), Button(ButtonSizes.Large), GUIColor(0,1,1)]
        public void NextMap(bool wrap = false)
        {
            ChangeIndexRelative(+1, wrap);
        }
        
        
        [FoldoutGroup("Map Button"), Button(ButtonSizes.Large), GUIColor(0,1,1)]
        public void PrevMap(bool wrap = false)
        {
            ChangeIndexRelative(-1, wrap);
        }
        
        [FoldoutGroup("Map Button"), Button(ButtonSizes.Large), GUIColor(0, 1, 1)]
        public void EnterRushState()
        {
            EnterRush();
        }
        
        #region Internal

        private List<MapDataSO> GetActiveList()
        {
            var s = Sender;
            var c = s != null ? s.currentmapSelectionDataContainer : mapContainer;
            var list = c != null ? c.mapSelectionList : null;
            return list != null ? list : MapDataList;
        }

        private void EnsureSenderContainerSync()
        {
            var s = Sender;
            if (s == null) return;

            if (s.currentmapSelectionDataContainer == null)
                s.currentmapSelectionDataContainer = mapContainer;
            else
                mapContainer = s.currentmapSelectionDataContainer;
        }

        private int NormalizeIndex(int idx, int count, bool clamp, bool wrap)
        {
            if (count <= 0) return -1;
            if (wrap)
            {
                idx = (idx % count + count) % count;
                return idx;
            }

            if (clamp) return Mathf.Clamp(idx, 0, count - 1);
            return idx < 0 || idx >= count ? -1 : idx;
        }

        private void ApplyInSceneIndex(int idx)
        {
            var list = GetActiveList();
            if (list == null || list.Count == 0) return;
            if (idx < 0 || idx >= list.Count) return;

            currentMapIndex = idx;
            AssignMapRuntime(list[idx]);
            SetState(_tutorialState);
        }

        private void ApplyWithSenderRestart(int idx)
        {
            var s = Sender;
            if (s == null)
            {
                ApplyInSceneIndex(idx);
                return;
            }

            EnsureSenderContainerSync();

            var list = s.currentmapSelectionDataContainer?.mapSelectionList;
            if (list == null || list.Count == 0) return;
            idx = NormalizeIndex(idx, list.Count, true, false);
            s.currentMapSelectionIndex = idx;
            RestartMap();
        }

        private void SetIndexAbsolute(int targetIndex, bool clamp)
        {
            var list = GetActiveList();
            if (list == null || list.Count == 0) return;

            var idx = NormalizeIndex(targetIndex, list.Count, clamp, false);
            if (idx < 0) return;

            var s = Sender;
            if (s == null)
                ApplyInSceneIndex(idx);
            else
                ApplyWithSenderRestart(idx);
        }

        private void ChangeIndexRelative(int delta, bool wrap)
        {
            var list = GetActiveList();
            if (list == null || list.Count == 0) return;

            var s = Sender;
            if (s == null)
            {
                var idx = NormalizeIndex(currentMapIndex + delta, list.Count, !wrap, wrap);
                if (idx < 0) return;
                ApplyInSceneIndex(idx);
                return;
            }

            EnsureSenderContainerSync();
            var idxFromCurrent = NormalizeIndex(currentMapIndex + delta, list.Count, !wrap, wrap);
            if (idxFromCurrent < 0) return;
            ApplyWithSenderRestart(idxFromCurrent);
        }
        #endregion
    }
}