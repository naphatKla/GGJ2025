using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CameraShakeSetting : MonoBehaviour
{
    public enum Level
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }
    
    [Serializable] public class Option
    {
        public Button button;
        public Image background;
        public TMP_Text text;
    }
    
    [Header("Options (0=Low, 1=Medium, 2=High)")]
    [SerializeField] private Option[] options = new Option[3];

    [Header("Default")]
    [SerializeField] private Level defaultLevel = Level.Medium;
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool disableSelectedButton = true;
    [SerializeField] private bool blockRepeatClick = true;

    [Header("Colors")]
    [SerializeField] private Color bgNormal = new Color(0.12f, 0.12f, 0.14f);
    [SerializeField] private Color bgSelected = new Color(0.00f, 0.62f, 1.00f);
    [SerializeField] private Color textNormal = Color.white;
    [SerializeField] private Color textSelected = Color.black;
    
    [Header("Events")]
    public UnityEvent<Level> onLevelChanged;

    public Level Current { get; private set; }
    private UnityAction[] _cached;
    
    private void Awake()
    {
        _cached = new UnityAction[options.Length];

        for (int i = 0; i < options.Length; i++)
        {
            var idx = i;
            var opt = options[idx];
            if (opt?.button == null) continue;

            // bind onClick
            _cached[idx] = () => Apply((Level)idx);
            opt.button.onClick.AddListener(_cached[idx]);

            // auto-fill background
            if (opt.background == null)
                opt.background = opt.button.targetGraphic as Image ?? opt.button.GetComponent<Image>();

            // auto-fill TMP label
            if (opt.text == null)
                opt.text = opt.button.GetComponentInChildren<TMP_Text>(true);
        }
    }
    
    private void OnEnable()
    {
        if (applyOnEnable) Apply(defaultLevel, false); // เปลี่ยนสีอัตโนมัติ + ล็อกปุ่มเริ่มต้น
        else UpdateVisuals(Current);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i]?.button == null || _cached?[i] == null) continue;
            options[i].button.onClick.RemoveListener(_cached[i]);
        }
    }
    
    public void OnClickLow() => Apply(Level.Low);
    public void OnClickMedium() => Apply(Level.Medium);
    public void OnClickHigh() => Apply(Level.High);

    private void Apply(Level level, bool fromUser = true)
    {
        if (blockRepeatClick && fromUser && level == Current) return;
        Current = level;
        
        switch (level)
        {
            case Level.Low: 
                ApplyLow(); 
                break;
            case Level.Medium: 
                ApplyMedium(); 
                break;
            case Level.High: 
                ApplyHigh();   
                break;
        }
        
        if (disableSelectedButton)
        {
            for (int i = 0; i < options.Length; i++)
                if (options[i]?.button) options[i].button.interactable = i != (int)level;
        }

        UpdateVisuals(level);
        onLevelChanged?.Invoke(level);
    }
    
    private void UpdateVisuals(Level level)
    {
        for (int i = 0; i < options.Length; i++)
        {
            bool selected = i == (int)level;
            var opt = options[i];
            if (opt == null) continue;

            if (opt.background) 
                opt.background.color = selected ? bgSelected : bgNormal;
            if (opt.text) 
                opt.text.color = selected ? textSelected : textNormal;
        }
    }

    protected virtual void ApplyLow()
    { 
        /* CameraShaker.SetPreset(0.2f, 8f, 0.12f); */ 
         Debug.Log("ApplyLow");
    }

    protected virtual void ApplyMedium()
    { 
        /* CameraShaker.SetPreset(0.35f, 10f, 0.15f); */ 
        Debug.Log("ApplyMedium");
    }

    protected virtual void ApplyHigh()
    {
        /* CameraShaker.SetPreset(0.55f, 12f, 0.20f); */
        Debug.Log("ApplyHigh");
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (options == null || options.Length != 3)
            options = new Option[3];
    }
#endif
}
