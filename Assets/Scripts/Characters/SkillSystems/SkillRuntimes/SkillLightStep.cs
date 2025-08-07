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

        public override void UpdateCoolDown(float deltaTime)
        {
            if (IsWaitForCondition && IsPerforming) return;
            base.UpdateCoolDown(deltaTime);
        }

        protected override void OnSkillStart()
        {
            _isWaitForCounterAttack = true;
            _isWaitForMovementEnd = true;
            owner.CombatSystem.OnCounterAttack += TriggerCondition;
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
            owner.TryPlayFeedback(FeedbackName.LightStepNormalPhase);
            startTime = Time.time;
            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectWhileLightStep);

            Transform closetTarget = GetFurthestNonRepeatedTarget(skillData.StartLightStepRadius);
            if (!closetTarget) return;
            owner.DamageOnTouch.EnableDamage(owner.gameObject, this, 3);
            var speedMultiplier = 1f;
            var radius = skillData.StartLightStepRadius;
            owner.MovementSystem.StopTween();

            for (int i = 0; i < skillData.TargetAmount; i++)
            {
                var randomCurveIndex = Random.Range(0, skillData.RandomCurve.Count);
                var randomCurve = skillData.RandomCurve.Count > 0 ? skillData.RandomCurve[randomCurveIndex] : null;

                speedMultiplier = Mathf.Clamp(speedMultiplier + skillData.NormalPhaseSpeedStepUp, 1,
                    skillData.NormalPhaseMaxSpeedMultiplier);

                // god speed phase
                if (Time.time - startTime > skillData.GodSpeedPhaseStartTime)
                {
                    if (!_inGodSpeedPhase)
                    {
                        _inGodSpeedPhase = true;
                        owner.TryStopFeedback(FeedbackName.LightStepNormalPhase);
                        owner.TryPlayFeedback(FeedbackName.LightStepGodSpeedPhase);
                    }
                    
                    speedMultiplier = Mathf.Clamp(speedMultiplier + skillData.GodSpeedPhaseSpeedStepUp, 1,
                        skillData.GodSpeedPhaseMaxSpeedMultiplier);
                }

                await owner.MovementSystem.TryMoveToPositionBySpeed(closetTarget.position,
                        skillData.LightStepSpeed * speedMultiplier, moveCurve: randomCurve).SetEase(Ease.InSine)
                    .WithCancellation(cancelToken);

                closetTarget = GetFurthestNonRepeatedTarget(radius);
                radius = skillData.LightStepRadius;
                if (!closetTarget) break;
            }
        }

        protected override void OnSkillExit()
        {
            ResetOnEnd();
        }

        private void OnDisable()
        {
            ResetOnEnd();
        }

        private void ResetOnEnd()
        {
            owner.CombatSystem.OnCounterAttack -= TriggerCondition;
            owner.SkillSystem.SetCanUsePrimary(true);
            owner.SkillSystem.SetCanUseSecondary(true);
            owner.DamageOnTouch.DisableDamage(this);
            StatusEffectManager.RemoveEffectAt(owner.gameObject, StatusEffectName.Iframe);
            _isWaitForCounterAttack = false;
            _isWaitForMovementEnd = false;
            _inGodSpeedPhase = false;
        }

        private void TriggerCondition() => _isWaitForCounterAttack = false;

        private Transform GetFurthestNonRepeatedTarget(float radius)
        {
            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            Collider2D[] targetsInRange = Physics2D.OverlapCircleAll(owner.transform.position, radius, damageLayer);

            Transform furthest = null;
            float maxSqrDistance = 0f;
            Vector2 origin = owner.transform.position;

            foreach (var collider in targetsInRange)
            {
                if (!collider || collider.transform == owner.transform) continue;

                Transform candidate = collider.transform;

                float sqrDist = ((Vector2)candidate.position - origin).sqrMagnitude;
                if (sqrDist > maxSqrDistance)
                {
                    maxSqrDistance = sqrDist;
                    furthest = candidate;
                }
            }
            
            return furthest;
        }
    }
}