using UnityEngine;

namespace RayOEZoning
{
    /// <summary>Cosmetic first-person lamp. Its model never controls the beam's aim.</summary>
    [DefaultExecutionOrder(1300)]
    public sealed class HeldFlashlightVisual : MonoBehaviour
    {
        public Renderer lampGlass;
        public Transform poseRoot;
        /// <summary>Added to the rest pose at full upward and full downward look, so the arm can always reach it.</summary>
        public Vector3 lookUpOffset = new Vector3(-.01f, -.1f, -.06f), lookDownOffset = new Vector3(0, .05f, .1f);
        /// <summary>The torch reaches its full intensity on surfaces this far away.</summary>
        const float FullBrightnessDistance = 6f;
        StealthGame game;
        MaterialPropertyBlock block;
        float maximumIntensity;
        Vector3 restPosition, current;
        float gait;
        void Start()
        {
            game = FindAnyObjectByType<StealthGame>(); block = new MaterialPropertyBlock();
            maximumIntensity = game && game.torch ? game.torch.intensity : 28f;
            if (!poseRoot) poseRoot = transform;
            restPosition = current = poseRoot.localPosition;
        }
        float LookPitch()
        {
            if (!game || !game.playerCamera || !game.body) return 0;
            Transform view = game.playerCamera.transform;
            return Mathf.Clamp(Vector3.SignedAngle(game.body.transform.forward, view.forward, view.right), -80, 80);
        }

        void LateUpdate()
        {
            if (!lampGlass || block == null) return;
            Color color = game && game.TorchOn ? new Color(3.4f, 3.1f, 2.6f) : new Color(.025f, .03f, .032f);
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color); lampGlass.SetPropertyBlock(block);
            if (game && poseRoot)
            {
                float movement = game.stance && game.Mode == GameMode.Playing ? game.stance.NormalizedMoveSpeed : 0;
                bool sprinting = game.stance && game.stance.IsSprinting;
                if (game.Mode == GameMode.Playing) gait += Time.deltaTime * movement * (sprinting ? 11 : 7);
                bool prone = game.stance && game.stance.CurrentStance == PlayerStance.Prone;
                var offset = new Vector3(Mathf.Sin(gait) * .005f * movement,
                    Mathf.Cos(gait * 2) * .004f * movement - (sprinting ? .035f : 0), prone ? .025f : 0);
                // Held lower and closer while looking up, further out while looking down past the chest.
                float pitch = LookPitch() / 80f;
                offset += pitch < 0 ? lookUpOffset * -pitch : lookDownOffset * pitch;
                // Smoothed separately from the transform: the body's arm IK may nudge the root after this runs.
                current = Application.isPlaying ? Vector3.Lerp(current, restPosition + offset, Time.deltaTime * 14) : restPosition + offset;
                poseRoot.localPosition = current;
            }
            // Auto-exposure: scale with distance squared so whatever the beam lands on is lit about as brightly at
            // arm's length as at 6 m, and paper close up never washes out to white.
            // Illumination origin, angle, range, on/off state and puzzle tests remain in StealthGame.
            if (game && game.torch && game.torchOrigin)
            {
                float target = maximumIntensity;
                if (Physics.Raycast(game.torchOrigin.position, game.torchOrigin.forward, out RaycastHit hit,
                        game.torch.range, (1 << 8) | 1, QueryTriggerInteraction.Ignore))
                {
                    float distance = hit.distance / FullBrightnessDistance;
                    target = maximumIntensity * Mathf.Clamp(distance * distance, .015f, 1f);
                }
                float blend = Application.isPlaying ? 1 - Mathf.Exp(-Time.deltaTime * 10) : 1;
                game.torch.intensity = Mathf.Lerp(game.torch.intensity, target, blend);
            }
        }
    }
}
