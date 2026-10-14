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
            if (_age >= 1.1f || transform.position.z > _controller.MaxDartTravelZ)
                Finish(null, transform.position);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_resolved || _controller == null) return;
            var target = collision.collider.GetComponentInParent<KnockdownTarget>();
            var position = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Finish(target, position);
        }

        private void Finish(KnockdownTarget target, Vector3 position)
        {
            if (_resolved) return;
            _resolved = true;
            _controller.ResolveDart(this, target, position);
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
