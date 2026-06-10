using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleBurst : MonoBehaviour
    {
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private int _count = 20;
        [SerializeField] private float _lifetime = 0.5f;
        [SerializeField] private float _speed = 4f;
        [SerializeField] private float _size = 0.3f;
        [SerializeField] private float _radius = 0.1f;
        [SerializeField] private bool _playOnAwake = false;
        [SerializeField] private bool _destroyOnFinish = false;
        [SerializeField] private bool _worldSpace = false;
        [SerializeField] private Material _material;

        private ParticleSystem _system;

        private void Awake()
        {
            _system = GetComponent<ParticleSystem>();
            Configure();
        }

        private void Start()
        {
            if (_playOnAwake)
            {
                _system.Play();
            }
        }

        public void Play()
        {
            _system.Play();
        }

        private void Configure()
        {
            ParticleSystem.MainModule main = _system.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = _lifetime;
            main.startSpeed = _speed;
            main.startSize = _size;
            main.startColor = _color;
            main.gravityModifier = 0f;
            main.maxParticles = Mathf.Max(_count, 32);
            main.scalingMode = ParticleSystemScalingMode.Local;

            if (_worldSpace)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }
            else
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }

            if (_destroyOnFinish)
            {
                main.stopAction = ParticleSystemStopAction.Destroy;
            }
            else
            {
                main.stopAction = ParticleSystemStopAction.None;
            }

            ParticleSystem.EmissionModule emission = _system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];
            bursts[0] = new ParticleSystem.Burst(0f, (short)_count);
            emission.SetBursts(bursts);

            ParticleSystem.ShapeModule shape = _system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = _radius;

            ParticleSystemRenderer systemRenderer = GetComponent<ParticleSystemRenderer>();
            if (systemRenderer != null)
            {
                systemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                systemRenderer.alignment = ParticleSystemRenderSpace.View;

                if (_material != null)
                {
                    systemRenderer.sharedMaterial = _material;
                }
            }

            _system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
