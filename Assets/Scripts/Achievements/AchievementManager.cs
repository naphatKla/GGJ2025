using System.Linq;
using Manager;
using Player;
using ProjectExtensions;
using UnityEngine;

namespace Achievements
{
    public class AchievementManager : NonAutoCreatePersistentSingleton<AchievementManager>
    {
        [SerializeField] private AchievementDatabase database;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);

            if (!database)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[AchievementManager] Database is not assigned.", this);
#endif
            }
        }

        private PlayerData CurrentPlayerData =>
            ActiveProfileService.Instance?.CurrentProfile ??
            ActiveProfileService.Instance?.LoadCurrent();

        #region AchievementTrigger

        /// <summary>เรียกเมื่อจบ 1 run</summary>
        public void OnRunEnded(string mapId)
        {
            var p = CurrentPlayerData;
            if (p == null || !database) return;

            var ctx = new AchievementContext
            {
                TriggerType = AchievementTriggerType.OnRunEnd,
                MapId = mapId,
                Player = p
            };

            EvaluateAll(ctx);
        }

        public void OnRunWin(string mapId)
        {
            var p = CurrentPlayerData;
            if (p == null || !database) return;

            var ctx = new AchievementContext
            {
                TriggerType = AchievementTriggerType.OnRunWin,
                MapId = mapId,
                Player = p
            };

            EvaluateAll(ctx);
        }

        #endregion

        #region Condition and Reward

        private bool EvaluateCondition(AchievementConditionConfig c, AchievementContext ctx)
        {
            var p = ctx.Player;

            switch (c.type)
            {
                case AchievementConditionType.None:
                    return true;

                case AchievementConditionType.MapIdEquals:
                    if (string.IsNullOrEmpty(c.mapId)) return true;
                    return ctx.MapId == c.mapId;

                case AchievementConditionType.ChallengeIdEquals:
                    return p.SelectedChallenges.Contains(c.challengeId);

                case AchievementConditionType.HighestScoreAtLeast:
                    if (p == null) return false;
                    return p.HighestScore >= c.minHighestScore;

                case AchievementConditionType.TotalDamageAtLeast:
                    return p.TotalDamageDeal >= c.minTotalDamage;

                case AchievementConditionType.TotalKillAtLeast:
                    if (p == null || string.IsNullOrEmpty(c.enemyId))
                        return false;
                    p.totalKillDictionary.TryGetValue(c.enemyId, out int count);
                    return count >= c.minKillAtLeast;

                case AchievementConditionType.ChallengeAtLeast:
                    return p.SelectedChallenges.Count >= c.challengeAtLeast;

                case AchievementConditionType.ParryAtLeast:
                    return p.HighestParryUseOnRun >= c.minParryAmountOnRun;

                case AchievementConditionType.WinAtLeast:
                    return p.MapStats.Sum(w => w.Value.WinAmount) >= c.winAtLeast;

                case AchievementConditionType.TakeHitLessThan:
                    p.takeDamageOnRunDictionary.TryGetValue(c.takeHitFromId, out int hit);
                    return hit < c.takeHitLessThan;

                case AchievementConditionType.HealAtLeast:
                    return p.HighestHealOnRun >= c.healAtLeastOnRun;

                case AchievementConditionType.DiedAtLeast:
                    int diedCount = 0;

                    if (c.diedFromId == "*")
                        diedCount = p.totalDiedDictionary.Sum(e => e.Value);
                    else
                        p.totalKillDictionary.TryGetValue(c.diedFromId, out diedCount);

                    return diedCount >= c.diedAtLeast;
            }

            return false;
        }


        private void ApplyRewards(PlayerData p, AchievementEntry entry)
        {
            if (p == null || entry.rewards == null) return;

            foreach (var r in entry.rewards)
            {
                switch (r.type)
                {
                    case AchievementRewardType.NanoCoin:
                        //  p.nanoCoin += r.nanoAmount;
                        break;

                    case AchievementRewardType.UnlockMap:
                        if (!string.IsNullOrEmpty(r.refId))
                            ProgressionManager.Instance.UnlockMap(r.refId, saveNow: true);
                        break;

                    case AchievementRewardType.UnlockChallenge:
                        if (!string.IsNullOrEmpty(r.refId))
                            ProgressionManager.Instance.UnlockChallenge(r.refId, saveNow: true);
                        break;

                    case AchievementRewardType.UnlockPermanentUpgrade:
                        //
                        break;
                }
            }
        }

        #endregion

        private void EvaluateAll(AchievementContext ctx)
        {
            if (database.entries == null) return;

            foreach (var entry in database.entries)
            {
                if (entry == null) continue;
                if (entry.triggerType != ctx.TriggerType) continue;
                if (string.IsNullOrEmpty(entry.id)) continue;

                if (ProgressionManager.Instance.IsAchievementUnlocked(entry.id))
                    continue;

                if (!AreConditionsSatisfied(entry, ctx))
                    continue;

                ProgressionManager.Instance.UnlockAchievement(
                    entry.id,
                    player => ApplyRewards(player, entry),
                    saveNow: true,
                    silent: false);
            }
        }

        private bool AreConditionsSatisfied(AchievementEntry entry, AchievementContext ctx)
        {
            var list = entry.conditions;
            if (list == null || list.Count == 0)
                return true;

            switch (entry.logic)
            {
                case AchievementConditionLogic.And:
                    foreach (var c in list)
                        if (!EvaluateCondition(c, ctx))
                            return false;
                    return true;

                case AchievementConditionLogic.Or:
                    foreach (var c in list)
                        if (EvaluateCondition(c, ctx))
                            return true;
                    return false;

                default:
                    return true;
            }
        }
    }
}