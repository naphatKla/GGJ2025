using System.Threading;
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
        public bool IsWaitForCondition { get; private set; }
        private Transform _lastTarget;

        protected override void OnSkillStart()
        {
            IsWaitForCondition = true;
            owner.CombatSystem.OnCounterAttack += TriggerCondition;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            await UniTask.WaitUntil(() => !IsWaitForCondition, cancellationToken: cancelToken);
            if (cancelToken.IsCancellationRequested) return;
            owner.SkillSystem.SetCanUsePrimary(false);
            owner.SkillSystem.SetCanUseSecondary(false);
            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectWhileLightStep);

            Transform closetTarget = GetClosestNonRepeatedTarget(skillData.StartLightStepRadius);
            if (!closetTarget) return;
            owner.DamageOnTouch.EnableDamage(owner.gameObject, this, 3);

            for (int i = 0; i < skillData.TargetAmount; i++)
            {
                await owner.MovementSystem.TryMoveToPositionBySpeed(closetTarget.position, skillData.LightStepSpeed).SetEase(Ease.InOutQuad)
                    .WithCancellation(cancelToken);
                closetTarget = GetClosestNonRepeatedTarget(skillData.StartLightStepRadius);
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
        }

        private void TriggerCondition() => IsWaitForCondition = false;

        private Transform GetClosestNonRepeatedTarget(float radius)
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
                if (candidate == _lastTarget) continue;

                float sqrDist = ((Vector2)candidate.position - origin).sqrMagnitude;
                if (sqrDist > maxSqrDistance)
                {
                    maxSqrDistance = sqrDist;
                    furthest = candidate;
                }
            }

            if (furthest != null)
                _lastTarget = furthest;

            return furthest;
        }
    }
}