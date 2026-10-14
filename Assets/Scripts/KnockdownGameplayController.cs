using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Knockdown
{
    public sealed class KnockdownGameplayController : MonoBehaviour
    {
        [SerializeField] private RoundSettings settings = new RoundSettings();
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private KnockdownBalloon[] balloons;
        [SerializeField] private KnockdownDart dartPrefab;
        [SerializeField] private KnockdownHud hud;
        [SerializeField] private KnockdownFeedback feedback;
        [SerializeField] private float aimPlaneZ = 3f;
        [SerializeField, Min(1f)] private float launchSpeed = 28f;
        [SerializeField] private Vector2 aimXBounds = new Vector2(-4.8f, 4.8f);
        [SerializeField] private Vector2 aimYBounds = new Vector2(0.65f, 4.85f);
        private readonly HashSet<KnockdownDart> _darts = new HashSet<KnockdownDart>();
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
        private EventSystem _pointerEventSystem;
        private PointerEventData _uiPointerData;
        private RoundPhase _previousPhase;
        private bool _ownsPause;
        private bool _waitForRelease;
        private bool _ignoreNextLook = true;
        private KnockdownCameraFraming _cameraFraming;
        private RoundModel _model;

        public RoundModel Model => _model;
        public Camera GameplayCamera => gameplayCamera;
        public IReadOnlyList<KnockdownBalloon> Balloons => balloons;
        public float AimPlaneZ => aimPlaneZ;
        public int ActiveProjectileCount => _darts.Count;
        public bool IsMouseLookActive => _model != null && _model.Phase == RoundPhase.Playing && !_model.IsPaused;

        public void Configure(Camera camera, KnockdownBalloon[] targets, KnockdownDart dart,
            KnockdownHud interfaceView, KnockdownFeedback effects)
        {
            gameplayCamera = camera;
            balloons = targets;
            dartPrefab = dart;
            hud = interfaceView;
            feedback = effects;
        }

        private void Awake()
        {
            _cameraFraming = gameplayCamera.GetComponent<KnockdownCameraFraming>();
            _model = new RoundModel(settings);
            _previousPhase = _model.Phase;
            foreach (var balloon in balloons) balloon.Initialize(this);
            hud.Bind(this);
            _model.Changed += OnModelChanged;
            ResetTargets();
            OnModelChanged();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_model.IsPaused) ResumeRound(); else PauseRound();
            }

            _model.Tick(Time.deltaTime);
            foreach (var balloon in balloons)
                balloon.TickRespawn(Time.deltaTime, _model.Phase == RoundPhase.Playing && !_model.IsPaused);

            var mouse = Mouse.current;
            var interactive = IsMouseLookActive;
            if (interactive)
            {
                if (_ignoreNextLook) _ignoreNextLook = false;
                else if (mouse != null && _cameraFraming != null)
                    _cameraFraming.ApplyMouseLook(mouse.delta.ReadValue());
            }

            var screenPoint = gameplayCamera.pixelRect.center;
            var aimPoint = Vector3.zero;
            var hasAim = interactive && TryGetCenterAim(out aimPoint);
            hud.SetCenteredAim(interactive, hasAim);

            if (mouse == null) return;
            if (!mouse.leftButton.isPressed) _waitForRelease = false;
            if (!hasAim || _waitForRelease || !mouse.leftButton.isPressed) return;
            if (IsPointerOverUI(screenPoint)) return;
            FireAt(aimPoint);
        }

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            if (_pointerEventSystem != eventSystem || _uiPointerData == null)
            {
                _pointerEventSystem = eventSystem;
                _uiPointerData = new PointerEventData(eventSystem);
            }
            _uiPointerData.Reset();
            _uiPointerData.position = screenPosition;
            _uiRaycastResults.Clear();
            // Check the crosshair against UI before launching a dart.
            eventSystem.RaycastAll(_uiPointerData, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }

        private void FixedUpdate()
        {
            if (_model == null || _model.IsPaused) return;
            foreach (var balloon in balloons)
                balloon.Move(_model.Mode, _model.ElapsedPlaySeconds);
        }

        public bool TryGetAim(Vector2 screenPosition, out Vector3 point)
        {
            point = default;
            if (screenPosition.x < 0 || screenPosition.y < 0 || screenPosition.x > Screen.width || screenPosition.y > Screen.height)
                return false;
            return TryGetAim(gameplayCamera.ScreenPointToRay(screenPosition), out point);
        }

        public bool TryGetCenterAim(out Vector3 point) =>
            TryGetAim(gameplayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out point);

        private bool TryGetAim(Ray ray, out Vector3 point)
        {
            point = default;
            var plane = new Plane(Vector3.back, new Vector3(0, 0, aimPlaneZ));
            if (!plane.Raycast(ray, out var distance)) return false;
            point = ray.GetPoint(distance);
            return point.x >= aimXBounds.x && point.x <= aimXBounds.y && point.y >= aimYBounds.x && point.y <= aimYBounds.y;
        }

        // Launch a physical dart toward an in-bounds point on the target plane.
        public bool FireAt(Vector3 worldPoint)
        {
            if (_model == null || dartPrefab == null || !IsFinite(worldPoint)) return false;
            if (worldPoint.x < aimXBounds.x || worldPoint.x > aimXBounds.y || worldPoint.y < aimYBounds.x || worldPoint.y > aimYBounds.y)
                return false;
            var origin = gameplayCamera.transform.TransformPoint(new Vector3(0, -0.32f, 0.45f));
            var direction = worldPoint - origin;
            if (direction.sqrMagnitude < 0.01f || direction.z <= 0 || !_model.TryFire(out var shotId)) return false;
            var dart = Instantiate(dartPrefab, origin, Quaternion.LookRotation(direction));
            _darts.Add(dart);
            dart.Launch(this, shotId, direction.normalized * launchSpeed);
            feedback.Throw();
            return true;
        }

        private static bool IsFinite(Vector3 point) =>
            !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
            !float.IsNaN(point.y) && !float.IsInfinity(point.y) &&
            !float.IsNaN(point.z) && !float.IsInfinity(point.z);

        internal void ResolveDart(KnockdownDart dart, KnockdownBalloon balloon, Vector3 position)
        {
            if (!_darts.Remove(dart)) return;
            var canScore = !_model.IsPaused && (_model.Phase == RoundPhase.Playing || _model.Phase == RoundPhase.Resolving);
            var popped = canScore && balloon != null && balloon.TryPop();
            if (_model.ResolveShot(dart.ShotId, popped))
            {
                if (popped) { feedback.Pop(balloon.transform.position, gameplayCamera); hud.PulseScore(); }
                else feedback.Miss(position);
            }
        }

        public void StartRound()
        {
            if (_model.Phase != RoundPhase.Ready) return;
            RestoreTime();
            ClearDarts();
            ResetTargets();
            ResetLook();
            _waitForRelease = true;
            _model.StartRound();
        }

        public void RestartRound()
        {
            RestoreTime();
            ClearDarts();
            ResetTargets();
            ResetLook();
            feedback.ClearEffects();
            _waitForRelease = true;
            _model.RestartRound();
        }

        public void SelectMode(GameMode mode)
        {
            RestoreTime();
            ClearDarts();
            feedback.ClearEffects();
            ResetLook();
            _waitForRelease = true;
            _model.SelectMode(mode);
            ResetTargets();
        }

        public void SelectStatic() => SelectMode(GameMode.Static);
        public void SelectMoving() => SelectMode(GameMode.Moving);
        public void SelectOtherMode() => SelectMode(_model.Mode == GameMode.Static ? GameMode.Moving : GameMode.Static);

        public void PauseRound()
        {
            _model.Pause();
            if (!_model.IsPaused) return;
            _ownsPause = true;
            Time.timeScale = 0f;
            feedback.SetPaused(true);
            _waitForRelease = true;
        }

        public void ResumeRound()
        {
            RestoreTime();
            _waitForRelease = true;
            _ignoreNextLook = true;
            _model.Resume();
        }

        private void ResetLook()
        {
            if (_cameraFraming != null) _cameraFraming.ResetLook();
            _ignoreNextLook = true;
        }

        private void RestoreTime()
        {
            if (_ownsPause)
            {
                Time.timeScale = 1f;
                if (feedback != null) feedback.SetPaused(false);
            }
            _ownsPause = false;
        }

        private void ResetTargets()
        {
            foreach (var balloon in balloons) balloon.ResetTarget(_model.Mode);
        }

        private void ClearDarts()
        {
            foreach (var dart in _darts) if (dart != null) dart.Discard();
            _darts.Clear();
        }

        private void OnModelChanged()
        {
            if (_model.Phase != _previousPhase)
            {
                if (_model.Phase == RoundPhase.Playing)
                {
                    feedback.StartBell();
                    _ignoreNextLook = true;
                }
                if (_model.Phase == RoundPhase.Resolving) feedback.EndBell();
                if (_model.Phase == RoundPhase.Results) ClearDarts();
                _previousPhase = _model.Phase;
            }
            var looking = IsMouseLookActive;
            Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !looking;
            hud.Refresh();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused || _model == null) return;
            _ignoreNextLook = true;
            _waitForRelease = true;
            PauseRound();
        }

        private void OnDisable()
        {
            RestoreTime();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_model != null) _model.Changed -= OnModelChanged;
            ClearDarts();
        }
    }
}
