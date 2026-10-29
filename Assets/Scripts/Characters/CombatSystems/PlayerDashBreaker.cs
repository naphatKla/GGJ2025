using System.Collections.Generic;
using UnityEngine;

namespace Characters.CombatSystems
{
    /// <summary>
    /// สถานะ "Player กำลังพุ่งชน" ให้กระสุนศัตรู (EnemyProjectileSkillObject) เช็คว่าโดนชนแตกหรือยัง
    /// สกิลที่ต้องการให้ทำลายกระสุนได้ เรียก Begin ตอนเริ่ม และ End ตอนจบ
    /// </summary>
    public static class PlayerDashBreaker
    {
        private static readonly HashSet<object> Sources = new();
        private static Transform _player;

        public static bool IsActive => _player && Sources.Count > 0;
        public static Transform Player => _player;
        public static Vector2 Position => _player ? (Vector2)_player.position : Vector2.zero;

        public static void Begin(object source, Transform player)
        {
            if (source == null || !player) return;
            _player = player;
            Sources.Add(source);
        }

        public static void End(object source)
        {
            if (source == null) return;
            Sources.Remove(source);
            if (Sources.Count == 0) _player = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Sources.Clear();
            _player = null;
        }
    }
}
