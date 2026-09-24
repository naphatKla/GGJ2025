using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Characters.Controllers;
using Characters.SO.SkillDataSo;
using Cysharp.Threading.Tasks;
using GameControl.Controller;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillEnemySummonRuntime : BaseSkillRuntime<SkillEnemySummonDataSo>
    {
        private readonly List<EnemyController> _aliveSummons = new();
        private readonly Dictionary<SkillEnemySummonDataSo.SummonOption, int> _plannedCounts = new();

        protected override void OnSkillStart()
        {
            CleanupSummonList();
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            if (skillData.DelayBeforeFirstSummon > 0f)
                await UniTask.WaitForSeconds(skillData.DelayBeforeFirstSummon, cancellationToken: cancelToken);

            if (cancelToken.IsCancellationRequested) return;

            var summonPlan = BuildSummonPlan();
            for (int i = 0; i < summonPlan.Count; i++)
            {
                if (cancelToken.IsCancellationRequested) return;

                SummonWithFallback(summonPlan[i]);

                if (skillData.DelayBetweenSummons > 0f && i < summonPlan.Count - 1)
                    await UniTask.WaitForSeconds(skillData.DelayBetweenSummons, cancellationToken: cancelToken);
            }
        }

        protected override void OnSkillExit()
        {
            CleanupSummonList();
        }

        private List<SkillEnemySummonDataSo.SummonOption> BuildSummonPlan()
        {
            CleanupSummonList();

            int availableAliveSlots = skillData.MaxAliveSummons <= 0
                ? int.MaxValue
                : Mathf.Max(0, skillData.MaxAliveSummons - _aliveSummons.Count);

            if (availableAliveSlots <= 0) return new List<SkillEnemySummonDataSo.SummonOption>();

            int minCount = Mathf.Max(0, skillData.MinTotalSummonCount);
            int maxCount = Mathf.Max(minCount, skillData.MaxTotalSummonCount);
            int targetCount = Random.Range(minCount, maxCount + 1);
            targetCount = Mathf.Min(targetCount, availableAliveSlots);

            var options = skillData.SummonOptions?
                .Where(IsValidOption)
                .ToList() ?? new List<SkillEnemySummonDataSo.SummonOption>();

            _plannedCounts.Clear();
            foreach (var option in options)
                _plannedCounts[option] = 0;

            var plan = new List<SkillEnemySummonDataSo.SummonOption>(targetCount);

            foreach (var option in options)
            {
                int count = Mathf.Min(option.minAmount, option.maxAmount, targetCount - plan.Count);
                for (int i = 0; i < count; i++)
                    AddToPlan(plan, option);

                if (plan.Count >= targetCount)
                    return plan;
            }

            while (plan.Count < targetCount)
            {
                var option = PickWeightedOption(options);
                if (option == null) break;
                AddToPlan(plan, option);
            }

            return plan;
        }

        private bool IsValidOption(SkillEnemySummonDataSo.SummonOption option)
        {
            if (option == null
                || !option.enabled
                || (string.IsNullOrWhiteSpace(option.id) && option.enemyPrefab == null)
                || option.maxAmount <= 0)
                return false;

            if (!skillData.UseSpawnerPool || skillData.AllowInstantiateFallback || string.IsNullOrWhiteSpace(option.id))
                return true;

            var spawner = SpawnerStateController.Instance?.EnemySpawnerController;
            return spawner != null && spawner.CanSpawnEnemyFromPool(
                option.id,
                skillData.CountTowardMapPerEnemyMax,
                skillData.RequirePrewarmedPoolInstance);
        }

        private void AddToPlan(List<SkillEnemySummonDataSo.SummonOption> plan, SkillEnemySummonDataSo.SummonOption option)
        {
            plan.Add(option);
            _plannedCounts[option]++;
        }

        private SkillEnemySummonDataSo.SummonOption PickWeightedOption(List<SkillEnemySummonDataSo.SummonOption> options)
        {
            float totalWeight = 0f;
            foreach (var option in options)
            {
                if (_plannedCounts.TryGetValue(option, out int count) && count >= option.maxAmount)
                    continue;

                if (option.weight <= 0f) continue;
                totalWeight += option.weight;
            }

            if (totalWeight <= 0f) return null;

            float roll = Random.Range(0f, totalWeight);
            float accum = 0f;

            foreach (var option in options)
            {
                if (_plannedCounts.TryGetValue(option, out int count) && count >= option.maxAmount)
                    continue;

                if (option.weight <= 0f) continue;
                accum += option.weight;
                if (roll <= accum)
                    return option;
            }

            return null;
        }

        private bool SummonWithFallback(SkillEnemySummonDataSo.SummonOption preferredOption)
        {
            if (Summon(preferredOption)) return true;

            var fallbackOptions = skillData.SummonOptions?
                .Where(option => option != preferredOption && IsValidOption(option))
                .OrderByDescending(option => option.weight)
                .ToList();

            if (fallbackOptions == null) return false;

            foreach (var option in fallbackOptions)
            {
                if (Summon(option)) return true;
            }

            return false;
        }

        private bool Summon(SkillEnemySummonDataSo.SummonOption option)
        {
            if (option == null) return false;
            Vector2 spawnPosition = GetSpawnPosition();

            if (TrySummonFromSpawnerPool(option, spawnPosition, out var pooledSummon))
            {
                TrackSummon(pooledSummon, destroyOnDeath: false);
                FacePlayer(pooledSummon);
                return true;
            }

            if (!skillData.AllowInstantiateFallback || option.enemyPrefab == null) return false;

            Transform parent = SpawnerStateController.Instance != null
                ? SpawnerStateController.Instance.EnemyParent
                : null;

            var summon = Instantiate(option.enemyPrefab, spawnPosition, Quaternion.identity, parent);
            if (option.overrideCharacterData && option.characterDataOverride != null)
                summon.AssignCharacterData(option.characterDataOverride);
            else
                summon.AssignCharacterData(summon.CharacterData);

            FacePlayer(summon);

            summon.ResetAllDependentBehavior();
            summon.gameObject.SetActive(true);
            TrackSummon(summon, destroyOnDeath: true);
            return true;
        }

        private bool TrySummonFromSpawnerPool(SkillEnemySummonDataSo.SummonOption option, Vector2 spawnPosition,
            out EnemyController summon)
        {
            summon = null;
            if (!skillData.UseSpawnerPool) return false;
            if (string.IsNullOrWhiteSpace(option.id)) return false;

            var spawner = SpawnerStateController.Instance?.EnemySpawnerController;
            if (spawner == null) return false;

            return spawner.TrySpawnEnemyFromPool(
                option.id,
                spawnPosition,
                out summon,
                skillData.CountTowardMapPerEnemyMax,
                skillData.ShowTrailOnSpawn,
                skillData.RequirePrewarmedPoolInstance);
        }

        private void TrackSummon(EnemyController summon, bool destroyOnDeath)
        {
            if (!summon) return;
            _aliveSummons.Add(summon);

            if (destroyOnDeath)
                summon.HealthSystem.OnDeadAnimationFinish += () => DestroySummon(summon);
        }

        private void FacePlayer(EnemyController summon)
        {
            if (!skillData.FacePlayerOnSpawn || !PlayerController.Instance || !summon || !summon.Body) return;

            Vector2 direction = PlayerController.Instance.transform.position - summon.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                summon.Body.transform.up = direction.normalized;
        }

        private Vector2 GetSpawnPosition()
        {
            Vector2 origin = GetSpawnOrigin();
            Vector2 chosen = origin;
            int attempts = Mathf.Max(1, skillData.PositionAttempts);

            for (int i = 0; i < attempts; i++)
            {
                chosen = origin + GetRandomOffset();
                if (!skillData.AvoidOverlap || !Physics2D.OverlapCircle(chosen, skillData.OverlapRadius, skillData.OverlapLayer))
                    return chosen;
            }

            return chosen;
        }

        private Vector2 GetSpawnOrigin()
        {
            var player = PlayerController.Instance;

            return skillData.Origin switch
            {
                SkillEnemySummonDataSo.SpawnOrigin.Player when player =>
                    player.transform.position,
                SkillEnemySummonDataSo.SpawnOrigin.MidpointOwnerPlayer when player =>
                    ((Vector2)owner.transform.position + (Vector2)player.transform.position) * 0.5f,
                _ => owner.transform.position
            };
        }

        private Vector2 GetRandomOffset()
        {
            float radius = Random.Range(skillData.MinSpawnRadius, skillData.MaxSpawnRadius);

            if (!skillData.UseForwardCone)
            {
                float angle = Random.Range(0f, 360f);
                return (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector2.up) * radius;
            }

            Vector2 forward = owner.Body ? (Vector2)owner.Body.transform.up : (Vector2)owner.transform.up;
            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector2.up;

            float halfAngle = skillData.ConeAngle * 0.5f;
            float coneOffsetAngle = Random.Range(-halfAngle, halfAngle);
            return (Vector2)(Quaternion.Euler(0f, 0f, coneOffsetAngle) * forward.normalized) * radius;
        }

        private void CleanupSummonList()
        {
            _aliveSummons.RemoveAll(s => s == null || !s.gameObject.activeInHierarchy);
        }

        private void DestroySummon(EnemyController summon)
        {
            _aliveSummons.Remove(summon);
            if (summon)
                Destroy(summon.gameObject);
        }
    }
}
