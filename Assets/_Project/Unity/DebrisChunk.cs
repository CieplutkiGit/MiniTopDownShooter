using System;
using UnityEngine;

namespace Cieplutki.MiniTopDownShooter.Runtime
{
    public class DebrisChunk : MonoBehaviour
    {
        private Action<DebrisChunk> _onFinished;
        private Vector3 _velocity;
        private Vector3 _spin;
        private Vector3 _startScale;
        private float _lifetime;
        private float _age;
        private float _gravity;
        private float _bounce;
        private float _floorY;

        public void SetReturnCallback(Action<DebrisChunk> callback)
        {
            _onFinished = callback;
        }

        public void Initialize(Vector3 velocity, Vector3 spin, float lifetime, float gravity, float bounce, float floorY)
        {
            _velocity = velocity;
            _spin = spin;
            _lifetime = lifetime;
            _gravity = gravity;
            _bounce = bounce;
            _floorY = floorY;
            _age = 0f;
            _startScale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= _lifetime)
            {
                Finish();
                return;
            }

            _velocity.y -= _gravity * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;
            transform.Rotate(_spin * Time.deltaTime);

            float halfHeight = transform.localScale.y * 0.5f;

            if (transform.position.y < _floorY + halfHeight && _velocity.y < 0f)
            {
                Vector3 position = transform.position;
                position.y = _floorY + halfHeight;
                transform.position = position;
                _velocity.y = -_velocity.y * _bounce;
                _velocity.x *= 0.7f;
                _velocity.z *= 0.7f;
            }

            float progress = _age / _lifetime;

            if (progress > 0.6f)
            {
                float shrink = 1f - (progress - 0.6f) / 0.4f;
                transform.localScale = _startScale * shrink;
            }
        }

        private void Finish()
        {
            if (_onFinished != null)
            {
                _onFinished(this);
                return;
            }

            Destroy(gameObject);
        }
    }
}
