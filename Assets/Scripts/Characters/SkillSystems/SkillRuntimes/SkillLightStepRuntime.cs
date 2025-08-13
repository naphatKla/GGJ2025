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

        [Header("Camera (optional)")]
        [Tooltip("If null, will use Camera.main. Must be Orthographic for 2D OverlapArea bounds.")]
        [SerializeField]
        private Camera targetCamera;

        private bool _isWaitForCounterAttack;
        private bool _isWaitForMovementEnd;
        private bool _inGodSpeedPhase;

        private readonly HashSet<Transform> _dashedTargets = new();

        public override async void PerformSkill()
        {
            if (IsWaitForCondition) return;
            if (IsCooldown || IsPerforming) return;
            
            _isWaitForCounterAttack = true;
            _isWaitForMovementEnd = true;
            _dashedTargets.Clear();
            owner.CombatSystem.OnCounterAttack += TriggerCondition;

            await UniTask.WaitUntil(() => !_isWaitForCounterAttack, cancellationToken: cts.Token);
            await UniTask.WaitUntil(() => !owner.MovementSystem.IsMoveTweenActive, cancellationToken: cts.Token);

            if (cts.IsCancellationRequested)
            {
                ResetWaitingCondition();
                return;
            }

            base.PerformSkill();
        }

        protected override void OnSkillStart()
        {
            owner.TryPlayFeedback(FeedbackName.LightStepUse);
            owner.SkillSystem.SetCanUsePrimary(false);
            owner.SkillSystem.SetCanUseSecondary(false);
            owner.MovementSystem.CanInterruptTween = false;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            PlayerController player = owner as PlayerController;
            player?.CameraController.LerpOrthoSize(15f, 0.5f).Forget();

            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectWhileLightStep);
            owner.DamageOnTouch.EnableDamage(owner.gameObject, this, 4.5f, skillData.BaseDamagePerHit,
                skillData.DamageMultiplier, 0, 0, skillData.LifeStealPercentChance, skillData.LifeStealEffective);

            for (int i = 0; i < skillData.TargetAmount; i++)
            {
                var targetPosition = GetBestTargetPositionInView();
                if (targetPosition == null) break;

                owner.MovementSystem.StopTween();

                float speedMultiplier = Mathf.Clamp(
                    1f + i * (skillData.NormalPhaseSpeedStepUp / 100f),
                    1f, skillData.NormalPhaseMaxSpeedMultiplier / 100f
                );

                if (i >= skillData.GodSpeedPhaseStartHit)
                {
                    if (!_inGodSpeedPhase)
                    {
                        _inGodSpeedPhase = true;
                        owner.FeedbackSystem.SetIgnoreFeedback(FeedbackName.CounterAttack, true);
                        player?.CameraController.LerpOrthoSize(22f, 0.25f).Forget();
                        player?.CameraController.SetFollowTarget(null);
                    }

                    speedMultiplier += skillData.GodSpeedPhaseSpeedStepUp / 100f;
                    speedMultiplier = Mathf.Clamp(speedMultiplier, 1f,
                        skillData.GodSpeedPhaseMaxSpeedMultiplier / 100f);
                }

                var curve = skillData.RandomCurve.Count > 0
                    ? skillData.RandomCurve[Random.Range(0, skillData.RandomCurve.Count)]
                    : null;

                await owner.MovementSystem
                    .TryMoveToPositionBySpeed(targetPosition.Value, skillData.LightStepSpeed * speedMultiplier,
                        moveCurve: curve)
                    .SetEase(Ease.InSine)
                    .WithCancellation(cancelToken);

                if (cancelToken.IsCancellationRequested) break;
            }
        }

        protected override void OnSkillExit()
        {
            ResetOnEnd().Forget();
        }

        private void OnDisable()
        {
            ResetOnEnd().Forget();
        }

        private void ResetWaitingCondition()
        {
            _isWaitForCounterAttack = false;
            _isWaitForMovementEnd = false;
            owner.CombatSystem.OnCounterAttack -= TriggerCondition;
        }

        private async UniTaskVoid ResetOnEnd()
        {
            ResetWaitingCondition();

            _inGodSpeedPhase = false;
            owner.SkillSystem.SetCanUsePrimary(true);
            owner.SkillSystem.SetCanUseSecondary(true);
            owner.MovementSystem.CanInterruptTween = true;
            owner.DamageOnTouch.DisableDamage(this);

            owner.TryPlayFeedback(FeedbackName.LightStepEnd);
            owner.FeedbackSystem.SetIgnoreFeedback(FeedbackName.CounterAttack, false);

            if (owner is PlayerController player)
                player.CameraController.ResetCamera(0.25f);

            await UniTask.WaitForSeconds(0.5f, cancellationToken: destroyCancellationToken);
            StatusEffectManager.RemoveEffectAt(owner.gameObject, StatusEffectName.Iframe);
        }

        private void TriggerCondition() => _isWaitForCounterAttack = false;

        /// <summary>
        /// Pick the closest enemy to owner within current camera view (orthographic).
        /// If distance >= MinStepDistance => dash to enemy position.
        /// Else => dash MinStepDistance toward that enemy.
        /// Returns null if no enemy in view.
        /// </summary>
        private Vector2? GetBestTargetPositionInView()
        {
            var cam = targetCamera ? targetCamera : Camera.main;
            if (!cam) return null;

            if (!cam.orthographic)
            {
                // Game seems to be 2D with orthographic camera; if not, early out or adapt here.
                // You can replace this with a perspective-safe bounds calc if needed.
                return null;
            }

            // Build world AABB of the current camera view on XY
            Vector3 cpos = cam.transform.position;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Vector2 min = new Vector2(cpos.x - halfW, cpos.y - halfH);
            Vector2 max = new Vector2(cpos.x + halfW, cpos.y + halfH);

            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            Collider2D[] candidates = Physics2D.OverlapAreaAll(min, max, damageLayer);
            if (candidates == null || candidates.Length == 0) return null;

            Vector2 origin = owner.transform.position;
            float minStepSqr = skillData.MinStepDistance * skillData.MinStepDistance;

            Transform nearest = null;
            float nearestSqr = float.MaxValue;

            foreach (var col in candidates)
            {
                if (!col) continue;
                var t = col.transform;
                if (t == owner.transform) continue;

                // (ถ้าต้องการไม่ dash ซ้ำเป้าหมายเดิม ให้เปิดเช็คนี้)
                // if (_dashedTargets.Contains(t)) continue;

                float sqr = ((Vector2)t.position - origin).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = t;
                }
            }

            if (!nearest) return null;

            _dashedTargets.Add(nearest);

            if (nearestSqr >= minStepSqr)
            {
                // far enough: dash to enemy
                return (Vector2)nearest.position;
            }

            // too close: dash MinStepDistance toward enemy
            Vector2 dir = ((Vector2)nearest.position - origin);
            if (dir.sqrMagnitude < 1e-6f) return null;

            dir.Normalize();
            Vector2 fallback = origin + dir * skillData.MinStepDistance;

            // (ถ้าต้องการบังคับไม่ให้ออกนอกหน้าจอ ลอง clamp fallback ให้ยังอยู่ใน [min,max])
            fallback.x = Mathf.Clamp(fallback.x, min.x, max.x);
            fallback.y = Mathf.Clamp(fallback.y, min.y, max.y);

            return fallback;
        }
    }
}