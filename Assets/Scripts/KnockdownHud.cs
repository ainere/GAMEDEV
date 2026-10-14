using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Knockdown
{
    public sealed class KnockdownHud : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform reticle;
        [SerializeField] private UnityEngine.UI.Image reticleImage;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text readyDescription;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text resultScore;
        [SerializeField] private TMP_Text resultDetails;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private GameObject readyPanel;
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private GameObject scrim;
        [SerializeField] private GameObject pauseButton;
        [SerializeField] private UnityEngine.UI.Image staticButtonImage;
        [SerializeField] private UnityEngine.UI.Image movingButtonImage;
        private KnockdownGameplayController _controller;
        private int _staticBest;
        private int _movingBest;
        private float _pulse;
        private readonly Color _cream = new Color32(255, 243, 205, 255);
        private readonly Color _navy = new Color32(36, 57, 74, 255);
        private readonly Color _coral = new Color32(255, 107, 87, 255);

        public void Configure(RectTransform canvas, RectTransform aim, UnityEngine.UI.Image aimImage,
            TMP_Text score, TMP_Text timer, TMP_Text description, TMP_Text countdown, TMP_Text result,
            TMP_Text details, TMP_Text best, GameObject ready, GameObject counting, GameObject paused,
            GameObject results, GameObject overlay, GameObject pauseControl,
            UnityEngine.UI.Image staticControl, UnityEngine.UI.Image movingControl)
        {
            canvasRect = canvas; reticle = aim; reticleImage = aimImage; scoreText = score; timerText = timer;
            readyDescription = description; countdownText = countdown; resultScore = result; resultDetails = details;
            bestText = best; readyPanel = ready; countdownPanel = counting; pausePanel = paused;
            resultsPanel = results; scrim = overlay; pauseButton = pauseControl;
            staticButtonImage = staticControl; movingButtonImage = movingControl;
        }

        public void Bind(KnockdownGameplayController controller) => _controller = controller;

        public void Refresh()
        {
            if (_controller == null) return;
            var model = _controller.Model;
            scoreText.SetText("{0:0}", model.Score);
            var seconds = Mathf.CeilToInt(model.SecondsRemaining);
            timerText.text = (seconds / 60).ToString("0") + ":" + (seconds % 60).ToString("00");
            timerText.color = seconds <= 10 && model.Phase != RoundPhase.Ready ? _coral : _cream;
            staticButtonImage.color = model.Mode == GameMode.Static ? _cream : _navy;
            movingButtonImage.color = model.Mode == GameMode.Moving ? _cream : _navy;
            staticButtonImage.GetComponentInChildren<TMP_Text>().color = model.Mode == GameMode.Static ? _navy : _cream;
            movingButtonImage.GetComponentInChildren<TMP_Text>().color = model.Mode == GameMode.Moving ? _navy : _cream;
            readyDescription.text = model.Mode == GameMode.Static
                ? "Knock them down. They'll stand up again."
                : "Lead your shots. Targets reset behind the covers.";
            readyPanel.SetActive(model.Phase == RoundPhase.Ready && !model.IsPaused);
            countdownPanel.SetActive(model.Phase == RoundPhase.Countdown && !model.IsPaused);
            pausePanel.SetActive(model.IsPaused);
            resultsPanel.SetActive(model.Phase == RoundPhase.Results && !model.IsPaused);
            scrim.SetActive(model.IsPaused || model.Phase == RoundPhase.Ready || model.Phase == RoundPhase.Results);
            pauseButton.SetActive(model.Phase == RoundPhase.Playing || model.Phase == RoundPhase.Countdown || model.Phase == RoundPhase.Resolving);
            countdownText.SetText("{0:0}", Mathf.Ceil(model.CountdownRemaining));
            resultScore.SetText("{0:0}", model.Score);
            resultDetails.text = model.Knockdowns + " knocked down     /     " + Mathf.RoundToInt(model.Accuracy * 100) + "% accuracy";
            if (model.Phase == RoundPhase.Results)
            {
                if (model.Mode == GameMode.Static) _staticBest = Mathf.Max(_staticBest, model.Score);
                else _movingBest = Mathf.Max(_movingBest, model.Score);
            }
            bestText.text = model.Mode.ToString().ToUpperInvariant() + " SESSION BEST  " + (model.Mode == GameMode.Static ? _staticBest : _movingBest);
        }

        public void SetAim(Vector2 screenPosition, bool visible, bool valid)
        {
            reticle.gameObject.SetActive(visible);
            if (!visible) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out var position);
            reticle.anchoredPosition = position;
            reticleImage.color = valid ? _cream : new Color(_cream.r, _cream.g, _cream.b, 0.22f);
        }

        public void SetCenteredAim(bool visible, bool valid)
        {
            reticle.gameObject.SetActive(visible);
            if (!visible) return;
            reticle.anchoredPosition = Vector2.zero;
            reticleImage.color = valid ? _cream : new Color(_cream.r, _cream.g, _cream.b, 0.22f);
        }

        public void PulseScore() => _pulse = 0.18f;
        private void Update()
        {
            _pulse = Mathf.Max(0, _pulse - Time.deltaTime);
            scoreText.transform.localScale = Vector3.one * (1 + 0.10f * Mathf.Sin(_pulse / 0.18f * Mathf.PI));
        }
    }
}
