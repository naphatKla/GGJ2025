using Characters.Controllers;
using Characters.SO.StatusEffectSO;

namespace Characters.StatusEffectSystems.StatusEffects
{
    public abstract class BaseStatusEffect
    {
        protected StatusEffectName effectName;
        protected float duration; // = Max duration
        protected float currentDuration;
        protected int level;
        protected bool isDebuff;

        // === ใหม่: เก็บ SO ต้นทาง ไว้ให้ UI ดึง Icon/ชื่อ ===
        public BaseStatusEffectDataSo DataSo { get; protected set; }

        public StatusEffectName EffectName => effectName;
        public float MaxDuration => duration; // alias ให้อ่านง่าย

        public float CurrentDuration
        {
            get => currentDuration;
            set => currentDuration = value;
        }

        public int Level => level;
        public bool IsDebuff => isDebuff;
        public bool IsDone => currentDuration <= 0f;

        public abstract void AssignEffectData(BaseStatusEffectDataSo data, float duration = 0);
        public abstract void OnStart(BaseController owner);
        public abstract void OnUpdate(BaseController owner, float deltaTime);
        public abstract void OnExit(BaseController owner);

        public void ClearThisEffect() => currentDuration = 0;
    }

    public abstract class BaseStatusEffect<T> : BaseStatusEffect where T : BaseStatusEffectDataSo
    {
        protected T effectData;

        public override void AssignEffectData(BaseStatusEffectDataSo data, float duration = 0)
        {
            effectData = data as T;
            DataSo = data; // ★ เก็บอ้างอิง Data ไว้
            effectName = effectData.EffectName;
            this.duration = duration; // ค่าที่คำนวณมาแล้วจาก Manager
            currentDuration = duration;
            level = effectData.Level;
            isDebuff = effectData.IsDebuff;
        }
    }
}