using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class InputReader : IDisposable
    {
        private const string BindingKeyPrefix = "MiniTopDownShooter.Input.";

        private readonly GameInput _input;
        private readonly Camera _aimCamera;
        private readonly Transform _playerTransform;
        private readonly MobileInputState _mobileInput;
        private readonly InputAction _fireAction;
        private readonly InputAction _reloadAction;
        private readonly InputAction _nextWeaponAction;
        private readonly InputAction _previousWeaponAction;
        private readonly InputAction _interactAction;

        private bool _firePressedQueued;
        private bool _reloadPressedQueued;
        private bool _nextWeaponPressedQueued;
        private bool _previousWeaponPressedQueued;
        private bool _interactPressedQueued;
        private bool _previousLookShootHeld;
        private bool _requireFireNeutralBeforeRearm;

        public InputReader(
            Camera aimCamera,
            Transform playerTransform,
            MobileInputState mobileInput = null)
        {
            _aimCamera = aimCamera;
            _playerTransform = playerTransform;
            _mobileInput = mobileInput;
            _input = new GameInput();

            _fireAction = new InputAction("Fire", InputActionType.Button);
            _fireAction.AddBinding("<Mouse>/leftButton");
            _fireAction.AddBinding("<Gamepad>/rightTrigger");

            _reloadAction = new InputAction("Reload", InputActionType.Button);
            _reloadAction.AddBinding("<Keyboard>/r");
            _reloadAction.AddBinding("<Gamepad>/buttonWest");

            _nextWeaponAction = new InputAction("NextWeapon", InputActionType.Button);
            _nextWeaponAction.AddBinding("<Keyboard>/e");
            _nextWeaponAction.AddBinding("<Gamepad>/rightShoulder");

            _previousWeaponAction = new InputAction("PreviousWeapon", InputActionType.Button);
            _previousWeaponAction.AddBinding("<Keyboard>/q");
            _previousWeaponAction.AddBinding("<Gamepad>/leftShoulder");

            _interactAction = new InputAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/buttonSouth");

            LoadBindings();
            SubscribeButtonEvents();
            _input.Enable();
            _fireAction.Enable();
            _reloadAction.Enable();
            _nextWeaponAction.Enable();
            _previousWeaponAction.Enable();
            _interactAction.Enable();
        }

        public float MoveDeadzone { get; set; } = 0.1f;
        public float LookDeadzone { get; set; } = 0.1f;
        public float AimSensitivity { get; set; } = 1.0f;

        public Vector2 MoveDirection
        {
            get
            {
                Vector2 physical = _input.Player.Move.ReadValue<Vector2>();
                Vector2 mobile = _mobileInput != null
                    ? _mobileInput.MoveDirection
                    : Vector2.zero;

                Vector2 raw = mobile.sqrMagnitude > physical.sqrMagnitude
                    ? mobile
                    : physical;

                if (raw.magnitude < MoveDeadzone)
                {
                    return Vector2.zero;
                }

                return raw;
            }
        }

        public Vector2 LookDirection
        {
            get
            {
                Vector2 physicalLook = _input.Player.Look.ReadValue<Vector2>();

                if (physicalLook.magnitude >= LookDeadzone)
                {
                    return physicalLook * AimSensitivity;
                }

                if (_mobileInput != null &&
                    _mobileInput.LookDirection.magnitude >= LookDeadzone)
                {
                    return _mobileInput.LookDirection * AimSensitivity;
                }

                if (TryGetPointerLookDirection(out Vector2 pointerLook))
                {
                    return pointerLook;
                }

                return Vector2.zero;
            }
        }

        public bool IsFireArmed => !_requireFireNeutralBeforeRearm;

        public void RequireNeutralToRearm()
        {
            _requireFireNeutralBeforeRearm = true;
            _firePressedQueued = false;
            _previousLookShootHeld = false;
        }

        public void ArmFire()
        {
            _requireFireNeutralBeforeRearm = false;
        }

        public bool IsShootHeld(float lookThreshold)
        {
            Vector2 look = _input.Player.Look.ReadValue<Vector2>();
            bool lookHeld = look.magnitude > lookThreshold;
            bool buttonHeld = _fireAction.IsPressed();
            bool mobileHeld = _mobileInput != null && _mobileInput.FireHeld;

            if (_requireFireNeutralBeforeRearm)
            {
                if (!lookHeld && !buttonHeld && !mobileHeld)
                {
                    _requireFireNeutralBeforeRearm = false;
                }
                else
                {
                    return false;
                }
            }

            return lookHeld ||
                   buttonHeld ||
                   mobileHeld;
        }

        public void ReadShootState(
            float lookThreshold,
            out bool isHeld,
            out bool wasPressed)
        {
            bool lookHeld =
                _input.Player.Look.ReadValue<Vector2>().magnitude > lookThreshold;

            bool buttonHeld = _fireAction.IsPressed();
            bool mobileHeld = _mobileInput != null && _mobileInput.FireHeld;
            bool mobilePressed =
                _mobileInput != null && _mobileInput.ConsumeFirePressed();

            if (_requireFireNeutralBeforeRearm)
            {
                if (!lookHeld && !buttonHeld && !mobileHeld)
                {
                    _requireFireNeutralBeforeRearm = false;
                }
                else
                {
                    _firePressedQueued = false;
                    _previousLookShootHeld = false;
                    isHeld = false;
                    wasPressed = false;
                    return;
                }
            }

            isHeld = lookHeld || buttonHeld || mobileHeld;
            wasPressed =
                _firePressedQueued ||
                (lookHeld && !_previousLookShootHeld) ||
                mobilePressed;

            _firePressedQueued = false;
            _previousLookShootHeld = lookHeld;
        }

        public bool ConsumeReloadPressed()
        {
            bool mobilePressed =
                _mobileInput != null && _mobileInput.ConsumeReloadPressed();

            bool value = _reloadPressedQueued || mobilePressed;
            _reloadPressedQueued = false;
            return value;
        }

        public bool ConsumeNextWeaponPressed()
        {
            bool mobilePressed =
                _mobileInput != null && _mobileInput.ConsumeNextWeaponPressed();

            bool value = _nextWeaponPressedQueued || mobilePressed;
            _nextWeaponPressedQueued = false;
            return value;
        }

        public bool ConsumePreviousWeaponPressed()
        {
            bool mobilePressed =
                _mobileInput != null && _mobileInput.ConsumePreviousWeaponPressed();

            bool value = _previousWeaponPressedQueued || mobilePressed;
            _previousWeaponPressedQueued = false;
            return value;
        }

        public bool ConsumeInteractPressed()
        {
            bool mobilePressed =
                _mobileInput != null && _mobileInput.ConsumeInteractPressed();

            bool value = _interactPressedQueued || mobilePressed;
            _interactPressedQueued = false;
            return value;
        }

        public void ResetGameplayTransientState()
        {
            _firePressedQueued = false;
            _reloadPressedQueued = false;
            _nextWeaponPressedQueued = false;
            _previousWeaponPressedQueued = false;
            _interactPressedQueued = false;
            _previousLookShootHeld = false;

            if (_mobileInput != null)
            {
                _mobileInput.ResetGameplayState();
            }
        }

        public void ClearQueuedActions()
        {
            ResetGameplayTransientState();
        }

        public InputActionRebindingExtensions.RebindingOperation BeginInteractiveRebind(
            string actionName,
            int bindingIndex,
            Action onComplete = null,
            Action onCancel = null)
        {
            InputAction action = FindAction(actionName);

            if (action == null)
            {
                throw new ArgumentException(
                    $"Unknown input action '{actionName}'.",
                    nameof(actionName));
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(bindingIndex));
            }

            action.Disable();

            InputActionRebindingExtensions.RebindingOperation operation = action
                .PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape");

            operation.OnComplete(op =>
            {
                action.Enable();
                SaveBindings();
                op.Dispose();
                onComplete?.Invoke();
            });

            operation.OnCancel(op =>
            {
                action.Enable();
                op.Dispose();
                onCancel?.Invoke();
            });

            operation.Start();
            return operation;
        }

        public string GetBindingDisplayString(
            string actionName,
            int bindingIndex)
        {
            InputAction action = FindAction(actionName);

            if (action == null ||
                bindingIndex < 0 ||
                bindingIndex >= action.bindings.Count)
            {
                return string.Empty;
            }

            return action.GetBindingDisplayString(bindingIndex);
        }

        public void ResetBindingsToDefault()
        {
            foreach (InputAction action in _input.asset)
            {
                action.RemoveAllBindingOverrides();
            }

            _fireAction.RemoveAllBindingOverrides();
            _reloadAction.RemoveAllBindingOverrides();
            _nextWeaponAction.RemoveAllBindingOverrides();
            _previousWeaponAction.RemoveAllBindingOverrides();

            PlayerPrefs.DeleteKey(BindingKeyPrefix + "Generated");
            PlayerPrefs.DeleteKey(BindingKeyPrefix + "Fire");
            PlayerPrefs.DeleteKey(BindingKeyPrefix + "Reload");
            PlayerPrefs.DeleteKey(BindingKeyPrefix + "NextWeapon");
            PlayerPrefs.DeleteKey(BindingKeyPrefix + "PreviousWeapon");
            PlayerPrefs.Save();
        }

        public void Dispose()
        {
            UnsubscribeButtonEvents();

            _input.Disable();
            _fireAction.Disable();
            _reloadAction.Disable();
            _nextWeaponAction.Disable();
            _previousWeaponAction.Disable();

            _fireAction.Dispose();
            _reloadAction.Dispose();
            _nextWeaponAction.Dispose();
            _previousWeaponAction.Dispose();
            _interactAction.Dispose();
            _input.Dispose();
        }

        private void SubscribeButtonEvents()
        {
            _fireAction.performed += HandleFirePerformed;
            _reloadAction.performed += HandleReloadPerformed;
            _nextWeaponAction.performed += HandleNextWeaponPerformed;
            _previousWeaponAction.performed += HandlePreviousWeaponPerformed;
            _interactAction.performed += HandleInteractPerformed;
        }

        private void UnsubscribeButtonEvents()
        {
            _fireAction.performed -= HandleFirePerformed;
            _reloadAction.performed -= HandleReloadPerformed;
            _nextWeaponAction.performed -= HandleNextWeaponPerformed;
            _previousWeaponAction.performed -= HandlePreviousWeaponPerformed;
            _interactAction.performed -= HandleInteractPerformed;
        }

        private void HandleFirePerformed(InputAction.CallbackContext context)
        {
            _firePressedQueued = true;
        }

        private void HandleReloadPerformed(InputAction.CallbackContext context)
        {
            _reloadPressedQueued = true;
        }

        private void HandleNextWeaponPerformed(InputAction.CallbackContext context)
        {
            _nextWeaponPressedQueued = true;
        }

        private void HandlePreviousWeaponPerformed(InputAction.CallbackContext context)
        {
            _previousWeaponPressedQueued = true;
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            _interactPressedQueued = true;
        }

        private InputAction FindAction(string actionName)
        {
            switch (actionName)
            {
                case "Fire":
                    return _fireAction;
                case "Reload":
                    return _reloadAction;
                case "NextWeapon":
                    return _nextWeaponAction;
                case "PreviousWeapon":
                    return _previousWeaponAction;
                case "Interact":
                    return _interactAction;
                default:
                    return _input.asset.FindAction(actionName, false);
            }
        }

        private void SaveBindings()
        {
            PlayerPrefs.SetString(
                BindingKeyPrefix + "Generated",
                _input.asset.SaveBindingOverridesAsJson());

            SaveActionBindings(_fireAction);
            SaveActionBindings(_reloadAction);
            SaveActionBindings(_nextWeaponAction);
            SaveActionBindings(_previousWeaponAction);
            PlayerPrefs.Save();
        }

        private void SaveActionBindings(InputAction action)
        {
            PlayerPrefs.SetString(
                BindingKeyPrefix + action.name,
                action.SaveBindingOverridesAsJson());
        }

        private void LoadBindings()
        {
            string generated =
                PlayerPrefs.GetString(
                    BindingKeyPrefix + "Generated",
                    string.Empty);

            if (!string.IsNullOrEmpty(generated))
            {
                _input.asset.LoadBindingOverridesFromJson(generated);
            }

            LoadActionBindings(_fireAction);
            LoadActionBindings(_reloadAction);
            LoadActionBindings(_nextWeaponAction);
            LoadActionBindings(_previousWeaponAction);
        }

        private void LoadActionBindings(InputAction action)
        {
            string json =
                PlayerPrefs.GetString(
                    BindingKeyPrefix + action.name,
                    string.Empty);

            if (!string.IsNullOrEmpty(json))
            {
                action.LoadBindingOverridesFromJson(json);
            }
        }

        private bool TryGetPointerLookDirection(out Vector2 lookDirection)
        {
            lookDirection = Vector2.zero;

            Mouse mouse = Mouse.current;

            if (mouse == null ||
                _aimCamera == null ||
                _playerTransform == null)
            {
                return false;
            }

            Ray ray =
                _aimCamera.ScreenPointToRay(
                    mouse.position.ReadValue());

            Plane groundPlane =
                new Plane(Vector3.up, _playerTransform.position);

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
