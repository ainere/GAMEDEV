using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LaserCorridor
{
    // The HUD reads current run state. Pickup eligibility and all gameplay clocks stay in RunModel.
    public sealed class CorridorHud : MonoBehaviour
    {
        public const string TimeCaption = "Time remaining";
        public const string ExitWarning = "Blue exit lasers kill\nuntil time runs out.";
        public static string ReadyInstructions(float duration) => "Survive " + duration.ToString("0") +
            " seconds.\nThen reach the exit.\n\nSteady lights: healing, shield or slower lasers.\nFlickering lights: shock or slower movement.";

        public TMP_Text health, time, status, notice, overlayTitle, overlayBody;
        public TMP_Text timerCaption, objective, sharedCooldown, volumeValue, overlayKicker;
        public TMP_Text resetCountdown, resultCaption, pauseIndicator;
        public TMP_Text[] effectLabels, effectTimes, rechargeLabels, rechargeTimes;
        public UnityEngine.UI.Image healthFill, hitFlash, timerProgress, noticeAccent, overlayAccent;
        public UnityEngine.UI.RawImage noiseRaw, scanlineRaw;
        public UnityEngine.UI.Image[] effectRings, rechargeRings;
        public GameObject[] effectIcons, rechargeIcons;
        public GameObject overlay, volumeGroup, readyGroup, pauseGroup, resultGroup;
        public GameObject noticeGroup, effectsGroup, rechargeGroup;
        // Legacy serialized fields remain for migration/QA. The builder constructs no pause menu,
        // volume control or recharge display and leaves their references null or arrays empty.
        public UnityEngine.UI.Slider volume;
        public UnityEngine.UI.Button startButton, resumeButton, restartButton;

        static readonly Color Ivory = new Color(.94f, .955f, .94f);
        static readonly Color Muted = new Color(.75f, .79f, .79f);
        static readonly Color Cyan = new Color(.44f, .835f, .9f);
        static readonly Color Red = new Color(1, .396f, .357f);
        RunController run;
        DepthOfField depth;
        RectTransform effectsRect;
        float hitAt = -10, shieldAt = -10, deadAt = -10, feedClock;
        Color feedbackColor;
        int lastOverlayMode = -1;

        public void Initialize(RunController owner)
        {
            if (run && run.Model != null) run.Model.Feedback -= Feedback;
            run = owner;
            run.Model.Feedback += Feedback;
            var sceneVolume = FindAnyObjectByType<Volume>();
            if (sceneVolume && sceneVolume.profile) sceneVolume.profile.TryGet(out depth);
            if (depth) depth.active = false;
            if (effectsGroup) effectsRect = effectsGroup.GetComponent<RectTransform>();
            feedbackColor = Cyan;
            feedClock = Time.unscaledTime;
            lastOverlayMode = -1;
        }

        void OnDestroy()
        {
            if (run && run.Model != null) run.Model.Feedback -= Feedback;
        }

        public void BeginRun()
        {
            if (!run || run.Model == null || run.Model.State != RunState.Ready || !overlay.activeInHierarchy) return;
            run.StartRun();
            Refresh();
        }

        // Kept for legacy callbacks; there is no constructed Resume control in the CCTV HUD.
        public void ResumeRun()
        {
            if (!run || run.Model == null || !run.Model.Paused || !resumeButton || !resumeButton.gameObject.activeInHierarchy) return;
            run.Model.TogglePause();
            Refresh();
        }

        public void RestartRun()
        {
            if (!run || run.Model == null || !overlay.activeInHierarchy || !restartButton.gameObject.activeInHierarchy) return;
            if (run.Model.State != RunState.Dead && run.Model.State != RunState.Won) return;
            run.Restart();
            Refresh();
        }

        void Feedback(string kind)
        {
            if (kind == "damage" || kind == "shock")
            {
                hitAt = Time.unscaledTime;
                run.securityCamera.HitShake();
            }
            if (kind == "shield") shieldAt = Time.unscaledTime;
            if (kind == "death") deadAt = Time.unscaledTime;
            feedbackColor = kind == "damage" || kind == "shock" || kind == "trap" ? Red : Cyan;
        }

        public void Refresh()
        {
            if (!run || run.Model == null) return;
            var m = run.Model;
            var s = m.settings;
            bool live = m.State == RunState.Running;
            bool open = m.State == RunState.ExitOpen || m.State == RunState.Won;
            bool recording = !m.Paused && (live || m.State == RunState.ExitOpen);
            if (m.State == RunState.Ready) hitAt = shieldAt = deadAt = -10;
            if (!m.Paused) feedClock = Time.unscaledTime;
            float hit = Mathf.Clamp01(1 - (feedClock - hitAt) / .25f);
            float shield = Mathf.Clamp01(1 - (feedClock - shieldAt) / .25f);
            hitFlash.color = hit > 0 ? new Color(Red.r, Red.g, Red.b, hit * .5f) : new Color(Cyan.r, Cyan.g, Cyan.b, shield * .35f);

            health.text = m.Health.ToString("000") + " <size=22>/ " + s.maximumHealth + " HP</size>";
            healthFill.fillAmount = m.Health / (float)Mathf.Max(1, s.maximumHealth);
            healthFill.color = hit > 0 ? Color.white : m.Health <= 25 ? Red : Ivory;
            time.text = open ? "OPEN" : Mathf.CeilToInt(m.Remaining).ToString("000") + " <size=28>s</size>";
            time.fontSize = open ? 54 : 64;
            time.color = live && m.Remaining <= 10 ? Red : Ivory;
            time.transform.localScale = Vector3.one * (live && m.Remaining <= 10 ? 1 + .015f * Mathf.Pow(1 - m.Elapsed % 1, 4) : 1);
            if (timerCaption) timerCaption.text = open ? "Security offline" : TimeCaption;
            if (objective) objective.text = m.State == RunState.Won ? "Exit reached" : open ? "Reach the exit" : m.State == RunState.Dead ? "Feed ended" : "Exit locked";
            if (timerProgress)
            {
                timerProgress.fillAmount = open ? 1 : Mathf.Clamp01(m.Elapsed / Mathf.Max(.001f, s.duration));
                timerProgress.color = live && m.Remaining <= 10 ? Red : Ivory;
            }
            string feedState = m.Paused ? "Paused" : m.State == RunState.Dead ? "Signal lost" : m.State == RunState.Ready ? "Standby" : m.State == RunState.Won ? "Complete" : "Live";
            string rec = recording ? (Mathf.FloorToInt(feedClock * 2) % 2 == 0 ? "   <color=#FF655B>● REC</color>" : "   <color=#FF655B00>● REC</color>") : "";
            status.text = "CAM 01" + rec + "\n" + TimeSpan.FromSeconds(m.Elapsed).ToString(@"hh\:mm\:ss") + "  /  " + feedState;
            if (pauseIndicator) pauseIndicator.gameObject.SetActive(m.Paused);

            // A paused feed retains the last panel popup; the model's notice clock is frozen too.
            bool toast = live && m.Elapsed < m.NoticeUntil && !string.IsNullOrEmpty(m.Notice);
            notice.text = toast ? m.Notice.Replace(" · ", "  /  ") : "";
            if (noticeGroup) noticeGroup.SetActive(toast);
            if (noticeAccent) noticeAccent.color = feedbackColor;
            RefreshEffects(live);

            int mode = m.State == RunState.Ready ? 1 : m.State == RunState.Dead && run.player.DeathTime >= 1.2f ? 3 : m.State == RunState.Won ? 4 : 0;
            RefreshOverlay(mode);
            // CCTV text stays over the actual feed. Pause never dims or blurs the camera.
            if (depth) depth.active = false;
            float loss = m.State == RunState.Dead ? Mathf.Clamp01(1 - (feedClock - deadAt) / .4f) : 0;
            noiseRaw.color = new Color(1, 1, 1, .16f + loss * .3f);
            noiseRaw.uvRect = new Rect(feedClock % 1, loss * feedClock * 3, 8, 6);
            scanlineRaw.color = new Color(.03f, .04f, .045f, .28f);
        }

        void RefreshEffects(bool live)
        {
            var m = run.Model;
            var s = m.settings;
            float[] remaining = { Mathf.Max(0, m.ShieldUntil - m.Elapsed), Mathf.Max(0, m.LaserSlowUntil - m.Elapsed), Mathf.Max(0, m.MovementSlowUntil - m.Elapsed) };
            float[] durations = { s.shieldDuration, s.laserSlowDuration, s.movementSlowDuration };
            int activeRows = 0;
            for (int i = 0; i < 3; i++)
            {
                bool active = live && remaining[i] > 0;
                effectIcons[i].SetActive(active);
                effectRings[i].fillAmount = Mathf.Clamp01(remaining[i] / Mathf.Max(.001f, durations[i]));
                effectTimes[i].text = remaining[i].ToString("0.0") + " s";
                if (active)
                {
                    effectIcons[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(8, -30 - activeRows * 44);
                    activeRows++;
                }
            }
            if (effectsGroup) effectsGroup.SetActive(live && activeRows > 0);
            if (effectsRect) effectsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 34 + activeRows * 44);
        }

        void RefreshOverlay(int mode)
        {
            var m = run.Model;
            overlay.SetActive(mode != 0);
            if (readyGroup) readyGroup.SetActive(mode == 1);
            if (resultGroup) resultGroup.SetActive(mode == 3 || mode == 4);
            if (startButton) startButton.gameObject.SetActive(mode == 1);
            if (restartButton) restartButton.gameObject.SetActive(mode == 3 || mode == 4);
            if (overlayKicker) overlayKicker.color = mode == 3 ? Red : Muted;
            if (mode == 1)
            {
                overlayKicker.text = "CAM 01 / STANDBY";
                overlayTitle.text = "LASER CORRIDOR";
                overlayBody.text = ReadyInstructions(m.settings.duration);
            }
            else if (mode == 3)
            {
                overlayKicker.text = "CAM 01 / FEED ENDED";
                overlayTitle.text = "SIGNAL LOST";
                overlayBody.text = m.DeathReason + "\n\nThe run will reset automatically.";
                resultCaption.text = "Resetting in";
                resetCountdown.text = Mathf.Max(0, 2 - run.player.DeathTime).ToString("0.0") + " s";
                resetCountdown.color = Red;
            }
            else if (mode == 4)
            {
                overlayKicker.text = "CAM 01 / EXIT REACHED";
                overlayTitle.text = "CORRIDOR CLEARED";
                overlayBody.text = "Security is offline. You reached the exit.\n\nThe run is complete.";
                resultCaption.text = "Health remaining";
                resetCountdown.text = m.Health + " HP";
                resetCountdown.color = Ivory;
            }
            if (lastOverlayMode != mode)
            {
                var events = EventSystem.current;
                if (events)
                {
                    GameObject target = mode == 1 ? startButton.gameObject : mode >= 3 ? restartButton.gameObject : null;
                    events.SetSelectedGameObject(null);
                    if (target) events.SetSelectedGameObject(target);
                }
                lastOverlayMode = mode;
            }
        }
    }
}
