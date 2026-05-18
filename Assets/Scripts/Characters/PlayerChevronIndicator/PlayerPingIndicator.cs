using UnityEngine;

/// <summary>
/// Multi-ring ping indicator - วงแหวนขยาย-จาง ต่อเนื่องสลับกัน
/// วางเป็น child ของ Player prefab จะสร้าง ring ลูกอัตโนมัติตาม ringCount
/// </summary>
public class PlayerPingIndicator : MonoBehaviour
{
    [Header("Sprite & Material")]
    [Tooltip("Ring sprite (เช่น ring-icon.png)")]
    [SerializeField] private Sprite ringSprite;

    [Tooltip("HDR material สำหรับ neon glow (เช่น Mat_Chevron_Neon)")]
    [SerializeField] private Material ringMaterial;

    [Tooltip("Sorting Order ของ ring (ควรอยู่ระหว่าง enemy กับ player)")]
    [SerializeField] private int sortingOrder = 5;

    [Header("Ping Settings")]
    [Tooltip("จำนวน ring (เปลี่ยนแล้วต้อง restart play)")]
    [SerializeField] private int ringCount = 2;

    [Tooltip("เวลา 1 รอบของ ring แต่ละตัว (วินาที)")]
    [SerializeField] private float cycleDuration = 1.8f;

    [Tooltip("ขนาดเริ่มต้นของ ring (multiplier)")]
    [SerializeField] private float startScale = 0.4f;

    [Tooltip("ขนาดสุดท้ายของ ring (multiplier)")]
    [SerializeField] private float endScale = 1.4f;

    [Tooltip("สัดส่วนของรอบที่ ring มองเห็น (0.7 = เห็น 70% พัก 30%)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float visibleFraction = 0.7f;

    [Header("Rotation")]
    [Tooltip("ให้ ring คงทิศ ไม่หมุนตาม player")]
    [SerializeField] private bool keepUpright = true;

    private Transform parentTransform;
    private SpriteRenderer[] rings;

    private void Awake()
    {
        parentTransform = transform.parent;
        if (parentTransform == null)
        {
            Debug.LogWarning("[PlayerPingIndicator] ไม่พบ parent — ควรวางเป็น child ของ Player");
        }

        // สร้าง ring ลูกอัตโนมัติตามจำนวนที่กำหนด
        rings = new SpriteRenderer[ringCount];
        for (int i = 0; i < ringCount; i++)
        {
            GameObject ringObj = new GameObject($"Ring_{i}");
            ringObj.transform.SetParent(transform);
            ringObj.transform.localPosition = Vector3.zero;
            ringObj.transform.localRotation = Quaternion.identity;
            ringObj.transform.localScale = Vector3.one * startScale;

            SpriteRenderer sr = ringObj.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite;
            if (ringMaterial != null) sr.material = ringMaterial;
            sr.sortingOrder = sortingOrder;

            rings[i] = sr;
        }
    }

    private void LateUpdate()
    {
        if (parentTransform == null) return;

        // ติดตำแหน่ง parent
        transform.position = parentTransform.position;
        if (keepUpright) transform.rotation = Quaternion.identity;

        // animate แต่ละ ring แบบ stagger
        for (int i = 0; i < rings.Length; i++)
        {
            // offset แต่ละ ring แบ่งเท่าๆ กันใน cycle
            float offset = (i / (float)ringCount) * cycleDuration;
            float t = ((Time.time + offset) % cycleDuration) / cycleDuration; // 0..1

            SpriteRenderer sr = rings[i];

            if (t < visibleFraction)
            {
                // อยู่ในช่วงมองเห็น: ขยาย scale + จาง alpha
                float p = t / visibleFraction; // 0..1
                float scale = Mathf.Lerp(startScale, endScale, p);
                float alpha = 1f - p;

                sr.transform.localScale = Vector3.one * scale;
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
                sr.enabled = true;
            }
            else
            {
                // ช่วงพักก่อนเริ่ม cycle ใหม่
                sr.enabled = false;
            }
        }
    }
}