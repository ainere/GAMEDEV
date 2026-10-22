using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RayOEZoning
{
    public enum PlayerStance
    {
        Standing,
        Crouched,
        Prone
    }

    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class StealthStance : MonoBehaviour
    {
        const float StandingHeight = 1.8f;
        const float CrouchedHeight = 1.1f;
        const float ProneHeight = 0.56f;
        const float PlayerRadius = 0.28f;
        const float SkinWidth = 0.025f;

        public StealthGame game;
        public FirstPersonController controller;
        public CharacterController body;
        public Transform cameraRoot;
        public LayerMask obstructionMask;

        public PlayerStance CurrentStance { get; private set; } = PlayerStance.Standing;
        public bool IsSprinting { get; private set; }
        public float EyeHeight => cameraRoot ? cameraRoot.localPosition.y : EyeHeightFor(CurrentStance);
        public float NormalizedMoveSpeed
        {
            get
            {
                if (!body || !controller)
                    return 0f;

                Vector3 velocity = body.velocity;
                float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
                float maximum = IsSprinting ? controller.SprintSpeed : controller.MoveSpeed;
                return maximum > 0.001f ? Mathf.Clamp01(horizontalSpeed / maximum) : 0f;
            }
        }

        StarterAssetsInputs inputs;
        Vector3 standingCameraLocalPosition;
        Vector3 standingBodyCenter;
        float feetLocalY;
        CharacterController capturedBody;
        Transform capturedCameraRoot;

        private void Awake()
        {
            Bind(game, controller, body, cameraRoot);
        }

        public void Bind(StealthGame owner, FirstPersonController firstPersonController,
            CharacterController characterBody, Transform viewRoot)
        {
            if (owner != null)
                game = owner;
            if (firstPersonController != null)
                controller = firstPersonController;
            if (characterBody != null)
                body = characterBody;
            if (viewRoot != null)
                cameraRoot = viewRoot;

            if (controller == null)
                controller = GetComponent<FirstPersonController>();
            if (body == null && controller != null)
                body = controller.GetComponent<CharacterController>();
            if (inputs == null && controller != null)
                inputs = controller.GetComponent<StarterAssetsInputs>();
            if (inputs == null && game != null)
                inputs = game.inputs;
            if (cameraRoot == null && controller != null && controller.CinemachineCameraTarget != null)
                cameraRoot = controller.CinemachineCameraTarget.transform;

            if (body != null && body != capturedBody)
            {
                capturedBody = body;
                standingBodyCenter = body.center;
                feetLocalY = body.center.y - body.height * 0.5f;
                ApplyBodyStance(CurrentStance);
            }
            if (cameraRoot != null && cameraRoot != capturedCameraRoot)
            {
                capturedCameraRoot = cameraRoot;
                standingCameraLocalPosition = cameraRoot.localPosition;
                ApplyCameraStance(CurrentStance);
            }
            if (controller != null)
                ApplyControllerStance(CurrentStance);
            if (inputs != null && (game == null || game.Mode != GameMode.Playing))
                ClearSprint();

            if (game != null && game.stance != this)
                game.stance = this;
        }

        private void Update()
        {
            // The game bootstrap may assign references after Awake, so resolve them each frame
            // without recapturing baselines from an already-bound component.
            Bind(game, controller, body, cameraRoot);

            Keyboard keyboard = Keyboard.current;
            if (game == null || game.Mode != GameMode.Playing || keyboard == null)
            {
                ClearSprint();
                return;
            }

            if (keyboard.cKey.wasPressedThisFrame || keyboard.leftCtrlKey.wasPressedThisFrame)
            {
                PlayerStance next = CurrentStance == PlayerStance.Crouched
                    ? PlayerStance.Standing
                    : PlayerStance.Crouched;
                TrySetStance(next);
            }
            if (keyboard.zKey.wasPressedThisFrame)
            {
                PlayerStance next = CurrentStance == PlayerStance.Prone
                    ? PlayerStance.Standing
                    : PlayerStance.Prone;
                TrySetStance(next);
            }

            bool forward = inputs != null && inputs.move.y > 0.01f;
            IsSprinting = CurrentStance == PlayerStance.Standing && forward && keyboard.leftShiftKey.isPressed;
            if (inputs != null)
                inputs.sprint = IsSprinting;
        }

        public bool TrySetStance(PlayerStance stance)
        {
            Bind(game, controller, body, cameraRoot);
            if ((int)stance < (int)PlayerStance.Standing || (int)stance > (int)PlayerStance.Prone)
                return false;
            if (game != null && game.Mode != GameMode.Playing)
                return false;
            if (stance == CurrentStance)
                return true;

            float requestedHeight = HeightFor(stance);
            if (body != null && requestedHeight > body.height && !HasHeadroom(requestedHeight))
                return false;

            ApplyStance(stance);
            return true;
        }

        public void ResetStance()
        {
            Bind(game, controller, body, cameraRoot);
            ApplyStance(PlayerStance.Standing);
        }

        public void ClearSprint()
        {
            IsSprinting = false;
            if (inputs != null)
                inputs.sprint = false;
        }

        private bool HasHeadroom(float requestedHeight)
        {
            if (body == null || obstructionMask.value == 0)
                return true;

            float queryRadius = Mathf.Max(0.001f, body.radius - body.skinWidth);
            Vector3 center = new Vector3(standingBodyCenter.x,
                feetLocalY + requestedHeight * 0.5f, standingBodyCenter.z);
            // Keep the query's sphere centers at the full CharacterController radius endpoints.
            // Using queryRadius for the endpoint offset makes its bottom touch the floor and can
            // report a grounded floor as blocked headroom.
            Vector3 lower = body.transform.TransformPoint(center + Vector3.up * (-requestedHeight * 0.5f + body.radius));
            Vector3 upper = body.transform.TransformPoint(center + Vector3.up * (requestedHeight * 0.5f - body.radius));
            int mask = obstructionMask.value & ~(1 << body.gameObject.layer);
            Physics.SyncTransforms();
            return !Physics.CheckCapsule(lower, upper, queryRadius, mask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyStance(PlayerStance stance)
        {
            CurrentStance = stance;
            ClearSprint();

            ApplyBodyStance(stance);
            ApplyCameraStance(stance);
            ApplyControllerStance(stance);
        }

        private void ApplyBodyStance(PlayerStance stance)
        {
            if (body == null || body != capturedBody)
                return;

            float height = HeightFor(stance);
            body.height = height;
            body.radius = PlayerRadius;
            body.skinWidth = SkinWidth;
            body.center = new Vector3(standingBodyCenter.x, feetLocalY + height * 0.5f, standingBodyCenter.z);
            body.stepOffset = StepOffsetFor(stance);
        }

        private void ApplyCameraStance(PlayerStance stance)
        {
            if (cameraRoot == null || cameraRoot != capturedCameraRoot)
                return;

            cameraRoot.localPosition = new Vector3(standingCameraLocalPosition.x,
                EyeHeightFor(stance), standingCameraLocalPosition.z);
        }

        private void ApplyControllerStance(PlayerStance stance)
        {
            if (controller == null)
                return;

            float speed = WalkSpeedFor(stance);
            controller.MoveSpeed = speed;
            controller.SprintSpeed = stance == PlayerStance.Standing ? 4.55f : speed;
            controller.JumpHeight = 0f;
        }

        private static float HeightFor(PlayerStance stance)
        {
            switch (stance)
            {
                case PlayerStance.Crouched: return CrouchedHeight;
                case PlayerStance.Prone: return ProneHeight;
                default: return StandingHeight;
            }
        }

        private static float EyeHeightFor(PlayerStance stance)
        {
            switch (stance)
            {
                case PlayerStance.Crouched: return 0.95f;
                case PlayerStance.Prone: return 0.43f;
                default: return 1.65f;
            }
        }

        private static float WalkSpeedFor(PlayerStance stance)
        {
            switch (stance)
            {
                case PlayerStance.Crouched: return 1.35f;
                case PlayerStance.Prone: return 0.70f;
                default: return 2.35f;
            }
        }

        private static float StepOffsetFor(PlayerStance stance)
        {
            switch (stance)
            {
                case PlayerStance.Crouched: return 0.08f;
                case PlayerStance.Prone: return 0.04f;
                default: return 0.12f;
            }
        }
    }
}
