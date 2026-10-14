using UnityEngine;

namespace Knockdown
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class KnockdownDart : MonoBehaviour
    {
        private Rigidbody _body;
        private KnockdownGameplayController _controller;
        private float _age;
        private bool _resolved;
        public int ShotId { get; private set; }

        public void Launch(KnockdownGameplayController controller, int shotId, Vector3 velocity)
        {
            _controller = controller;
            ShotId = shotId;
            _body = GetComponent<Rigidbody>();
            _body.linearVelocity = velocity;
            _body.angularVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (_controller == null || _resolved) return;
            _age += Time.deltaTime;
            if (_age >= 1.1f || transform.position.z > _controller.AimPlaneZ + 0.7f)
                Finish(null, transform.position);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_resolved || _controller == null) return;
            var balloon = collision.collider.GetComponentInParent<KnockdownBalloon>();
            var position = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Finish(balloon, position);
        }

        private void Finish(KnockdownBalloon balloon, Vector3 position)
        {
            if (_resolved) return;
            _resolved = true;
            _controller.ResolveDart(this, balloon, position);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public void Discard()
        {
            _resolved = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
