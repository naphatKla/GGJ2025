using System;
using System.Collections.Generic;
using System.Linq;
using Challenge.Challenge;
using Sirenix.OdinInspector;
using UI.MapSelection;
using UnityEngine;

namespace Challenge
{
    public class ChallengeManager : MonoBehaviour
    {
        [Header("Catalog (via Container)")]
        public ChallengeContainer allChallenges;

        [Header("Current Selected")]
        public List<ChallengeDataSO> selected = new();

        private readonly Dictionary<PlayerAdditiveStat, float> _aggPlayerMods = new(); //Additive
        private readonly Dictionary<PlayerSetStat, float> _aggPlayerSets = new(); //Set
        
        private readonly Dictionary<string, Dictionary<EnemyStat, float>> _aggEnemyMods = new();
        
        private float _overallScoreMultiplier = 1f;
        private float _flatBonus = 0f;
        private float _playerPercent = 0f;
        private float _enemiesPercent = 0f;


        private const string GLOBAL_ID = "*";

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug Inspector")]
        private float OverallMultiplier => _overallScoreMultiplier;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug Inspector")]
        private float TotalPercent => _flatBonus + _playerPercent + _enemiesPercent;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug Inspector")]
        private float FlatBonusPercent => _flatBonus;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug Inspector")]
        private float PlayerPercent => _playerPercent;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug Inspector")]
        private float EnemiesPercent => _enemiesPercent;
        
        [ShowInInspector, ReadOnly, FoldoutGroup("Debug Inspector/SnapShot")]
        public ChallengeSnapshot Snapshot { get; private set; }
        public event Action<ChallengeSnapshot> OnSnapshotChanged;
        
        private void OnEnable()
        {
            if (MapSelectionSender.Instance != null)
                OnSnapshotChanged += MapSelectionSender.Instance.UpdateChallengeData;
        }

        private void OnDisable()
        {
            if (MapSelectionSender.Instance != null)
                OnSnapshotChanged -= MapSelectionSender.Instance.UpdateChallengeData;
        }

        
        private void Start()
        {
            RecalculateAll();
            Snapshot = CreateSnapshot();
            OnSnapshotChanged?.Invoke(Snapshot);
        } 

        #region Propertie
        public IReadOnlyList<ChallengeDataSO> GetCatalog()
            => allChallenges != null && allChallenges.challengeList != null
                ? allChallenges.challengeList
                : Array.Empty<ChallengeDataSO>();

        public ChallengeDataSO FindById(string id)
            => GetCatalog().FirstOrDefault(x => x != null && x.id == id);
        #endregion

        #region Public API
        
        [Button("Add Challenge"), GUIColor(0, 1, 0)]
        public void SelectChallenge(ChallengeDataSO challenge)
        {
            if (challenge == null || selected.Contains(challenge)) return;
            selected.Add(challenge);
            RecalculateAll();
            Snapshot = CreateSnapshot();
            OnSnapshotChanged?.Invoke(Snapshot);
        }

        [Button("Remove Challenge"), GUIColor(1, 0, 0)]
        public void DeselectChallenge(ChallengeDataSO challenge)
        {
            if (challenge == null) return;
            if (selected.Remove(challenge)) 
                RecalculateAll();
            Snapshot = CreateSnapshot();
            OnSnapshotChanged?.Invoke(Snapshot);
        }

        public void ToggleChallenge(ChallengeDataSO def)
        {
            if (def == null) return;
            if (selected.Contains(def)) selected.Remove(def);
            else selected.Add(def);
            RecalculateAll();
            Snapshot = CreateSnapshot();
            OnSnapshotChanged?.Invoke(Snapshot);
        }

        public float GetOverallScoreMultiplier() => _overallScoreMultiplier;

        public float GetPlayerStatPercent(PlayerAdditiveStat additiveStat)
            => _aggPlayerMods.TryGetValue(additiveStat, out var v) ? v : 0f;

        /// <summary>รวมค่าจาก GLOBAL("*") + enemyId เฉพาะ</summary>
        public float GetEnemyStatPercent(string enemyId, EnemyStat stat)
        {
            float sum = 0f;
            if (_aggEnemyMods.TryGetValue(GLOBAL_ID, out var g) && g.TryGetValue(stat, out var gv)) sum += gv;
            if (!string.IsNullOrEmpty(enemyId) &&
                _aggEnemyMods.TryGetValue(enemyId, out var m) &&
                m.TryGetValue(stat, out var v)) sum += v;
            return sum;
        }

        [Button(ButtonSizes.Medium), FoldoutGroup("Debug Inspector")]
        public void RecalculateAll()
        {
            _aggPlayerMods.Clear();
            _aggEnemyMods.Clear();
            _aggPlayerSets.Clear();

            _flatBonus = 0f;
            _playerPercent = 0f;
            _enemiesPercent = 0f;

            foreach (var ch in selected)
            {
                if (ch == null) continue;

                // Flat
                _flatBonus += ch.flatScoreBonusPercent;

                // Player
                if (ch.statMode == StatMode.Set)
                {
                    foreach (var sm in ch.playerSetMods)
                    {
                        _aggPlayerSets[sm.stat] = _aggPlayerSets.TryGetValue(sm.stat, out var cur) ? Mathf.Min(cur, sm.setDelta) : sm.setDelta;
                    }
                }
                else // StatMode.Additive
                {
                    foreach (var pm in ch.playerMods)
                        _aggPlayerMods[pm.stat] = _aggPlayerMods.TryGetValue(pm.stat, out var cur) ? cur + pm.percentDelta : pm.percentDelta;
                    var perSO = ChallengeScoringUtility.Compute(ch);
                    _playerPercent  += perSO.playerPercent;
                    _enemiesPercent += perSO.enemiesPercentSum;
                }

                // Enemy (grouped)
                foreach (var grp in ch.enemyGroups)
                {
                    if (grp == null || grp.stats.IsZero) continue;

                    var dict = new Dictionary<EnemyStat, float>();

                    void add(EnemyStat s, float val)
                    {
                        if (!Mathf.Approximately(val, 0)) dict[s] = dict.TryGetValue(s, out var x) ? x + val : val;
                    }

                    add(EnemyStat.MaxHP, grp.stats.maxHP);
                    add(EnemyStat.Damage, grp.stats.damage);
                    add(EnemyStat.MoveSpeed, grp.stats.moveSpeed);

                    if (grp.enemyIds == null || grp.enemyIds.Count == 0)
                        AccumulateEnemyDict(GLOBAL_ID, dict);
                    else
                        foreach (var id in grp.enemyIds)
                        {
                            //Global + Per ID
                            var key = string.IsNullOrWhiteSpace(id) ? GLOBAL_ID : id.Trim();
                            AccumulateEnemyDict(key, dict);
                        }
                }
            }

            var totalPercent = _flatBonus + _playerPercent + _enemiesPercent;
            _overallScoreMultiplier = 1f + (totalPercent / 100f);
        }

        public ChallengeSnapshot CreateSnapshot()
        {
            // ----- Player -----
            var addDict = new Dictionary<PlayerAdditiveStat, float>(_aggPlayerMods);
            var setDict = new Dictionary<PlayerSetStat, float>(_aggPlayerSets);
            var playerSnap = new PlayerSnapshot(addDict, setDict);

            // ----- Enemy (รวม Global + แต่ละ ID) -----
            var result = new Dictionary<string, EnemySnapshot>();
            
            //Global
            var global = _aggEnemyMods.TryGetValue(GLOBAL_ID, out var g) ? g : null;
            
            //Enemy Key
            foreach (var kv in _aggEnemyMods)
            {
                var enemyId = kv.Key;
                if (enemyId == GLOBAL_ID) continue;

                var merged = new Dictionary<EnemyStat, float>();
                
                //Add Global
                if (global != null)
                    foreach (var (stat, val) in global)
                        if (!Mathf.Approximately(val, 0))
                            merged[stat] = val;

                //Add Specific
                foreach (var (stat, val) in kv.Value)
                {
                    if (Mathf.Approximately(val, 0)) continue;
                    merged[stat] = merged.TryGetValue(stat, out var cur) ? cur + val : val;
                }

                result[enemyId] = new EnemySnapshot(enemyId, merged);
            }

            return new ChallengeSnapshot(
                playerSnap, result,
                _overallScoreMultiplier,
                _flatBonus,
                _playerPercent,
                _enemiesPercent
            );
        }

        #endregion

        private void AccumulateEnemyDict(string enemyId, Dictionary<EnemyStat, float> add)
        {
            if (!_aggEnemyMods.TryGetValue(enemyId, out var dict))
            {
                dict = new Dictionary<EnemyStat, float>();
                _aggEnemyMods[enemyId] = dict;
            }
            foreach (var (k, v) in add)
                dict[k] = dict.TryGetValue(k, out var cur) ? cur + v : v;
        }
    }
}
