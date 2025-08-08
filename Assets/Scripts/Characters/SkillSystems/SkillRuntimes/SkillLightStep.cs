using System.Collections.Generic;
using System.Threading;
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
    public class SkillLightStep : BaseSkillRuntime<SkillLightStepDataSo>, ISpecialConditionSkill
    {
        public bool IsWaitForCondition => _isWaitForCounterAttack || _isWaitForMovementEnd;
        private bool _isWaitForCounterAttack;
        private bool _isWaitForMovementEnd;
        private bool _inGodSpeedPhase;
        private float startTime;
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
            owner.TryStopFeedback(FeedbackName.LightStepUse);
            owner.TryPlayFeedback(FeedbackName.LightStepUse);
            owner.FeedbackSystem.SetIgnoreFeedback(FeedbackName.CounterAttack, true);

            startTime = Time.time;
            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectWhileLightStep);
            owner.DamageOnTouch.EnableDamage(owner.gameObject, this, 3);

            float radius = skillData.StartLightStepRadius;

            for (int i = 0; i < skillData.TargetAmount; i++)
            {
                var targetPosition = GetBestTargetPosition(radius, skillData.LightStepRadius);
                if (targetPosition == null) break;

                owner.MovementSystem.StopTween();

                float speedMultiplier = 1f + i * skillData.NormalPhaseSpeedStepUp;

                if (Time.time - startTime > skillData.GodSpeedPhaseStartTime)
                {
                    if (!_inGodSpeedPhase)
                    {
                        _inGodSpeedPhase = true;
                        // Enter god speed phase feedback or effect
                    }

                    speedMultiplier += skillData.GodSpeedPhaseSpeedStepUp;
                    speedMultiplier = Mathf.Clamp(speedMultiplier, 1, skillData.GodSpeedPhaseMaxSpeedMultiplier);
                }
                else
                {
                    speedMultiplier = Mathf.Clamp(speedMultiplier, 1, skillData.NormalPhaseMaxSpeedMultiplier);
                }

                var curve = skillData.RandomCurve.Count > 0
                    ? skillData.RandomCurve[Random.Range(0, skillData.RandomCurve.Count)]
                    : null;

                if (i >= skillData.TargetAmount - 1)
                {
                    owner.TryPlayFeedback(FeedbackName.LightStepEnd);
                    owner.FeedbackSystem.SetIgnoreFeedback(FeedbackName.LightStepEnd, true);
                }

                await owner.MovementSystem
                    .TryMoveToPositionBySpeed(targetPosition.Value, skillData.LightStepSpeed * speedMultiplier,
                        moveCurve: curve)
                    .SetEase(Ease.InSine)
                    .WithCancellation(cancelToken);

                radius = skillData.LightStepRadius;
            }

            _isSuccess = true;
        }

        protected override void OnSkillExit()
        {
            ResetOnEnd().Forget();
        }

        private void OnDisable()
        {
            ResetOnEnd().Forget();
        }

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
            owner.FeedbackSystem.SetIgnoreFeedback(FeedbackName.LightStepEnd, false);
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
            Transform bestNew = null;
            Transform bestRepeat = null;
            float bestNewScore = float.MaxValue;
            float bestRepeatScore = float.MaxValue;

            foreach (var (target, sqrDist) in validTargets)
            {
                float delta = Mathf.Abs(sqrDist - avgDist);

                int futureTargets = 0;
                Collider2D[] lookahead = Physics2D.OverlapCircleAll(target.position, lookaheadRadius, damageLayer);
                foreach (var l in lookahead)
                {
                    if (!l || l.transform == owner.transform || l.transform == target) continue;
                    if (_dashedTargets.Contains(l.transform)) continue;
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
                else
                {
                    if (score < bestRepeatScore)
                    {
                        bestRepeatScore = score;
                        bestRepeat = target;
                    }
                }
            }

            var chosen = bestNew ?? bestRepeat;
            if (chosen != null)
            {
                _dashedTargets.Add(chosen);
                Vector2 pos = chosen.position;
                float minDist = skillData.MinStepDistance;
                float currentDist = Vector2.Distance(origin, pos);
                if (currentDist < minDist)
                {
                    Vector2 dir = (pos - origin).normalized;
                    pos = origin + dir * minDist;
                }

                return pos;
            }

            return null;
        }
    }
}