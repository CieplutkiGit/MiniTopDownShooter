using System;
using Application.Weapons;
using Application.Workshop;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Workshop.Presentation
{
    /// <summary>
    /// Visual-only weapon preview and inspection view for the modular weapon workshop.
    /// Strictly non-combat: never fires, never interacts with health or damage affiliations.
    /// Supports exploded view animation using unscaled delta time and 3D slot selection highlighting.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponPreviewView : MonoBehaviour, IWeaponPreviewView
    {
        [Header("Components")]
        [SerializeField] private WeaponModelAssembler _assembler;
        [SerializeField] private WeaponVisualProfile _visualProfile;

        [Header("Rig & Camera")]
        [Tooltip("Dedicated camera for rendering preview/inspection.")]
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private RectTransform _previewSurface;

        [Tooltip("Transform root rotated by turntable or user inspection dragging.")]
        [SerializeField] private Transform _inspectionRigRoot;

        [SerializeField] private bool _autoRotateTurntable = false;
        [SerializeField] private float _turntableSpeed = 20f;

        [Header("Exploded View")]
        [Tooltip("Duration of the explosion/collapse transition in unscaled seconds.")]
        [SerializeField] private float _explosionDuration = 0.35f;

        [Header("Highlighting")]
        [SerializeField] private Color _highlightColor = new Color(0.2f, 0.8f, 1f, 1f);

        // State
        private WeaponBuild _currentBuild;
        private string _selectedSlotId;
        private bool _isExploded;
        private float _explosionProgress = 0f;
        private float _targetExplosionProgress = 0f;

        private MaterialPropertyBlock _highlightPropertyBlock;

        public event Action<string> SlotSelected;

        public WeaponModelAssembler Assembler
        {
            get => _assembler;
            set => _assembler = value;
        }

        public WeaponVisualProfile VisualProfile
        {
            get => _visualProfile;
            set => _visualProfile = value;
        }

        public Camera PreviewCamera
        {
            get => _previewCamera;
            set => _previewCamera = value;
        }

        public Transform InspectionRigRoot
        {
            get => _inspectionRigRoot;
            set => _inspectionRigRoot = value;
        }

        public bool AutoRotateTurntable
        {
            get => _autoRotateTurntable;
            set => _autoRotateTurntable = value;
        }

        public float TurntableSpeed
        {
            get => _turntableSpeed;
            set => _turntableSpeed = value;
        }

        public float ExplosionDuration
        {
            get => _explosionDuration;
            set => _explosionDuration = value;
        }

        public Color HighlightColor
        {
            get => _highlightColor;
            set => _highlightColor = value;
        }

        public WeaponBuild CurrentBuild => _currentBuild;
        public string SelectedSlot => _selectedSlotId;
        public bool IsExploded => _isExploded;
        public float ExplosionProgress => _explosionProgress;

        private void Awake()
        {
            SanitizeCombatComponents();
            EnsureComponents();
        }

        private void OnValidate()
        {
            SanitizeCombatComponents();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // 1. Advance explosion animation using unscaled delta time
            StepAnimation(dt);

            // 2. Turntable rotation
            if (_autoRotateTurntable && _inspectionRigRoot != null)
            {
                _inspectionRigRoot.Rotate(Vector3.up, _turntableSpeed * dt, Space.World);
            }

            // 3. User interaction raycast selection
            HandleInputRaycast();
        }

        /// <summary>
        /// Ensures no combat or damage components can ever exist on this preview hierarchy.
        /// </summary>
        public void SanitizeCombatComponents()
        {
            // Remove Gun if attached
            var gun = GetComponent<Gun>();
            if (gun != null)
            {
                Debug.LogWarning("[WeaponPreviewView] Removed forbidden Gun component from preview view.");
                DestroyImmediate(gun);
            }

            // Remove DamageAffiliation if attached
            var affiliation = GetComponent<DamageAffiliation>();
            if (affiliation != null)
            {
                Debug.LogWarning("[WeaponPreviewView] Removed forbidden DamageAffiliation component from preview view.");
                DestroyImmediate(affiliation);
            }

            // Remove any weapon delivery components
            var deliveries = GetComponents<MonoBehaviour>();
            for (int i = deliveries.Length - 1; i >= 0; i--)
            {
                if (deliveries[i] is IWeaponDelivery)
                {
                    Debug.LogWarning($"[WeaponPreviewView] Removed forbidden IWeaponDelivery component '{deliveries[i].GetType().Name}' from preview view.");
                    DestroyImmediate(deliveries[i]);
                }
            }
        }

        /// <summary>
        /// Displays the visual representation of the provided weapon build.
        /// </summary>
        public void ShowBuild(WeaponBuild build)
        {
            SanitizeCombatComponents();
            EnsureComponents();

            _currentBuild = build;

            if (_assembler != null && _visualProfile != null)
            {
                _assembler.Assemble(build, _visualProfile);
            }

            // Restore explosion pose if currently exploded
            ApplyExplosionToSockets(_explosionProgress);

            // Restore slot highlight if a slot is currently selected
            if (!string.IsNullOrEmpty(_selectedSlotId))
            {
                ApplyHighlight(_selectedSlotId, true);
            }
        }

        /// <summary>
        /// Highlights the visual part meshes belonging to the specified slot ID.
        /// </summary>
        public void SelectSlot(string slotId)
        {
            // Clear previous highlight
            if (!string.IsNullOrEmpty(_selectedSlotId))
            {
                ApplyHighlight(_selectedSlotId, false);
            }

            _selectedSlotId = slotId;

            // Apply new highlight
            if (!string.IsNullOrEmpty(_selectedSlotId))
            {
                ApplyHighlight(_selectedSlotId, true);
            }
        }

        /// <summary>
        /// Programmatically triggers slot selection and fires the SlotSelected event.
        /// </summary>
        public void TriggerSlotSelected(string slotId)
        {
            SelectSlot(slotId);
            SlotSelected?.Invoke(slotId);
        }

        /// <summary>
        /// Sets whether the weapon is in exploded inspection mode.
        /// Smoothly transitions between assembled and exploded poses using unscaled time.
        /// </summary>
        public void SetExploded(bool exploded)
        {
            _isExploded = exploded;
            _targetExplosionProgress = exploded ? 1f : 0f;
        }

        /// <summary>
        /// Immediately sets the exploded state without animation.
        /// </summary>
        public void SetExplodedImmediate(bool exploded)
        {
            _isExploded = exploded;
            _explosionProgress = exploded ? 1f : 0f;
            _targetExplosionProgress = _explosionProgress;
            ApplyExplosionToSockets(_explosionProgress);
        }

        /// <summary>
        /// Advances the explosion animation step by step using unscaled delta time.
        /// </summary>
        public void StepAnimation(float unscaledDeltaTime)
        {
            if (Mathf.Approximately(_explosionProgress, _targetExplosionProgress))
            {
                return;
            }

            float speed = _explosionDuration > 0.0001f ? (1f / _explosionDuration) : 1000f;
            _explosionProgress = Mathf.MoveTowards(_explosionProgress, _targetExplosionProgress, unscaledDeltaTime * speed);

            ApplyExplosionToSockets(_explosionProgress);
        }

        /// <summary>
        /// Performs raycast inspection against weapon parts and raises SlotSelected if a part is hit.
        /// </summary>
        public bool HandleRaycast(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var target = hit.collider.GetComponentInParent<PartClickTarget>();
                if (target != null && !string.IsNullOrEmpty(target.SlotId))
                {
                    TriggerSlotSelected(target.SlotId);
                    return true;
                }
            }

            return false;
        }

        private void ApplyExplosionToSockets(float progress)
        {
            if (_assembler == null) return;

            float smoothedT = Mathf.SmoothStep(0f, 1f, progress);

            foreach (var kvp in _assembler.ActivePartData)
            {
                string slotId = kvp.Key;
                PartVisualData data = kvp.Value;

                Transform socket = _assembler.GetSocket(slotId);
                if (socket == null || data == null) continue;

                // Always calculate directly from stored baseline assembled pose -> ZERO drift
                Vector3 baselinePos = data.AssembledLocalPosition;
                Vector3 targetPos = baselinePos + data.ExplodedLocalOffset;
                socket.localPosition = Vector3.Lerp(baselinePos, targetPos, smoothedT);
            }
        }

        private void ApplyHighlight(string slotId, bool highlight)
        {
            if (_assembler == null) return;

            GameObject partObj = _assembler.GetPartObject(slotId);
            if (partObj == null) return;

            var renderers = partObj.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return;

            if (highlight)
            {
                if (_highlightPropertyBlock == null)
                {
                    _highlightPropertyBlock = new MaterialPropertyBlock();
                }

                _highlightPropertyBlock.SetColor("_BaseColor", _highlightColor);
                _highlightPropertyBlock.SetColor("_Color", _highlightColor);
                _highlightPropertyBlock.SetColor("_EmissionColor", _highlightColor * 0.5f);

                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].SetPropertyBlock(_highlightPropertyBlock);
                }
            }
            else
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].SetPropertyBlock(null);
                }
            }
        }

        private void HandleInputRaycast()
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                Camera cam = _previewCamera != null ? _previewCamera : Camera.main;
                if (cam != null)
                {
                    Vector2 position = pointer.position.ReadValue();
                    Ray ray;
                    if (_previewSurface != null)
                    {
                        if (!_previewSurface.gameObject.activeInHierarchy ||
                            !RectTransformUtility.RectangleContainsScreenPoint(_previewSurface, position)) return;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(_previewSurface, position, null, out var local);
                        var rect = _previewSurface.rect;
                        ray = cam.ViewportPointToRay(new Vector3((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height, 0));
                    }
                    else ray = cam.ScreenPointToRay(position);
                    HandleRaycast(ray);
                }
            }
        }

        private void EnsureComponents()
        {
            if (_assembler == null)
            {
                _assembler = GetComponentInChildren<WeaponModelAssembler>();
                if (_assembler == null)
                {
                    _assembler = gameObject.AddComponent<WeaponModelAssembler>();
                }
            }

            if (_inspectionRigRoot == null)
            {
                _inspectionRigRoot = transform;
            }
        }
    }
}
