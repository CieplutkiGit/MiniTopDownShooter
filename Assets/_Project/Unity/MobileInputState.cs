using UnityEngine;

namespace Game
{
    public enum MobileInputAction
    {
        Fire,
        Reload,
        NextWeapon,
        PreviousWeapon,
        Pause,
        Interact
    }

    public class MobileInputState : MonoBehaviour
    {
        private Vector2 _move;
        private Vector2 _look;
        private bool _aimFireHeld;
        private bool _buttonFireHeld;
        private bool _firePressed;
        private bool _reloadPressed;
        private bool _nextWeaponPressed;
        private bool _previousWeaponPressed;
        private bool _pausePressed;
        private bool _interactPressed;

        public Vector2 MoveDirection => _move;
        public Vector2 LookDirection => _look;
        public bool FireHeld => _aimFireHeld || _buttonFireHeld;

        public void SetMove(Vector2 direction)
        {
            _move = Vector2.ClampMagnitude(direction, 1f);
        }

        public void SetLook(Vector2 direction, bool fireWhileAiming, float fireThreshold)
        {
            _look = Vector2.ClampMagnitude(direction, 1f);
            bool previous = _aimFireHeld;
            _aimFireHeld = fireWhileAiming && _look.magnitude >= Mathf.Clamp01(fireThreshold);

            if (_aimFireHeld && !previous)
            {
                _firePressed = true;
            }
        }

        public void SetFireButton(bool held)
        {
            if (held && !_buttonFireHeld)
            {
                _firePressed = true;
            }

            _buttonFireHeld = held;
        }

        public void Press(MobileInputAction action)
        {
            switch (action)
            {
                case MobileInputAction.Fire:
                    SetFireButton(true);
                    break;

                case MobileInputAction.Reload:
                    _reloadPressed = true;
                    break;

                case MobileInputAction.NextWeapon:
                    _nextWeaponPressed = true;
                    break;

                case MobileInputAction.PreviousWeapon:
                    _previousWeaponPressed = true;
                    break;

                case MobileInputAction.Pause:
                    _pausePressed = true;
                    break;

                case MobileInputAction.Interact:
                    _interactPressed = true;
                    break;
            }
        }

        public void Release(MobileInputAction action)
        {
            if (action == MobileInputAction.Fire)
            {
                SetFireButton(false);
            }
        }

        public bool ConsumeFirePressed()
        {
            bool value = _firePressed;
            _firePressed = false;
            return value;
        }

        public bool ConsumeReloadPressed()
        {
            bool value = _reloadPressed;
            _reloadPressed = false;
            return value;
        }

        public bool ConsumeNextWeaponPressed()
        {
            bool value = _nextWeaponPressed;
            _nextWeaponPressed = false;
            return value;
        }

        public bool ConsumePreviousWeaponPressed()
        {
            bool value = _previousWeaponPressed;
            _previousWeaponPressed = false;
            return value;
        }

        public bool ConsumePausePressed()
        {
            bool value = _pausePressed;
            _pausePressed = false;
            return value;
        }

        public bool ConsumeInteractPressed()
        {
            bool value = _interactPressed;
            _interactPressed = false;
            return value;
        }

        public void ResetGameplayState()
        {
            _move = Vector2.zero;
            _look = Vector2.zero;
            _aimFireHeld = false;
            _buttonFireHeld = false;
            _firePressed = false;
            _reloadPressed = false;
            _nextWeaponPressed = false;
            _previousWeaponPressed = false;
            _interactPressed = false;
        }

        public void ResetAll()
        {
            ResetGameplayState();
            _pausePressed = false;
        }
    }
}
