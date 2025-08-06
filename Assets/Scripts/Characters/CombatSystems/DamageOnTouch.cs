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
        }

        public enum OverlapShape
        {
            Box,
            Circle
        }

        [Title("Overlap Config")]
        [OnValueChanged(nameof(OnShapeChanged))]
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
        private readonly List<DamageInstance> _damageInstances = new();
        private readonly Dictionary<(GameObject target, object caller), float> _cooldownMap = new();
        private readonly List<(GameObject, object)> _cooldownRemoveBuffer = new();

        private readonly Collider2D[] _overlapResults = new Collider2D[32];

        public GameObject Owner => _owner;
        public bool IsEnableDamage => _isEnableDamage;

        #region Public API

        public void EnableDamage(GameObject owner, object caller, float hitPerSec)
        {
            InternalEnableDamage(owner, caller, hitPerSec);
        }

        public void EnableDamage(GameObject owner, object caller, float hitPerSec,
            OverlapShape shape, LayerMask layerMask,
            Vector2? box = null, float? circle = null)
        {
            this.shape = shape;
            this.targetLayer = layerMask;

            if (box.HasValue) boxSize = box.Value;
            if (circle.HasValue) circleRadius = circle.Value;

            InternalEnableDamage(owner, caller, hitPerSec);
        }

        private void InternalEnableDamage(GameObject owner, object caller, float hitPerSec)
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
            });

            if (!_isEnableDamage)
            {
                _isEnableDamage = true;
                FixedUpdateManager.Instance.Register(this);
            }
        }

        public void DisableDamage(object caller)
        {
            _damageInstances.RemoveAll(instance => instance.Caller == caller);

            _cooldownRemoveBuffer.Clear();
            foreach (var kvp in _cooldownMap)
            {
                if (kvp.Key.caller == caller)
                    _cooldownRemoveBuffer.Add(kvp.Key);
            }

            foreach (var key in _cooldownRemoveBuffer)
                _cooldownMap.Remove(key);

            if (_damageInstances.Count == 0)
            {
                _isEnableDamage = false;
                _owner = null;
                FixedUpdateManager.Instance.Unregister(this);
            }
        }

        public void ResetDamageOnTouch()
        {
            _damageInstances.Clear();
            _cooldownMap.Clear();
            _cooldownRemoveBuffer.Clear();

            if (_isEnableDamage)
                FixedUpdateManager.Instance.Unregister(this);

            _isEnableDamage = false;
            _owner = null;
        }

        #endregion

        #region Damage Logic

        public void OnFixedUpdate()
        {
            if (!_isEnableDamage || _owner == null)
                return;

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
                TryApplyDamageTo(_overlapResults[i]);
            }
        }

        private void TryApplyDamageTo(Collider2D collider)
        {
            GameObject target = collider.gameObject;
            float now = Time.time;
            Vector2 hitPosition = collider.ClosestPoint(transform.position);

            foreach (var instance in _damageInstances)
            {
                var key = (target, instance.Caller);

                if (_cooldownMap.TryGetValue(key, out float nextTime) && now < nextTime)
                    continue;

                CombatManager.ApplyDamageTo(target, _owner, hitPosition);
                float cooldown = 1f / instance.HitPerSec;
                _cooldownMap[key] = now + cooldown;
            }
        }

        #endregion

        #region Odin Helper

        private bool IsBox() => shape == OverlapShape.Box;
        private bool IsCircle() => shape == OverlapShape.Circle;
        private void OnShapeChanged() => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

        #endregion

        #region Safety (Optional)

        private void OnDisable()
        {
            if (_isEnableDamage)
                FixedUpdateManager.Instance.Unregister(this);
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
