using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class InputReader : IDisposable
    {
        private readonly GameInput _input;
        private readonly Camera _aimCamera;
        private readonly Transform _playerTransform;

        public InputReader(Camera aimCamera, Transform playerTransform)
        {
            _aimCamera = aimCamera;
            _playerTransform = playerTransform;
            _input = new GameInput();
            _input.Enable();
        }

        public Vector2 MoveDirection
        {
            get { return _input.Player.Move.ReadValue<Vector2>(); }
        }

        public Vector2 LookDirection
        {
            get
            {
                Vector2 controllerLook = _input.Player.Look.ReadValue<Vector2>();

                if (controllerLook.sqrMagnitude > 0.0001f)
                {
                    return controllerLook;
                }

                if (TryGetPointerLookDirection(out Vector2 pointerLook))
                {
                    return pointerLook;
                }

                return Vector2.zero;
            }
        }

        public bool IsShootHeld(float lookThreshold)
        {
            Vector2 controllerLook = _input.Player.Look.ReadValue<Vector2>();

            if (controllerLook.magnitude > lookThreshold)
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed;
        }

        public void Dispose()
        {
            _input.Disable();
            _input.Dispose();
        }

        private bool TryGetPointerLookDirection(out Vector2 lookDirection)
        {
            lookDirection = Vector2.zero;

            Mouse mouse = Mouse.current;
            if (mouse == null || _aimCamera == null || _playerTransform == null)
            {
                return false;
            }

            Ray ray = _aimCamera.ScreenPointToRay(mouse.position.ReadValue());
            Plane groundPlane = new Plane(Vector3.up, _playerTransform.position);

            if (!groundPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3 worldPoint = ray.GetPoint(distance);
            Vector3 delta = worldPoint - _playerTransform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            delta.Normalize();
            lookDirection = new Vector2(delta.x, delta.z);
            return true;
        }
    }
}
