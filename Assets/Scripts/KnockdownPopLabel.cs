using TMPro;
using UnityEngine;

namespace Knockdown
{
    public sealed class KnockdownPopLabel : MonoBehaviour
    {
        private TMP_Text _text;
        private float _age;
        private Vector3 _origin;
        private void Awake() { _text = GetComponent<TMP_Text>(); _origin = transform.position; }
        private void Update()
        {
            _age += Time.deltaTime;
            transform.position = _origin + Vector3.up * (_age * 0.65f);
            var color = _text.color;
            color.a = Mathf.Clamp01((0.55f - _age) / 0.20f);
            _text.color = color;
            if (_age >= 0.55f) Destroy(gameObject);
        }
    }
}
