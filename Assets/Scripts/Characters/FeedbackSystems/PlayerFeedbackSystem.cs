using System.Collections.Generic;
using Characters.Controllers;
using Characters.SO.CharacterDataSO;
using DG.Tweening;
using Feedbacks;
using UnityEngine;

namespace Characters.FeedbackSystems
{
    public class PlayerFeedbackSystem : FeedbackSystem
    {
        [SerializeField] private SpriteRenderer focusBlackDrop;
        [SerializeField] private int focusOrderBase = 0; // จุดเริ่ม order ของฝั่ง player
        [SerializeField] private int focusOrderGap = 4; // เว้นช่องไฟก่อนเริ่ม attacker
        private PlayerController _player;
        private PlayerDataSo _playerData;

        public override void AssignData(BaseController owner)
        {
            _player = owner as PlayerController;
            _playerData = owner.CharacterData as PlayerDataSo;
            base.AssignData(owner);
        }

        public override void PlayFeedback(string feedbackName)
        {
            if (!feedbackMap.ContainsKey(feedbackName)) return;
            if (ignoreFeedbackList.Contains(feedbackName)) return;

            switch (feedbackName)
            {
                case FeedbackName.Character.AttackHit:
                    if (!IsFeedbackPlaying(FeedbackName.Character.CounterAttack))
                        _player.CameraController.ShakeCamera(_playerData.AttackHitCameraShakeOption);
                    break;
                case FeedbackName.Character.CounterAttack:
                    _player.CameraController.ShakeCamera(_playerData.CounterAttackHitCameraShakeOption);
                    break;
                case FeedbackName.Character.TakeDamage:
                    _player.CameraController.ShakeCamera(_playerData.TakeDamageCameraShakeOption);
                    break;
            }

            base.PlayFeedback(feedbackName);
        }

        // ============================================ Focus Backdrop ===============================================
        private Tween backDropTween;
        private struct RendererSnapshot
        {
            public Renderer renderer;
            public int layerId; // sortingLayerID เดิม
            public string layerName; // ไว้ดู/ดีบัก
            public int order; // sortingOrder เดิม
            public int layerValue; // ใช้เทียบความสูงของเลเยอร์
            public int seq; // tie-breaker ให้เรียงเสถียร
        }

// ===== Helpers =====
        private static void AddUniqueRenderers(HashSet<Renderer> set, Renderer[] arr, Renderer skip = null)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                var r = arr[i];
                if (!r || r == skip) continue;
                set.Add(r);
            }
        }

// ดึง Renderer ของฝั่งหนึ่ง (player/attacker)
// - เก็บ Renderer ใต้ root โดยตรง
// - หา VFXPlayerPooling ใต้ root → ตามไป CurrentVFXInstance → เก็บ Renderer ใต้ instance
        private List<Renderer> CollectRenderersFromRoot(GameObject root, Renderer skip)
        {
            var result = new HashSet<Renderer>();

            if (root)
            {
                // 1) Renderer ใต้ root
                var childRenderers = root.GetComponentsInChildren<Renderer>(true);
                AddUniqueRenderers(result, childRenderers, skip);

                // 2) VFX จากพูลที่แปะอยู่ใต้ root
                var pools = root.GetComponentsInChildren<VFXPlayerPooling>(true);
                if (pools != null)
                {
                    for (int i = 0; i < pools.Length; i++)
                    {
                        var inst = pools[i]?.CurrentVFXInstance;
                        if (!inst) continue;
                        var vfxRenderers = inst.GetComponentsInChildren<Renderer>(true);
                        AddUniqueRenderers(result, vfxRenderers, skip);
                    }
                }
            }

            return new List<Renderer>(result);
        }

        private static List<RendererSnapshot> SnapshotRenderers(List<Renderer> renderers, ref int runningSeq)
        {
            var list = new List<RendererSnapshot>(renderers.Count);
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (!r) continue;

                int id = r.sortingLayerID;
                list.Add(new RendererSnapshot
                {
                    renderer = r,
                    layerId = id,
                    layerName = SortingLayer.IDToName(id),
                    order = r.sortingOrder,
                    layerValue = SortingLayer.GetLayerValueFromID(id),
                    seq = runningSeq++
                });
            }

            return list;
        }

// คงลำดับซ้อนทับเดิม: เลเยอร์ล่าง→บน แล้วตาม order
        private static void SortByOriginalStack(List<RendererSnapshot> snaps)
        {
            snaps.Sort((a, b) =>
            {
                int lv = a.layerValue.CompareTo(b.layerValue);
                if (lv != 0) return lv;
                int so = a.order.CompareTo(b.order);
                if (so != 0) return so;
                return a.seq.CompareTo(b.seq);
            });
        }

        private static void ApplyToFocusLayer(List<RendererSnapshot> snaps, int targetLayerId, int baseOrder)
        {
            for (int i = 0; i < snaps.Count; i++)
            {
                var r = snaps[i].renderer;
                if (!r) continue;
                r.sortingLayerID = targetLayerId;
                r.sortingOrder = baseOrder + i;
            }
        }

        private static void RevertRenderers(List<RendererSnapshot> snaps)
        {
            for (int i = 0; i < snaps.Count; i++)
            {
                var s = snaps[i];
                if (!s.renderer) continue;
                s.renderer.sortingLayerID = s.layerId;
                s.renderer.sortingOrder = s.order;
            }
        }

// ===== Main =====
        public void OpenFocusBlackDropOnHit(float duration, GameObject attacker)
        {
            // จบเอฟเฟกต์เก่าพร้อมคืนค่าก่อนเริ่มใหม่เสมอ
            backDropTween?.Kill(true);

            if (focusBlackDrop) focusBlackDrop.gameObject.SetActive(true);

            var focusRenderer = focusBlackDrop ? (Renderer)focusBlackDrop : null;

            // ฝั่ง player
            var playerRoot = _player && _player.Body ? _player.Body.gameObject : null;
            var playerRenderers = CollectRenderersFromRoot(playerRoot, focusRenderer);

            // ฝั่ง attacker
            var attackerRenderers = CollectRenderersFromRoot(attacker, focusRenderer);

            // สแนปช็อต + เรียง “คงซ้อนทับเดิม”
            int seq = 0;
            var playerSnaps = SnapshotRenderers(playerRenderers, ref seq);
            var attackerSnaps = SnapshotRenderers(attackerRenderers, ref seq);

            SortByOriginalStack(playerSnaps);
            SortByOriginalStack(attackerSnaps);

            // ย้ายไป focus layer: player ก่อน แล้ว attacker ทับด้านบน
            int targetLayerId = focusBlackDrop ? focusBlackDrop.sortingLayerID : SortingLayer.NameToID("Default");

            int basePlayerOrder = focusOrderBase;
            int baseAttackerOrder = basePlayerOrder + playerSnaps.Count + focusOrderGap;

            ApplyToFocusLayer(playerSnaps, targetLayerId, basePlayerOrder);
            ApplyToFocusLayer(attackerSnaps, targetLayerId, baseAttackerOrder);

            // คืนค่าเมื่อครบเวลา หรือโดน Kill
            bool reverted = false;

            void RevertAll()
            {
                if (reverted) return;
                reverted = true;

                RevertRenderers(playerSnaps);
                RevertRenderers(attackerSnaps);

                if (focusBlackDrop) focusBlackDrop.gameObject.SetActive(false);
            }

            backDropTween = DOVirtual.DelayedCall(Mathf.Max(0.0001f, duration), RevertAll)
                .OnKill(RevertAll);
        }
    }
}