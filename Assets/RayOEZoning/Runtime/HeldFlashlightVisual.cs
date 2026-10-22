using UnityEngine;

namespace RayOEZoning
{
    /// <summary>Cosmetic first-person lamp. Its model never controls the beam's aim.</summary>
    [DefaultExecutionOrder(1300)]
    public sealed class HeldFlashlightVisual : MonoBehaviour
    {
        public Renderer lampGlass;
        public Transform poseRoot;
        StealthGame game;
        MaterialPropertyBlock block;
        float maximumIntensity;
        Vector3 restPosition;
        float gait;
        void Start()
        {
            game = FindAnyObjectByType<StealthGame>(); block = new MaterialPropertyBlock();
            maximumIntensity = game && game.torch ? game.torch.intensity : 28f;
            if (!poseRoot) poseRoot = transform;
            restPosition = poseRoot.localPosition;
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
                poseRoot.localPosition = Vector3.Lerp(poseRoot.localPosition, restPosition + offset, Time.deltaTime * 14);
            }
            // Proximity exposure keeps the nearby stencil surface from washing out to white.
            // Illumination origin, angle, range, on/off state and puzzle tests remain in StealthGame.
            if (game && game.torch && game.torchOrigin)
            {
                float intensity = maximumIntensity;
                if (Physics.Raycast(game.torchOrigin.position, game.torchOrigin.forward, out RaycastHit hit,
                        game.torch.range, (1 << 8) | 1, QueryTriggerInteraction.Ignore))
                    {
                    float distance = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, 6f, hit.distance));
                    intensity = Mathf.Lerp(maximumIntensity * .1f, maximumIntensity, distance);
                }
                game.torch.intensity = intensity;
            }
        }
    }
}
