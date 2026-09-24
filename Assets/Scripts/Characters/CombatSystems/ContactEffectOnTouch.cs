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

        [InfoBox("ศัตรูที่มี component นี้จะทำดาเมจเมื่อชนได้ตลอดเวลา ไม่ต้องมีสกิลพุ่งชนมาเปิดให้\n"
                 + "ดาเมจต่อ hit = Base Damage + (Damage Multiplier% × ค่า Damage ของศัตรูตัวนี้)\n"
                 + "ใช้กับ Bright2 Fragment (Piece of Mine): ชน → ดาเมจ + สโลว์ → หายไป")]
        [Title("Damage")]
        [Tooltip("ดาเมจคงที่ บวกเพิ่มจากส่วนที่คิดเป็น %\n0 = ใช้แค่ส่วน %")]
        [SerializeField] private float baseDamage;

        [Unit(Units.Percent)]
        [Tooltip("% ของค่า Damage ของศัตรูตัวนี้ (จาก Character Data)\n100 = เท่ากับค่า Damage พอดี")]
        [SerializeField] private float damageMultiplier = 100f;

        [MinValue(0.01f)]
        [Tooltip("ถ้ายังชนค้างอยู่ จะโดนซ้ำได้กี่ครั้งต่อวินาที (ต่อเป้าหมาย)\n1 = โดน 1 ครั้งต่อวินาที")]
        [SerializeField] private float hitPerSec = 1f;

        [Title("On Contact")]
        [Tooltip("Status effect ที่ใส่ให้เป้าหมายทุกครั้งที่ชนโดน\nเช่น LV1_MovementSlow_Effect + Override Duration 5 = สโลว์ 5 วินาที")]
        [SerializeField] private List<StatusEffectDataPayload> contactStatusEffects = new();

        [Tooltip("เปิด = effect และการหายไปเกิดเฉพาะตอนชน player\n(ดาเมจยังโดนทุกอย่างที่อยู่ใน Target Layer ของ DamageOnTouch)")]
        [SerializeField] private bool playerOnly = true;

        [Tooltip("เปิด = ชนโดนครั้งแรกแล้วตายทันที (ผ่านระบบตายปกติ pool / summon จึงนับถูก)\nปิด = ชนแล้วยังไล่ต่อได้")]
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
