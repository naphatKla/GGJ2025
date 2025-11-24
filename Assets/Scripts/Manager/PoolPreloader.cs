using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class PoolPreloader : MonoBehaviour
    {
        [Serializable]
        public class VfxPreloadEntry
        {
            [Tooltip("Prefab ของ VFX ที่จะเอาไปเข้าพูล")]
            public ParticleSystem prefab;

            [Tooltip("ถ้าเว้นว่าง = ใช้ prefab.name เป็น key")]
            public string overrideKey;

            [Min(0), Tooltip("จำนวน instance ที่จะ preload เข้าพูลไว้ตั้งแต่แรก")]
            public int prewarmCount = 8;
        }

        [Header("VFX Preload")] public List<VfxPreloadEntry> vfxEntries = new();

        [Header("Sound Preload (AudioSource Pools)")] [Tooltip("จำนวน SFX AudioSource ที่จะ prewarm (สร้างไว้ในพูล)")]
        public int sfxPrewarmCount = 16;

        [Tooltip("จำนวน UI AudioSource ที่จะ prewarm (สร้างไว้ในพูล)")]
        public int uiPrewarmCount = 8;

        private void Awake()
        {
            PreloadVfx();
            PreloadSoundPools();
        }

        private void PreloadVfx()
        {
            if (vfxEntries == null || vfxEntries.Count == 0)
                return;

            foreach (var entry in vfxEntries)
            {
                if (!entry.prefab || entry.prewarmCount <= 0)
                    continue;

                string key = string.IsNullOrEmpty(entry.overrideKey)
                    ? entry.prefab.name
                    : entry.overrideKey;

                // ให้ PoolingManager เป็นคนสร้างพูล + prewarm
                PoolingManager.Instance.Create<ParticleSystem>(
                    key,
                    PoolingGroupName.VFX,
                    createFunc: () =>
                    {
                        var vfx = Instantiate(entry.prefab);
                        vfx.gameObject.SetActive(false);
                        return vfx;
                    },
                    prewarmCount: entry.prewarmCount
                );
            }
        }

        private void PreloadSoundPools()
        {
            // เราให้ SoundManager เป็นคนจัดการสร้างพูล AudioSource อยู่แล้ว
            // ตรงนี้เลยให้ไปสั่ง "prewarm" ผ่าน SoundManager แทน

            if (SoundManager.SoundManager.Instance == null)
                return;

            if (sfxPrewarmCount > 0)
                SoundManager.SoundManager.Instance.PrewarmSFXSources(sfxPrewarmCount);

            if (uiPrewarmCount > 0)
                SoundManager.SoundManager.Instance.PrewarmUISources(uiPrewarmCount);
        }
    }
}