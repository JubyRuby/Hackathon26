#nullable enable

using System.Collections.Generic;
using ElevenLabs.Agents;
using ElevenLabs.Protocol;
using UnityEngine;

namespace ElevenLabs.Agents.Samples.GettingStarted
{
    public class TalkingBox : MonoBehaviour
    {
        [Header("Agent Config (assign per cube)")]
        [SerializeField]
        private TalkingBoxAgentConfig configAsset;

        [Header("Dynamic Variables")]
        [SerializeField]
        private string color = "yellow";

        [Header("Overrides")]
        [SerializeField]
        [TextArea(2, 4)]
        private string firstMessage = "Hi there! I'm a {{color}} cube.";

        [SerializeField]
        private string voiceIdOverride = "";

        [Header("Visual Bob Settings")]
        [SerializeField]
        private float peakOffsetY = 0.3f;

        [SerializeField]
        private float volumeSensitivity = 4.0f;

        [SerializeField]
        private float smoothingTau = 0.08f;

        [Header("Audio")]
        [SerializeField]
        private AudioSource? audioSource;

        private static TalkingBox? currentlyActive;
        private Vector3 baselinePosition;
        private Conversation? activeConversation;
        private float smoothedVolume;

        private void Awake()
        {
            baselinePosition = transform.localPosition;

            bool hasTrigger = false;
            foreach (Collider c in GetComponents<Collider>())
                if (c.isTrigger)
                    hasTrigger = true;

            if (!hasTrigger)
                Debug.LogWarning($"[TalkingBox:{name}] Add a SphereCollider with IsTrigger enabled.");

            if (audioSource == null)
                Debug.LogWarning($"[TalkingBox:{name}] No AudioSource assigned.");
        }

        private void Update()
        {
            float rms = activeConversation?.GetOutputVolume() ?? 0f;
            float target = Mathf.Clamp01(rms * volumeSensitivity);
            float alpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, smoothingTau));
            smoothedVolume = Mathf.Lerp(smoothedVolume, target, alpha);

            transform.localPosition =
                baselinePosition + Vector3.up * (smoothedVolume * peakOffsetY);
        }

        private void OnTriggerEnter(Collider other) => _ = StartTalkingAsync();
        private void OnTriggerExit(Collider other) => _ = StopTalkingAsync();

        private ConversationConfigOverride? BuildOverrides()
        {
            bool hasFirstMessage = !string.IsNullOrWhiteSpace(firstMessage);
            bool hasVoiceId = !string.IsNullOrWhiteSpace(voiceIdOverride);

            if (!hasFirstMessage && !hasVoiceId)
                return null;

            return new ConversationConfigOverride
            {
                Agent = hasFirstMessage
                    ? new ConversationConfigOverrideAgent { FirstMessage = firstMessage }
                    : null,
                Tts = hasVoiceId
                    ? new ConversationConfigOverrideTts { VoiceId = voiceIdOverride }
                    : null,
            };
        }

        private async Awaitable StartTalkingAsync()
        {
            if (currentlyActive == this)
                return;

            if (currentlyActive != null)
                await currentlyActive.StopTalkingAsync();

            currentlyActive = this;

            TalkingBoxAgentConfig config = configAsset;
            if (config == null || string.IsNullOrWhiteSpace(config.AgentId))
            {
                Debug.LogError("[TalkingBox] Missing agent config asset or empty AgentId.");
                currentlyActive = null;
                return;
            }

            var options = new ConversationOptions
            {
                AgentId = config.AgentId,
                DynamicVariables = new Dictionary<string, object>
                {
                    ["color"] = color,
                },
                Overrides = BuildOverrides(),
                OutputAudioSource = audioSource,
            };

            activeConversation = await Conversation.StartSessionAsync(options);
        }

        private async Awaitable StopTalkingAsync()
        {
            if (activeConversation != null)
            {
                await activeConversation.EndSession();
                activeConversation = null;
            }

            currentlyActive = null;
        }
    }
}
