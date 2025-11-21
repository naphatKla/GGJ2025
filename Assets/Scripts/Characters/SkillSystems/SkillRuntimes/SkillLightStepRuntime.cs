using System;
using System.Collections.Generic;
using System.Threading;
using Cameras;
using Characters.Controllers;
using Characters.HeathSystems;
using Characters.SO.SkillDataSo;
using Characters.StatusEffectSystems;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GlobalSettings;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Characters.SkillSystems.SkillRuntimes
{
    public class SkillLightStepRuntime : BaseSkillRuntime<SkillLightStepDataSo>
    {
        [Header("Camera (optional)")]
        [Tooltip("If null, will use Camera.main. Must be Orthographic for 2D OverlapArea bounds.")]
        [SerializeField]
        private Camera targetCamera;

        private bool _inGodSpeedPhase;
        private readonly HashSet<Transform> _dashedTargets = new();

        // ใช้ buffer ร่วมกันสำหรับ OverlapAreaNonAlloc
        private readonly Collider2D[] _candidates = new Collider2D[64];

        public override void PerformSkill()
        {
            if (IsCooldown || IsPerforming) return;
            if (!StartConditionCheck()) return;
            base.PerformSkill();
        }

        protected override void OnSkillStart()
        {
            _dashedTargets.Clear();
            owner.SkillSystem.SetCanUsePrimary(false);
            owner.SkillSystem.SetCanUseSecondary(false);
            owner.MovementSystem.CanInterruptTween = false;
        }

        protected override async UniTask OnSkillUpdate(CancellationToken cancelToken)
        {
            var camHandle = Cinemachine2DCameraController.Instance.PushOrtho(15.5f, 10, this, 0.25f);

            StatusEffectManager.ApplyEffectTo(owner.gameObject, skillData.EffectWhileLightStep);

            for (int i = 0; i < skillData.TargetAmount; i++)
            {
                var targetPosition = GetBestTargetPositionInView();

                owner.DamageOnTouch.DisableDamage(this);
                owner.DamageOnTouch.EnableDamage(
                    owner.gameObject,
                    owner.CharacterData.CharacterId,
                    this,
                    7f,
                    skillData.BaseDamagePerHit,
                    skillData.DamageMultiplier,
                    0, 0,
                    skillData.LifeStealPercentChance,
                    skillData.LifeStealEffective
                );

                if (targetPosition == null)
                {
                    break;
                }

                owner.MovementSystem.StopTween();

                var speedMultiplier = Mathf.Clamp(
                    1f + i * (skillData.NormalPhaseSpeedStepUp / 100f),
                    1f,
                    skillData.NormalPhaseMaxSpeedMultiplier / 100f
                );

                if (i >= skillData.GodSpeedPhaseStartHit)
                {
                    if (!_inGodSpeedPhase)
                    {
                        _inGodSpeedPhase = true;
                        Cinemachine2DCameraController.Instance.PushOrtho(24, 10f, this, 0.25f);
                        Cinemachine2DCameraController.Instance.CancelRequest(camHandle);
                        Cinemachine2DCameraController.Instance.SetFollowTarget(null);
                    }

                    speedMultiplier += skillData.GodSpeedPhaseSpeedStepUp / 100f;
                    speedMultiplier = Mathf.Clamp(
                        speedMultiplier,
                        1f,
                        skillData.GodSpeedPhaseMaxSpeedMultiplier / 100f
                    );
                }

                var curve = skillData.RandomCurve.Count > 0
                    ? skillData.RandomCurve[Random.Range(0, skillData.RandomCurve.Count)]
                    : null;

                await owner.MovementSystem
                    .TryMoveToPositionBySpeed(
                        targetPosition.Value,
                        skillData.LightStepSpeed * speedMultiplier,
                        moveCurve: curve
                    )
                    .SetEase(Ease.InSine)
                    .WithCancellation(cancelToken);

                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        protected override void OnSkillExit()
        {
            ResetOnEnd().Forget();

            if (!owner || !owner.gameObject.activeSelf) return;

            Vector2 endPos = (Vector2)owner.transform.position +
                             (owner.InputSystem.SightDirection.direction * 15f);

            owner.MovementSystem
                .TryMoveToPositionBySpeed(endPos, skillData.LightStepSpeed)
                .SetEase(Ease.InSine);
        }

        private void OnDisable()
        {
            ResetOnEnd().Forget();
        }

        // ====================== START CONDITION ======================

        private bool StartConditionCheck()
        {
            var cam = targetCamera ? targetCamera : Camera.main;

            if (!cam) return false;
            if (!cam.orthographic) return false;

            // Build world AABB of the current camera view on XY
            Vector3 cpos = cam.transform.position;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Vector2 min = new Vector2(cpos.x - halfW, cpos.y - halfH);
            Vector2 max = new Vector2(cpos.x + halfW, cpos.y + halfH);

            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            int targetFound = Physics2D.OverlapAreaNonAlloc(min, max, _candidates, damageLayer);

            if (targetFound <= 0) return false;

            int targetAimable = 0;
            float sumHpOfTargets = 0f;

            for (int i = 0; i < targetFound; i++)
            {
                var col = _candidates[i];
                if (!col) continue;

                // ใช้ GetComponentInParent เผื่อ enemy มีหลาย collider
                var health = col.GetComponentInParent<HealthSystem>();
                if (!health) continue;
                if (!health.CanAim) continue;

                sumHpOfTargets += health.CurrentHealth;
                targetAimable++;
            }

            var damageCalculatedPerHit = owner.CombatSystem.CalculateSkillDamageDeal(
                null,
                Vector2.zero,
                skillData.BaseDamagePerHit,
                skillData.DamageMultiplier,
                0, 0, 0, 0
            );

            const int minimumHitToStartSkill = 3;
            const int minimumTargetInRange = 3;
            
            if (sumHpOfTargets < (damageCalculatedPerHit.Damage * minimumHitToStartSkill) 
                && targetAimable < minimumTargetInRange)
            {
                return false;
            }
               

            return true;
        }

        // ====================== RESET ======================

        private async UniTaskVoid ResetOnEnd()
        {
            _inGodSpeedPhase = false;
            owner.SkillSystem.SetCanUsePrimary(true);
            owner.SkillSystem.SetCanUseSecondary(true);
            owner.MovementSystem.CanInterruptTween = true;
            owner.DamageOnTouch.DisableDamage(this);

            if (owner is PlayerController player && Cinemachine2DCameraController.Current)
            {
                Cinemachine2DCameraController.Current.CancelByOwner(this);
                Cinemachine2DCameraController.Current.SetFollowTarget(player.transform);
            }

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (owner)
                    StatusEffectManager.RemoveEffectAt(owner.gameObject, StatusEffectName.Iframe);
            }
        }

        // ====================== PICK BEST TARGET ======================

        /// <summary>
        /// Pick the closest enemy to owner within current camera view (orthographic),
        /// โดยจะเล็งเฉพาะตัวที่มี HealthSystem และ HealthSystem.CanAim == true.
        /// If distance >= MinStepDistance => dash to enemy position.
        /// Else => dash MinStepDistance toward that enemy.
        /// Returns null if no valid enemy in view.
        /// </summary>
        private Vector2? GetBestTargetPositionInView()
        {
            var cam = targetCamera ? targetCamera : Camera.main;
            if (!cam) return null;
            if (!cam.orthographic) return null;

            // Build world AABB of the current camera view on XY
            Vector3 cpos = cam.transform.position;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Vector2 min = new Vector2(cpos.x - halfW, cpos.y - halfH);
            Vector2 max = new Vector2(cpos.x + halfW, cpos.y + halfH);

            LayerMask damageLayer = CharacterGlobalSettings.Instance.EnemyLayerDictionary[owner.tag];
            int count = Physics2D.OverlapAreaNonAlloc(min, max, _candidates, damageLayer);
            if (count <= 0) return null;

            Vector2 origin = owner.transform.position;
            float minStepSqr = skillData.MinStepDistance * skillData.MinStepDistance;

            HealthSystem nearestHealth = null;
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = _candidates[i];
                if (!col) continue;

                var health = col.GetComponentInParent<HealthSystem>();
                if (!health) continue;
                if (!health.CanAim) continue;

                Transform t = health.transform;
                if (t == owner.transform) continue;

                // ถ้าอยากไม่ dash ซ้ำเป้าเดิม เปิดเช็คนี้ได้
                // if (_dashedTargets.Contains(t)) continue;

                float sqr = ((Vector2)t.position - origin).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearestHealth = health;
                }
            }

            if (!nearestHealth) return null;

            Transform nearest = nearestHealth.transform;
            _dashedTargets.Add(nearest);

            if (nearestSqr >= minStepSqr)
            {
                // far enough: dash to enemy
                return (Vector2)nearest.position;
            }

            // too close: dash MinStepDistance toward enemy
            Vector2 dir = ((Vector2)nearest.position - origin);
            if (dir.sqrMagnitude <= float.Epsilon)
                dir = owner.InputSystem.SightDirection.direction != Vector2.zero
                    ? owner.InputSystem.SightDirection.direction
                    : Vector2.right; // fallback เล็กน้อยกัน zero vector

            dir.Normalize();
            Vector2 fallback = origin + dir * skillData.MinStepDistance;

            // บังคับไม่ให้ออกนอกจอ
            fallback.x = Mathf.Clamp(fallback.x, min.x, max.x);
            fallback.y = Mathf.Clamp(fallback.y, min.y, max.y);

            return fallback;
        }
    }
}
