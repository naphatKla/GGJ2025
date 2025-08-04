using System;
using System.Collections.Generic;
using Characters.SkillSystems.SkillRuntimes;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Characters.SkillSystems
{
    public class PlayerSkillSystem : SkillSystem
    {
        [SerializeField, Tooltip("Enable skill input buffering")]
        private bool useInputBuffering = true;

        [SerializeField, Tooltip("Buffer window duration (sec)")]
        private float bufferWindow = 0.25f;

        private readonly Dictionary<SkillType, Action> activeBufferCallbacks = new();

        public override void PerformSkill(SkillType type)
        {
            if (!owner || !canUseSkills)
                return;

            var runtime = GetRuntime(type);

            if (useInputBuffering && runtime && runtime.IsCooldown)
            {
                runtime.ClearCooldownReadyCallback();
                var capturedRuntime = runtime;

                Action bufferAction = () =>
                {
                    if (!canUseSkills || GetRuntime(type) != capturedRuntime)
                        return;

                    base.PerformSkill(type);
                    activeBufferCallbacks.Remove(type);
                };

                activeBufferCallbacks[type] = bufferAction;
                runtime.RegisterCooldownReadyCallback(bufferAction);

                // <-- Add timeout cancellation
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(bufferWindow), DelayType.DeltaTime, PlayerLoopTiming.Update);

                    if (activeBufferCallbacks.TryGetValue(type, out var cb) && cb == bufferAction)
                    {
                        runtime.ClearCooldownReadyCallback();
                        activeBufferCallbacks.Remove(type);
                    }
                });

                return;
            }

            base.PerformSkill(type);
        }


        public override void ResetSkillSystem()
        {
            foreach (var pair in activeBufferCallbacks)
            {
                var runtime = GetRuntime(pair.Key);
                runtime?.ClearCooldownReadyCallback();
            }
            activeBufferCallbacks.Clear();
            base.ResetSkillSystem();
        }

        private BaseSkillRuntime GetRuntime(SkillType type)
        {
            return type switch
            {
                SkillType.PrimarySkill => GetSkillRuntimeOrDefault(primarySkillData),
                SkillType.SecondarySkill => GetSkillRuntimeOrDefault(secondarySkillData),
                _ => null,
            };
        }
    }
}