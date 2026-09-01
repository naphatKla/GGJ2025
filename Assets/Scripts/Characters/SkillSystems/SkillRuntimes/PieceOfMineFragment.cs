using System.Collections.Generic;
using Characters.Controllers;
using Characters.StatusEffectSystems;
using Manager;
using UnityEngine;

namespace Characters.SkillSystems.SkillRuntimes
{
    /// <summary>
    /// Attached at runtime to every fragment spawned by Bright2's Piece of Mine. The fragment chases and
    /// damages the player like any other enemy - this only adds the tail of the spec: "when contact with a
    /// player deals damage and decreases a player's movement speed down for a period of time then disappear".
    /// <para/>
    /// The damage itself is left to the fragment's own DamageOnTouch, so the two never double up.
    /// </summary>
    [DisallowMultipleComponent]
    public class PieceOfMineFragment : MonoBehaviour
    {
        private EnemyController _fragment;
        private List<StatusEffectDataPayload> _contactEffects;
        private bool _despawnOnContact;

        /// <summary>Fragments are pooled, so this is reset every time one is handed out again.</summary>
        private bool _hasHitPlayer;

        public void Bind(EnemyController fragment, List<StatusEffectDataPayload> contactEffects, bool despawnOnContact)
        {
            Unsubscribe();

            _fragment = fragment;
            _contactEffects = contactEffects;
            _despawnOnContact = despawnOnContact;
            _hasHitPlayer = false;

            if (_fragment && _fragment.DamageOnTouch)
                _fragment.DamageOnTouch.OnHit += HandleHit;
        }

        private void HandleHit(GameObject target)
        {
            if (_hasHitPlayer || !target) return;
            if (!CombatManager.TryGetCharacterFromCache(target, out var hitController)) return;
            if (hitController is not PlayerController) return;

            _hasHitPlayer = true;

            if (_contactEffects != null && _contactEffects.Count > 0)
                StatusEffectManager.ApplyEffectTo(target, _contactEffects);

            if (!_despawnOnContact) return;
            if (!_fragment || _fragment.HealthSystem == null) return;

            // Goes through the normal death path so the summon runtime's pool/destroy bookkeeping still runs.
            _fragment.HealthSystem.ForceDead();
        }

        private void Unsubscribe()
        {
            if (_fragment && _fragment.DamageOnTouch)
                _fragment.DamageOnTouch.OnHit -= HandleHit;
        }

        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
    }
}
