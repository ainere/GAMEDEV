using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RayOEZoning
{
    public enum GameMode
    {
        Playing,
        Keypad,
        Paused,
        Caught,
        Won
    }

    [DefaultExecutionOrder(1000)]
    public sealed class StealthGame : MonoBehaviour
    {
        public FirstPersonController controller;
        public StarterAssetsInputs inputs;
        public CharacterController body;
        public Camera playerCamera;
        public Light torch;
        public Transform torchOrigin;
        public Transform spawn;
        public Transform keypad;
        public HazardDetector[] hazards;
        public HiddenClue[] clues;
        public StealthStance stance;
        public ClueDirector director;
        public float escapeZ = 27.5f;
        public float exposureSeconds = 1.35f;
        public float decaySeconds = 1.5f;

        public GameMode Mode { get; private set; } = GameMode.Playing;
        public float Alert { get; private set; }
        public bool ExitUnlocked { get; private set; }
        public bool TorchOn { get; private set; }
        public string KeypadInput { get; private set; } = string.Empty;
        public string Notice { get; private set; } = string.Empty;
        public string Prompt { get; private set; } = string.Empty;
        public float CaughtFade { get; private set; }
        public string DetectorName { get; private set; } = string.Empty;
        public float PatrolTime { get; private set; }
        public bool Diagnostics { get; private set; }
        public bool CanUseKeypad => Mode == GameMode.Playing && !ExitUnlocked && IsNearKeypad();
        // This run's code and clues. Falls back to the fixed classic puzzle when nothing has rolled one.
        public PuzzleRun Puzzle => puzzle ?? PuzzleCatalog.Classic;

        public event System.Action Changed;

        private PuzzleRun puzzle;
        private PlayerInput playerInput;
        private float noticeRemaining;
        private bool initialized;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool hasSpawnPose;

        private void Awake()
        {
            InitializeReferences();
        }

        private void OnEnable()
        {
            InitializeReferences();
            ApplyModeState();
        }

        private void InitializeReferences()
        {
            if (initialized)
                return;

            if (controller == null)
                controller = GetComponent<FirstPersonController>();
            if (body == null)
                body = controller != null ? controller.GetComponent<CharacterController>() : GetComponent<CharacterController>();
            if (inputs == null)
                inputs = controller != null ? controller.GetComponent<StarterAssetsInputs>() : GetComponent<StarterAssetsInputs>();
            if (playerInput == null)
                playerInput = controller != null ? controller.GetComponent<PlayerInput>() : GetComponent<PlayerInput>();
            if (playerCamera == null && controller != null)
                playerCamera = controller.GetComponentInChildren<Camera>(true);
            if (torch == null && playerCamera != null)
                torch = playerCamera.GetComponentInChildren<Light>(true);
            if (spawn == null && body != null)
                spawn = body.transform;
            if (spawn != null)
            {
                spawnPosition = spawn.position;
                spawnRotation = spawn.rotation;
                hasSpawnPose = true;
            }

            if (controller != null)
            {
                controller.MoveSpeed = 2.2f;
                controller.SprintSpeed = 2.2f;
                controller.JumpHeight = 0f;
            }

            if (stance == null && controller != null)
                stance = controller.GetComponent<StealthStance>();
            if (stance == null)
                stance = FindAnyObjectByType<StealthStance>();
            if (stance != null)
                stance.Bind(this, controller, body,
                    controller != null && controller.CinemachineCameraTarget != null
                        ? controller.CinemachineCameraTarget.transform
                        : null);

            initialized = true;
            SetTorch(true);
            ApplyModeState();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f3Key.wasPressedThisFrame)
                ToggleDiagnostics();

            if (Mode == GameMode.Playing)
            {
                if (keyboard.fKey.wasPressedThisFrame)
                    ToggleTorch();
                if (keyboard.eKey.wasPressedThisFrame)
                    TryOpenKeypad();
            }
            else if (Mode == GameMode.Won)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                    NewGame();
            }
            else if (Mode == GameMode.Keypad)
            {
                ReadKeypadKeyboard(keyboard);
                if (keyboard.escapeKey.wasPressedThisFrame)
                    CloseOverlay();
            }
            else if (Mode == GameMode.Paused)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                    CloseOverlay();
            }

            if (inputs != null)
            {
                inputs.jump = false;
            }
        }

        private void FixedUpdate()
        {
            Advance(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            // LateUpdate refreshes visibility with this render frame's final camera direction.
            UpdateClues();
            UpdatePrompt();
        }

        // Installs a freshly rolled puzzle and the clue surfaces that carry it.
        public void SetPuzzle(PuzzleRun run, HiddenClue[] active)
        {
            puzzle = run;
            clues = active;
            KeypadInput = string.Empty;
            Changed?.Invoke();
        }

        public void SetMode(GameMode mode)
        {
            Mode = mode;
            ApplyModeState();
            Changed?.Invoke();
        }

        public void OpenKeypad()
        {
            if (Mode != GameMode.Playing || !IsNearKeypad())
                return;

            KeypadInput = string.Empty;
            Notice = string.Empty;
            noticeRemaining = 0f;
            SetMode(GameMode.Keypad);
        }

        public void CloseOverlay()
        {
            if (Mode == GameMode.Keypad || Mode == GameMode.Paused)
                SetMode(GameMode.Playing);
        }

        public void AppendDigit(string digit)
        {
            if (Mode != GameMode.Keypad || string.IsNullOrEmpty(digit) || digit.Length != 1 ||
                digit[0] < '0' || digit[0] > '9' || KeypadInput.Length >= 4)
                return;

            KeypadInput += digit;
            Notice = string.Empty;
            noticeRemaining = 0f;
            Changed?.Invoke();
        }

        public void Backspace()
        {
            if (Mode != GameMode.Keypad || KeypadInput.Length == 0)
                return;

            KeypadInput = KeypadInput.Substring(0, KeypadInput.Length - 1);
            Notice = string.Empty;
            noticeRemaining = 0f;
            Changed?.Invoke();
        }

        public bool SubmitCode()
        {
            if (Mode != GameMode.Keypad)
                return false;

            if (KeypadInput == Puzzle.code)
            {
                ExitUnlocked = true;
                KeypadInput = string.Empty;
                SetNotice("Archive unlocked");
                SetMode(GameMode.Playing);
                return true;
            }

            SetNotice("Wrong code");
            Changed?.Invoke();
            return false;
        }

        public void Retry()
        {
            KeypadInput = string.Empty;
            ExitUnlocked = false;
            Alert = 0f;
            CaughtFade = 0f;
            PatrolTime = 0f;
            DetectorName = string.Empty;
            Prompt = string.Empty;
            Notice = string.Empty;
            noticeRemaining = 0f;
            SetTorch(true);

            if (body != null && hasSpawnPose)
            {
                bool wasEnabled = body.enabled;
                body.enabled = false;
                body.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                body.enabled = wasEnabled;
                Physics.SyncTransforms();
            }

            if (controller != null)
                controller.ResetView();

            if (stance != null)
                stance.ResetStance();

            SetPatrols(0f);
            SetMode(GameMode.Playing);
        }

        public void NewGame()
        {
            if (director)
                director.Reroll();
            Retry();
            Changed?.Invoke();
        }

        public void ToggleTorch()
        {
            if (Mode == GameMode.Playing)
                SetTorch(!TorchOn);
        }

        public void SetTorch(bool on)
        {
            TorchOn = on;
            if (torch != null)
                torch.enabled = on;
            Changed?.Invoke();
        }

        public void ToggleDiagnostics()
        {
            Diagnostics = !Diagnostics;
            Changed?.Invoke();
        }

        public void Advance(float dt)
        {
            float step = Mathf.Max(0f, dt);

            if (Mode == GameMode.Caught)
            {
                CaughtFade = Mathf.Min(0.65f, CaughtFade + step);
                if (CaughtFade >= 0.65f)
                    Retry();
                return;
            }

            if (Mode != GameMode.Playing)
                return;

            PatrolTime += step;
            SetPatrols(PatrolTime);
            Physics.SyncTransforms();

            bool detected = false;
            DetectorName = string.Empty;
            if (hazards != null)
            {
                for (int i = 0; i < hazards.Length; i++)
                {
                    HazardDetector hazard = hazards[i];
                    if (hazard == null || !hazard.Evaluate())
                        continue;

                    if (!detected)
                        DetectorName = hazard.name;
                    detected = true;
                }
            }

            float previousAlert = Alert;
            if (detected)
                Alert = Mathf.Min(1f, Alert + step / Mathf.Max(0.0001f, exposureSeconds));
            else
                Alert = Mathf.Max(0f, Alert - step / Mathf.Max(0.0001f, decaySeconds));

            if (Alert >= 0.99999f)
            {
                Alert = 1f;
                CaughtFade = 0f;
                SetMode(GameMode.Caught);
                return;
            }

            UpdateClues();

            if (ExitUnlocked && body != null && body.transform.position.z >= escapeZ)
            {
                SetMode(GameMode.Won);
                return;
            }

            if (noticeRemaining > 0f)
            {
                noticeRemaining = Mathf.Max(0f, noticeRemaining - step);
                if (noticeRemaining == 0f)
                    Notice = string.Empty;
            }

            if (!Mathf.Approximately(previousAlert, Alert))
                Changed?.Invoke();
        }

        private void ApplyModeState()
        {
            bool playing = Mode == GameMode.Playing;
            ClearInput();
            if (!playing && stance != null)
                stance.ClearSprint();

            if (controller != null)
                controller.enabled = playing;
            if (playerInput != null)
                playerInput.enabled = playing;
            if (inputs != null)
            {
                inputs.cursorLocked = playing;
                inputs.cursorInputForLook = playing;
            }

            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
        }

        private void ClearInput()
        {
            if (inputs == null)
                return;
            inputs.move = Vector2.zero;
            inputs.look = Vector2.zero;
            inputs.jump = false;
            inputs.sprint = false;
        }

        private void SetPatrols(float elapsed)
        {
            if (hazards == null)
                return;
            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] != null)
                    hazards[i].SetPatrolTime(elapsed);
            }
        }

        private void UpdateClues()
        {
            if (torch != null && playerCamera != null)
            {
                Transform emitter = torchOrigin ? torchOrigin : playerCamera.transform;
                torch.transform.SetPositionAndRotation(emitter.position, emitter.rotation);
            }
            RevealInk.Push(torch, TorchOn);
            if (clues == null)
                return;

            for (int i = 0; i < clues.Length; i++)
            {
                if (clues[i] != null)
                    clues[i].Evaluate(TorchOn, torch ? torch.range : 8f, torch ? torch.spotAngle : 50f,
                        torch ? torch.transform : playerCamera ? playerCamera.transform : null);
            }
        }

        private void UpdatePrompt()
        {
            Prompt = Mode == GameMode.Playing && !ExitUnlocked && IsNearKeypad() ? "E  Use terminal" : string.Empty;
        }

        private bool IsNearKeypad()
        {
            if (keypad == null || body == null)
                return false;
            return Vector3.Distance(body.transform.position, keypad.position) <= 2.5f;
        }

        private void TryOpenKeypad()
        {
            if (IsNearKeypad())
                OpenKeypad();
        }

        private void ReadKeypadKeyboard(Keyboard keyboard)
        {
            if (keyboard.backspaceKey.wasPressedThisFrame)
                Backspace();
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                SubmitCode();

            for (int i = 0; i <= 9; i++)
            {
                Key digitKey = i == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + i - 1);
                Key padKey = (Key)((int)Key.Numpad0 + i);
                if (keyboard[digitKey].wasPressedThisFrame || keyboard[padKey].wasPressedThisFrame)
                {
                    AppendDigit(i.ToString());
                    break;
                }
            }
        }

        private void SetNotice(string notice)
        {
            Notice = notice;
            noticeRemaining = 0.8f;
            Changed?.Invoke();
        }
    }
}
