using System.Collections.Generic;
using Characters.Controllers;
using Characters.StatusEffectSystems;
using Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Characters.CombatSystems
{
    /// <summary>
    /// Gives a character permanent contact damage through its own <see cref="DamageOnTouch"/>, plus optional
    /// status effects and "vanish on contact".
    /// <para/>
    /// Enemies normally only hurt on touch while a skill (e.g. a dash) calls <see cref="DamageOnTouch.EnableDamage(GameObject,string,object,float,float,float,float,float,float,float,bool,float)"/>.
    /// Put this on a prefab that has no such skill but should still hit whatever it touches - e.g. Bright2's
    /// Piece of Mine fragment: "when contact with a player deals damage and decreases a player's movement
    /// speed down for a period of time then disappear".
    /// </summary>
    [DisallowMultipleComponent]
    public class ContactEffectOnTouch : MonoBehaviour
    {
        #region Inspector & Variables

        [Title("Damage")]
        [PropertyTooltip("Flat damage added on top of the multiplier part (same formula as skills: "
                         + "Base + Multiplier% x character damage).")]
        [SerializeField] private float baseDamage;

        [Unit(Units.Percent)]
        [PropertyTooltip("Percent of the character's own damage stat dealt per hit.")]
        [SerializeField] private float damageMultiplier = 100f;

        [MinValue(0.01f)]
        [PropertyTooltip("Hits per second against the same target while it stays in contact.")]
        [SerializeField] private float hitPerSec = 1f;

        [Title("On Contact")]
        [PropertyTooltip("Applied to the target every time a contact hit lands.")]
        [SerializeField] private List<StatusEffectDataPayload> contactStatusEffects = new();

        [PropertyTooltip("Only players trigger the status effects and the despawn. Damage itself still goes "
                         + "to anything on the DamageOnTouch target layer.")]
        [SerializeField] private bool playerOnly = true;

        [PropertyTooltip("Dies right after the first contact hit (through the normal death path, so pools and "
                         + "summon bookkeeping still run).")]
        [SerializeField] private bool despawnOnContact = true;

        private BaseController _owner;
        private bool _hasHit;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            _owner = GetComponent<BaseController>();
            if (!_owner)
            {
                Debug.LogWarning($"[ContactEffectOnTouch] {name} has no BaseController.", this);
                return;
            }

            // ResetAllDependentBehavior wipes DamageOnTouch (pool reuse / summon), so arm again after it.
            _owner.OnResetAllBehavior += Arm;
            if (_owner.DamageOnTouch)
                _owner.DamageOnTouch.OnHit += HandleHit;
        }

        private void OnEnable() => Arm();

        private void OnDestroy()
        {
            if (!_owner) return;
            _owner.OnResetAllBehavior -= Arm;
            if (_owner.DamageOnTouch)
                _owner.DamageOnTouch.OnHit -= HandleHit;
        }

        #endregion

        #region Internals

        private void Arm()
        {
            if (!_owner || !_owner.DamageOnTouch || !_owner.CharacterData) return;
            if (_owner.HealthSystem != null && _owner.HealthSystem.IsDead) return;

            _hasHit = false;

            // Replace, never stack, our own instance - Arm can run twice (reset + enable) on one spawn.
            _owner.DamageOnTouch.DisableDamage(this);
            _owner.DamageOnTouch.EnableDamage(
                _owner.gameObject,
                _owner.CharacterData.CharacterId,
                this,
                hitPerSec,
                baseDamage,
                damageMultiplier);
        }

        private void HandleHit(GameObject target)
        {
            if (_hasHit || !target) return;
            if (!CombatManager.TryGetCharacterFromCache(target, out var hitController)) return;
            if (playerOnly && hitController is not PlayerController) return;

            if (contactStatusEffects != null && contactStatusEffects.Count > 0)
                StatusEffectManager.ApplyEffectTo(target, contactStatusEffects);

            if (!despawnOnContact) return;

            _hasHit = true;
            if (_owner.HealthSystem != null)
                _owner.HealthSystem.ForceDead();
        }

        #endregion
    }
}
