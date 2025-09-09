using System.Collections.Generic;
using Cameras;
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
        [SerializeField] private float focusFadeDuration = 0.1f;
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
                        Cinemachine2DCameraController.Instance.ShakeCamera(_playerData.AttackHitCameraShakeOption);
                    break;
                case FeedbackName.Character.CounterAttack:
                    Cinemachine2DCameraController.Instance.ShakeCamera(_playerData.CounterAttackHitCameraShakeOption);
                    break;
                case FeedbackName.Character.TakeDamage:
                    Cinemachine2DCameraController.Instance.ShakeCamera(_playerData.TakeDamageCameraShakeOption);
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
            // จบเอฟเฟกต์เก่าพร้อมคืนค่าก่อนเริ่มใหม่เสมอ (กันซ้อน)
            backDropTween?.Kill(true);

            var focusRenderer = focusBlackDrop ? (Renderer)focusBlackDrop : null;

            // --- เก็บเรนเดอร์ของ player/attacker รวม VFX จากพูลใต้ root ---
            var playerRoot = _player && _player.Body ? _player.Body.gameObject : null;
            var playerRenderers = CollectRenderersFromRoot(playerRoot, focusRenderer);
            var attackerRenderers = CollectRenderersFromRoot(attacker, focusRenderer);

            // --- สแนปช็อต + เรียงคงลำดับซ้อนทับเดิม ---
            int seq = 0;
            var playerSnaps = SnapshotRenderers(playerRenderers, ref seq);
            var attackerSnaps = SnapshotRenderers(attackerRenderers, ref seq);

            SortByOriginalStack(playerSnaps);
            SortByOriginalStack(attackerSnaps);

            // --- ย้ายทั้งหมดขึ้น focus layer ---
            int targetLayerId = focusBlackDrop ? focusBlackDrop.sortingLayerID : SortingLayer.NameToID("Default");
            int basePlayerOrder = focusOrderBase;
            int baseAttackerOrder = basePlayerOrder + playerSnaps.Count + focusOrderGap;

            ApplyToFocusLayer(playerSnaps, targetLayerId, basePlayerOrder);
            ApplyToFocusLayer(attackerSnaps, targetLayerId, baseAttackerOrder);

            // --- เตรียม fade (เก็บสถานะเดิมไว้คืนค่า) ---
            bool reverted = false;
            bool originalActive = false;
            Color originalColor = Color.black;

            if (focusBlackDrop)
            {
                originalActive = focusBlackDrop.gameObject.activeSelf;
                originalColor = focusBlackDrop.color;

                // เปิด + ตั้ง alpha เริ่มต้นเป็น 0 ก่อนค่อยเฟดเข้า
                focusBlackDrop.gameObject.SetActive(true);
                var c = focusBlackDrop.color;
                c.a = 0f;
                focusBlackDrop.color = c;
            }

            void RevertAll()
            {
                if (reverted) return;
                reverted = true;

                // คืนค่า renderer ทั้งหมด
                RevertRenderers(playerSnaps);
                RevertRenderers(attackerSnaps);

                // คืนสถานะ backdrop
                if (focusBlackDrop)
                {
                    // คืนสีเดิม (รวม alpha เดิม) และสถานะการแอคทีฟเดิม
                    focusBlackDrop.color = originalColor;
                    focusBlackDrop.gameObject.SetActive(originalActive);
                }
            }

            // --- สร้างลำดับเฟดเข้า → ค้าง → เฟดออก ---
            float hold = Mathf.Max(0.0001f, duration);
            var seqTween = DOTween.Sequence();

            if (focusBlackDrop)
            {
                float targetAlpha = (originalColor.a > 0.001f) ? originalColor.a : 1f;

                seqTween.Append(focusBlackDrop.DOFade(targetAlpha, focusFadeDuration)) // fade-in
                    .AppendInterval(hold) // hold
                    .Append(focusBlackDrop.DOFade(0f, focusFadeDuration)); // fade-out
            }
            else
            {
                // ไม่มี backdrop ก็แค่หน่วงเวลาเพื่อให้คืนค่าทีหลัง
                seqTween.AppendInterval(hold);
            }

            // ครบลำดับ → คืนค่า
            seqTween.OnComplete(RevertAll)
                .OnKill(RevertAll);

            backDropTween = seqTween;
        }
    }
}