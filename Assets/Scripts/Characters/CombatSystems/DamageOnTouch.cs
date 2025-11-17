using System;
using System.Collections.Generic;
using UnityEngine;
using Manager;
using Sirenix.OdinInspector;

namespace Characters.CombatSystems
{
    public class DamageOnTouch : MonoBehaviour, IFixedUpdateable
    {
        private class DamageInstance
        {
            public object Caller;
            public float HitPerSec;
            public float BaseSkillDamage;
            public float DamageMultiplier;
            public float AdditionalCriRate;
            public float AdditionalCriDmg;
            public float LifeStealPercent;
            public float LifeStealEffective;

            // สามารถยิง event ชนกับ DamageOnTouch เป้าหมายได้ไหม
            public bool CanHitWithDamageOnTouch;
        }

        public enum OverlapShape
        {
            Box,
            Circle
        }

        [Title("Overlap Config")]
#if UNITY_EDITOR
        [OnValueChanged(nameof(OnShapeChanged))]
#endif
        public OverlapShape shape = OverlapShape.Box;

        [ShowIf(nameof(IsBox)), BoxGroup("Box"), LabelText("Size")]
        public Vector2 boxSize = Vector2.one;

        [ShowIf(nameof(IsCircle)), BoxGroup("Circle"), LabelText("Radius")]
        public float circleRadius = 0.5f;

        [LabelText("Target Layer")]
        public LayerMask targetLayer;

        [ShowInInspector, ReadOnly, ShowIf("@UnityEngine.Application.isPlaying")]
        private bool _isEnableDamage;

        private GameObject _owner;

        // --- Core data ---
        private readonly List<DamageInstance> _damageInstances = new();
        private readonly Dictionary<(GameObject target, object caller), float> _cooldownMap = new();
        private readonly List<(GameObject, object)> _cooldownRemoveBuffer = new();

        // --- Physics buffer ---
        private readonly Collider2D[] _overlapResults = new Collider2D[32];

        // Snapshot buffer to avoid "modified during iteration"
        private readonly List<DamageInstance> _iterBuffer = new(8);

        public event Action<GameObject> OnHit;

        /// <summary>
        /// เมื่อชนกับเป้าหมายที่มี DamageOnTouch เปิดอยู่ด้วย
        /// int = ค่าดาเมจโดยประมาณต่อ 1 hit ของ DamageOnTouch เป้าหมาย
        /// </summary>
        public event Action<int> OnHitWithDamageOnTouch;

        public GameObject Owner => _owner;
        public bool IsEnableDamage => _isEnableDamage;

        #region Public API

        public void EnableDamage(
            GameObject owner,
            object caller,
            float hitPerSec,
            float baseSkillDamage,
            float damageMultiplier = 100f,
            float additionalCriRate = 0f,
            float additionalCriDmg = 0f,
            float lifeStealPercent = 0f,
            float lifeStealEffective = 0f,
            bool canHitWithDamageOnTouch = false)
        {
            InternalEnableDamage(
                owner,
                caller,
                hitPerSec,
                baseSkillDamage,
                damageMultiplier,
                additionalCriRate,
                additionalCriDmg,
                lifeStealPercent,
                lifeStealEffective,
                canHitWithDamageOnTouch);
        }

        [Button]
        public void EnableDamage(
            GameObject owner,
            object caller,
            float hitPerSec,
            OverlapShape shape,
            LayerMask? layerMask = null,
            Vector2? box = null,
            float? circle = null,
            float baseSkillDamage = 0f,
            float damageMultiplier = 100f,
            float additionalCriRate = 0f,
            float additionalCriDmg = 0f,
            float lifeStealPercent = 0f,
            float lifeStealEffective = 0f,
            bool canHitWithDamageOnTouch = false)
        {
            this.shape = shape;
            if (layerMask.HasValue) targetLayer = layerMask.Value;
            if (box.HasValue) boxSize = box.Value;
            if (circle.HasValue) circleRadius = circle.Value;

            InternalEnableDamage(
                owner,
                caller,
                hitPerSec,
                baseSkillDamage,
                damageMultiplier,
                additionalCriRate,
                additionalCriDmg,
                lifeStealPercent,
                lifeStealEffective,
                canHitWithDamageOnTouch);
        }

        private void InternalEnableDamage(
            GameObject owner,
            object caller,
            float hitPerSec,
            float baseSkillDamage,
            float damageMultiplier,
            float additionalCriRate,
            float additionalCriDmg,
            float lifeStealPercent,
            float lifeStealEffective,
            bool canHitWithDamageOnTouch)
        {
            if (caller == null || owner == null)
            {
                Debug.LogWarning("[DamageOnTouch] Invalid caller or owner.");
                return;
            }

            if (_owner == null)
            {
                _owner = owner;
                gameObject.layer = owner.layer;
            }
            else if (_owner != owner)
            {
                Debug.LogWarning("[DamageOnTouch] Already assigned to a different owner.");
                return;
            }

            _damageInstances.Add(new DamageInstance
            {
                Caller = caller,
                HitPerSec = Mathf.Max(hitPerSec, 0.01f),
                BaseSkillDamage = baseSkillDamage,
                DamageMultiplier = damageMultiplier,
                AdditionalCriRate = additionalCriRate,
                AdditionalCriDmg = additionalCriDmg,
                LifeStealPercent = lifeStealPercent,
                LifeStealEffective = lifeStealEffective,
                CanHitWithDamageOnTouch = canHitWithDamageOnTouch,
            });

            if (!_isEnableDamage)
            {
                _isEnableDamage = true;
                FixedUpdateManager.Instance.Register(this);
            }
        }

        public void DisableDamage(object caller)
        {
            // Remove all instances of this caller
            _damageInstances.RemoveAll(instance => instance.Caller == caller);

            // Clean cooldown entries for this caller
            _cooldownRemoveBuffer.Clear();
            foreach (var kvp in _cooldownMap)
            {
                if (kvp.Key.caller == caller)
                    _cooldownRemoveBuffer.Add(kvp.Key);
            }
            foreach (var key in _cooldownRemoveBuffer)
                _cooldownMap.Remove(key);

            // Turn off if empty
            if (_damageInstances.Count == 0)
            {
                _isEnableDamage = false;
                _owner = null;
                FixedUpdateManager.Current?.Unregister(this);
            }
        }

        public void ResetDamageOnTouch()
        {
            _damageInstances.Clear();
            _cooldownMap.Clear();
            _cooldownRemoveBuffer.Clear();
            _iterBuffer.Clear();

            if (_isEnableDamage)
                FixedUpdateManager.Current?.Unregister(this);

            _isEnableDamage = false;
            _owner = null;
        }

        /// <summary>
        /// ดาเมจโดยประมาณต่อ 1 hit ของ DamageOnTouch ตัวนี้
        /// ใช้ตอนโดนชนโดยอีกฝั่งที่สนใจ counter / compare damage
        /// </summary>
        public int GetApproxTotalDamagePerHit()
        {
            float total = 0f;

            for (int i = 0; i < _damageInstances.Count; i++)
            {
                var inst = _damageInstances[i];
                if (inst == null) continue;

                total += inst.BaseSkillDamage * (inst.DamageMultiplier / 100f);
            }

            return Mathf.CeilToInt(total);
        }

        #endregion

        #region Damage Logic

        public void OnFixedUpdate()
        {
            if (!_isEnableDamage || _owner == null)
                return;

            // Snapshot instances to avoid "modified during iteration"
            _iterBuffer.Clear();
            _iterBuffer.AddRange(_damageInstances);

            // If nothing to apply, early out before physics
            if (_iterBuffer.Count == 0)
                return;

            // Physics query
            int count = 0;
            Vector2 position = transform.position;
            float angle = transform.eulerAngles.z;

            switch (shape)
            {
                case OverlapShape.Box:
                    count = Physics2D.OverlapBoxNonAlloc(position, boxSize, angle, _overlapResults, targetLayer);
                    break;

                case OverlapShape.Circle:
                    count = Physics2D.OverlapCircleNonAlloc(position, circleRadius, _overlapResults, targetLayer);
                    break;
            }

            for (int i = 0; i < count; i++)
            {
                var col = _overlapResults[i];
                if (!col) continue;

                // Use the snapshot for this whole pass
                TryApplyDamageTo(col, _iterBuffer);
            }
        }

        // Use instances snapshot instead of iterating the live list
        private void TryApplyDamageTo(Collider2D collider, List<DamageInstance> instancesSnapshot)
        {
            GameObject target = collider.gameObject;
            float now = Time.time;
            Vector2 hitPosition = collider.ClosestPoint(transform.position);

            for (int i = 0; i < instancesSnapshot.Count; i++)
            {
                var instance = instancesSnapshot[i];
                if (instance == null) continue;

                var key = (target, instance.Caller);

                if (_cooldownMap.TryGetValue(key, out float nextTime) && now < nextTime)
                    continue;

                CombatManager.ApplyCalculatedDamageTo(
                    target,
                    _owner,
                    gameObject,
                    hitPosition,
                    instance.BaseSkillDamage,
                    instance.DamageMultiplier,
                    instance.AdditionalCriRate,
                    instance.AdditionalCriDmg,
                    instance.LifeStealPercent,
                    instance.LifeStealEffective);

                float cooldown = 1f / instance.HitPerSec;
                _cooldownMap[key] = now + cooldown;
                OnHit?.Invoke(target);

                // ถ้า instance นี้เปิดให้เช็คชนกับ DamageOnTouch เป้าหมาย
                if (instance.CanHitWithDamageOnTouch && OnHitWithDamageOnTouch != null)
                {
                    var targetDoT = target.GetComponent<DamageOnTouch>();
                    if (targetDoT != null && targetDoT.IsEnableDamage)
                    {
                        int targetDamage = targetDoT.GetApproxTotalDamagePerHit();
                        OnHitWithDamageOnTouch?.Invoke(targetDamage);
                    }
                }
            }
        }

        #endregion

        #region Odin Helper

        private bool IsBox() => shape == OverlapShape.Box;
        private bool IsCircle() => shape == OverlapShape.Circle;

#if UNITY_EDITOR
        private void OnShapeChanged() => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif

        #endregion

        #region Safety

        private void OnDisable()
        {
            if (_isEnableDamage)
                FixedUpdateManager.Current?.Unregister(this);
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector2 pos = transform.position;
            float angle = transform.eulerAngles.z;

            switch (shape)
            {
                case OverlapShape.Box:
                    Matrix4x4 rotMatrix = Matrix4x4.TRS(pos, Quaternion.Euler(0, 0, angle), Vector3.one);
                    Gizmos.matrix = rotMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, boxSize);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;

                case OverlapShape.Circle:
                    UnityEditor.Handles.color = Color.red;
                    UnityEditor.Handles.DrawWireDisc(pos, Vector3.forward, circleRadius);
                    break;
            }
        }
#endif
    }
}
