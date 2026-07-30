using System;
using BepInEx.Configuration;

namespace LOM.MessageSpeed
{
    internal sealed class MessageSpeedConfig
    {
        private const float MinimumMultiplier = 0.1f;
        private const float MaximumMultiplier = 10.0f;

        internal MessageSpeedConfig(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Enable message typing speed adjustment.");

            SpeedMultiplier = config.Bind(
                "Message",
                "SpeedMultiplier",
                1.5f,
                new ConfigDescription(
                    "Multiplier applied to normal dialogue characters per second.",
                    new AcceptableValueRange<float>(MinimumMultiplier, MaximumMultiplier)));
        }

        internal ConfigEntry<bool> Enabled { get; private set; }

        internal ConfigEntry<float> SpeedMultiplier { get; private set; }

        internal float EffectiveMultiplier
        {
            get
            {
                float value = SpeedMultiplier.Value;
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                {
                    return 1.0f;
                }

                return Math.Min(MaximumMultiplier, Math.Max(MinimumMultiplier, value));
            }
        }
    }
}
