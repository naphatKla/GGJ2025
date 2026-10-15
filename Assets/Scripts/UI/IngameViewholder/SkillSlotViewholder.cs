using System.Collections.Generic;
using System.Linq;
using Characters.SO.SkillDataSo;
using Manager.SoundManager;
using PixelUI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.IngameViewholder
{
    public class SkillSlotViewholder : SerializedMonoBehaviour
    {
        [SerializeField] public Image skillIcon;
        [SerializeField] public ValueBar valueBar;
        [SerializeField] public TMP_Text cooldownText;
        [SerializeField] public Image skillframe;
        [SerializeField] public TMP_Text skillslotLv;
        [SerializeField] public GameObject skillLvBanner;
        [SerializeField] public ParticleSystem cooldownFinishParticle;
        
        [SerializeField] private Dictionary<float, GameObject> VFXPrefabs;
        private Dictionary<float, GameObject> OverloopVFX;

        private List<float> _sortedKeys;
        const float epsilon = 0.0001f;
        private float? _currentKey = null;
        
        [Title("Locked (ยังไม่ปลดจาก Upgrade)")]
        [Tooltip("สีที่คูณทับทุก Graphic ในสล็อตตอนยังไม่ปลด ยิ่งต่ำยิ่งมืด")]
        [SerializeField] private Color lockedTint = new Color(0.3f, 0.3f, 0.3f, 1f);

        private Graphic[] _graphics;
        private Color[] _originalColors;
        private bool _isLocked;

        private void Awake()
        {
            BuildSortedKeys();
        }

        public void SetLocked(bool locked)
        {
            if (_graphics == null)
            {
                if (!locked) return; // ยังไม่เคยมืด ไม่ต้องคืนสี
                _graphics = GetComponentsInChildren<Graphic>(true);
                _originalColors = _graphics.Select(g => g.color).ToArray();
            }
            if (_isLocked == locked) return;
            _isLocked = locked;

            for (int i = 0; i < _graphics.Length; i++)
            {
                if (!_graphics[i]) continue;
                _graphics[i].color = locked ? _originalColors[i] * lockedTint : _originalColors[i];
            }
        }

        public void UpdateLevelText(float level, BaseSkillDataSo skill)
        {
            skillLvBanner.SetActive(true);
            skillslotLv.text = $"LV.<color=#FFD700>{level:F0}</color>";
            cooldownText.text = "";
            skillIcon.sprite = skill.SkillIcon;
        }
        
        public void PlayCooldownFinishFeedback()
        {
            cooldownFinishParticle.Play();
        }
        
        public void ResetSkillSlot()
        {
            cooldownText.text = "";
            valueBar.CurrentValue = 0;
        }
        
        private void BuildSortedKeys()
        {
            _sortedKeys = VFXPrefabs?.Keys?.Distinct().OrderBy(k => k).ToList() ?? new List<float>();
        }

        public void OverloopFeedback(float multiply)
        {
            if (OverloopVFX == null) OverloopVFX = new Dictionary<float, GameObject>();
            if (_sortedKeys == null || _sortedKeys.Count == 0) return;
            if (multiply <= 1f + epsilon)
            {
                DeactivateAllVFX();
                return;
            }
            
            var selectedKey = FindKeyByRange(multiply);
            foreach (var kvp in OverloopVFX)
            {
                if (Mathf.Abs(kvp.Key - selectedKey) > epsilon && kvp.Value != null)
                    kvp.Value.SetActive(false);
            }
            
            //POOL
            if (OverloopVFX.TryGetValue(selectedKey, out var vfxObj))
            {
                if (vfxObj != null) vfxObj.SetActive(true);
                return;
            }
            //FIRST TIME
            if (VFXPrefabs.TryGetValue(selectedKey, out var prefab) && prefab != null)
            {
                var newVFX = Instantiate(prefab, transform);
                newVFX.name = $"OverloopVFX_{selectedKey}";
                newVFX.SetActive(true);
                OverloopVFX[selectedKey] = newVFX;
            }
        }
        
        private void DeactivateAllVFX()
        {
            if (OverloopVFX == null) return;
            foreach (var kvp in OverloopVFX)
            {
                if (kvp.Value != null) kvp.Value.SetActive(false);
            }
        }
        
        private float FindKeyByRange(float n)
        {
            int idx = _sortedKeys.BinarySearch(n);
            
            //first key
            if (idx >= 0) return _sortedKeys[idx];
            
            //-n
            idx = ~idx;
            
            //n >= _sortedKeys[0]
            if (idx == 0) return _sortedKeys[0];
            
            //n range
            return _sortedKeys[Mathf.Min(idx - 1, _sortedKeys.Count - 1)];
        }
    }
}
