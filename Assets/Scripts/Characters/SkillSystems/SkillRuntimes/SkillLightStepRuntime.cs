using System.Collections.Generic;
using System.Threading;
using Characters.Controllers;
using Characters.FeedbackSystems;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GlobalSettings;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillLightStepRuntime : BaseSkillRuntime<SkillLightStepDataSo>, ISpecialConditionSkill
    {
        public bool IsWaitForCondition => _isWaitForCounterAttack || _isWaitForMovementEnd;

        private bool _isWaitForCounterAttack;
        private bool _isWaitForMovementEnd;
        private bool _inGodSpeedPhase;
        private bool _isSuccess;

        private readonly HashSet<Transform> _dashedTargets = new();

        public override void UpdateCoolDown(float deltaTime)
        {
            if (IsWaitForCondition && IsPerforming) return;
            base.UpdateCoolDown(deltaTime);
        }

        protected override void OnSkillStart()
        {
            _isWaitForCounterAttack = true;
            _isWaitForMovementEnd = true;
            _dashedTargets.Clear();
            owner.CombatSystem.OnCounterAttack += TriggerCondition;
            _isSuccess = false;
            SetCurrentCooldown(0);
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await UniTask.WaitUntil(() => !_isWaitForCounterAttack, cancellationToken: cancelToken);
            await UniTask.WaitUntil(() => !owner.MovementSystem.IsMoveTweenActive, cancellationToken: cancelToken);

            _isWaitForMovementEnd = false;
            SetCurrentCooldown(skillData.Cooldown);
            if (cancelToken.IsCancellationRequested) return;

            owner.SkillSystem.SetCanUsePrimary(false);
            owner.SkillSystem.SetCanUseSecondary(false);
            owner.TryPlayFeedback(FeedbackName.LightStepUse);

            PlayerController player = owner as PlayerController;

            if (player)
                player.CameraController.LerpOrthoSize(15f, 0.5f).Forget();


            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectWhileLightStep);
            owner.DamageOnTouch.EnableDamage(owner.gameObject, this, 1, skillData.BaseDamagePerHit,
                skillData.DamageMultiplier, 0, 0, skillData.LifeStealPercentChance, skillData.LifeStealEffective);

            float radius = skillData.StartLightStepRadius;

            for (int i = 0; i < skillData.TargetAmount; i++)
            {
                var targetPosition = GetBestTargetPosition(radius, skillData.LightStepRadius);
                if (targetPosition == null) break;

                owner.MovementSystem.StopTween();

                float speedMultiplier = Mathf.Clamp(1f + i * (skillData.NormalPhaseSpeedStepUp/100),
                    1, skillData.NormalPhaseMaxSpeedMultiplier/100);

                if (i >= skillData.GodSpeedPhaseStartHit)
                {
                    if (!_inGodSpeedPhase)
                    {
                        _inGodSpeedPhase = true;
                        owner.FeedbackSystem.SetIgnoreFeedback(FeedbackName.CounterAttack, true);
                        player?.CameraController.LerpOrthoSize(22f, 0.25f).Forget();
                        player?.CameraController.SetFollowTarget(null);
                    }

                    speedMultiplier += skillData.GodSpeedPhaseSpeedStepUp/100;
                    speedMultiplier = Mathf.Clamp(speedMultiplier, 1, skillData.GodSpeedPhaseMaxSpeedMultiplier/100);
                }

                var curve = skillData.RandomCurve.Count > 0
                    ? skillData.RandomCurve[Random.Range(0, skillData.RandomCurve.Count)]
                    : null;

                await owner.MovementSystem
                    .TryMoveToPositionBySpeed(targetPosition.Value, skillData.LightStepSpeed * speedMultiplier,
                        moveCurve: curve)
                    .SetEase(Ease.InSine)
                    .WithCancellation(cancelToken);

                radius = skillData.LightStepRadius;
            }

            _isSuccess = true;
        }

        protected override void OnSkillExit() => ResetOnEnd().Forget();
        private void OnDisable() => ResetOnEnd().Forget();

        private async UniTaskVoid ResetOnEnd()
        {
            owner.CombatSystem.OnCounterAttack -= TriggerCondition;
            owner.SkillSystem.SetCanUsePrimary(true);
            owner.SkillSystem.SetCanUseSecondary(true);
            owner.DamageOnTouch.DisableDamage(this);

            _isWaitForCounterAttack = false;
            _isWaitForMovementEnd = false;
            _inGodSpeedPhase = false;

            if (!_isSuccess) return;

            owner.TryPlayFeedback(FeedbackName.LightStepEnd);
            owner.FeedbackSystem.SetIgnoreFeedback(FeedbackName.CounterAttack, false);

            if (owner is PlayerController player)
            {
                player.CameraController.ResetCamera(0.25f);
                player.MovementSystem.TryMoveToPositionBySpeed(
                    player.CameraController.GetCameraCenterWorldPosition(),
                    skillData.LightStepSpeed);
            }

            await UniTask.WaitForSeconds(0.5f, cancellationToken: destroyCancellationToken);
            StatusEffectManager.RemoveEffectAt(owner.gameObject, StatusEffectName.Iframe);
        }

        private void TriggerCondition() => _isWaitForCounterAttack = false;

        private Vector2? GetBestTargetPosition(float searchRadius, float lookaheadRadius)
        {
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            Collider2D[] candidates = Physics2D.OverlapCircleAll(owner.transform.position, searchRadius, damageLayer);
            if (candidates.Length == 0) return null;

            Vector2 origin = owner.transform.position;
            float totalDist = 0f;
            int count = 0;
            List<(Transform target, float sqrDist)> validTargets = new();

            foreach (var collider in candidates)
            {
                if (!collider || collider.transform == owner.transform) continue;
                Transform target = collider.transform;
                float sqrDist = ((Vector2)target.position - origin).sqrMagnitude;
                totalDist += sqrDist;
                count++;
                validTargets.Add((target, sqrDist));
            }

            if (count == 0) return null;

            float avgDist = totalDist / count;
            Transform bestNew = null, bestRepeat = null;
            float bestNewScore = float.MaxValue, bestRepeatScore = float.MaxValue;

            foreach (var (target, sqrDist) in validTargets)
            {
                float delta = Mathf.Abs(sqrDist - avgDist);
                int futureTargets = 0;

                foreach (var l in Physics2D.OverlapCircleAll(target.position, lookaheadRadius, damageLayer))
                {
                    if (!l || l.transform == owner.transform || l.transform == target ||
                        _dashedTargets.Contains(l.transform))
                        continue;
                    futureTargets++;
                }

                float score = delta - futureTargets * 0.1f;

                if (!_dashedTargets.Contains(target))
                {
                    if (score < bestNewScore)
                    {
                        bestNewScore = score;
                        bestNew = target;
                    }
                }
                else if (score < bestRepeatScore)
                {
                    bestRepeatScore = score;
                    bestRepeat = target;
                }
            }

            var chosen = bestNew ?? bestRepeat;
            if (chosen == null) return null;

            _dashedTargets.Add(chosen);
            Vector2 pos = chosen.position;
            float currentDist = Vector2.Distance(origin, pos);

            if (currentDist < skillData.MinStepDistance)
                pos = origin + (pos - origin).normalized * skillData.MinStepDistance;

            return pos;
        }
    }
}