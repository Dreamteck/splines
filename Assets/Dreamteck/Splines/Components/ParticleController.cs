namespace Dreamteck.Splines
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    [ExecuteInEditMode]
    [AddComponentMenu("Dreamteck/Splines/Users/Particle Controller")]
    public class ParticleController : SplineUser
    {
        public ParticleSystem particleSystemComponent
        {
            get { return _particleSystem; }
            set {
                _particleSystem = value;
                _renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
            }
        }

        [SerializeField]
        [HideInInspector]
        private ParticleSystem _particleSystem;
        private ParticleSystemRenderer _renderer;
        public enum EmitPoint { Beginning, Ending, Random, Ordered }
        public enum MotionType { None, UseParticleSystem, FollowForward, FollowBackward, ByNormal, ByNormalRandomized }
        public enum Wrap { Default, Loop }

        [HideInInspector]
        public bool pauseWhenNotVisible = false;
        [HideInInspector]
        public Vector2 offset = Vector2.zero;
        [HideInInspector]
        public bool volumetric = false;
        [HideInInspector]
        public bool emitFromShell = false;
        [HideInInspector]
        public bool apply3DRotation = false;
        [HideInInspector]
        public Vector2 scale = Vector2.one;
        [HideInInspector]
        public EmitPoint emitPoint = EmitPoint.Beginning;
        [HideInInspector]
        public MotionType motionType = MotionType.UseParticleSystem;
        [HideInInspector]
        public Wrap wrapMode = Wrap.Default;
        [HideInInspector]
        public float minCycles = 1f;
        [HideInInspector]
        public float maxCycles = 1f;

        private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[0];
        private Particle[] _controllers = new Particle[0];
        private int _particleCount = 0;
        private int _birthIndex = 0;
        private List<Vector4> _customParticleData = new List<Vector4>();

        protected override void LateRun()
        {
            if (_particleSystem == null) return;
            if (pauseWhenNotVisible)
            {
                if (_renderer == null)
                {
                    _renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
                }
                if (!_renderer.isVisible) return;
            }

            int maxParticles = _particleSystem.main.maxParticles;
            if (_particles.Length != maxParticles)
            {
                _particles = new ParticleSystem.Particle[maxParticles];
                _customParticleData = new List<Vector4>(maxParticles);
                Particle[] newControllers = new Particle[maxParticles];
                for (int i = 0; i < newControllers.Length; i++)
                {
                    if (i >= _controllers.Length) break;
                    newControllers[i] = _controllers[i];
                }
                _controllers = newControllers;
            }
            _particleCount = _particleSystem.GetParticles(_particles);
            _particleSystem.GetCustomParticleData(_customParticleData, ParticleSystemCustomData.Custom1);

            bool isLocal = _particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local;

            Transform particleSystemTransform = _particleSystem.transform;

            for (int i = 0; i < _particleCount; i++)
            {
                if (_controllers[i] == null)
                {
                    _controllers[i] = new Particle();
                }
                if (isLocal)
                {
                    TransformParticle(ref _particles[i], particleSystemTransform);
                }
                if (_customParticleData[i].w < 1f)
                {
                    OnParticleBorn(i);
                }
                HandleParticle(i);
                if (isLocal)
                {
                    InverseTransformParticle(ref _particles[i], particleSystemTransform);
                }
            }

            _particleSystem.SetCustomParticleData(_customParticleData, ParticleSystemCustomData.Custom1);
            _particleSystem.SetParticles(_particles, _particleCount);
        }

        void TransformParticle(ref ParticleSystem.Particle particle, Transform trs)
        {
            particle.position = trs.TransformPoint(particle.position);
            if (apply3DRotation)
            {

            }
            particle.velocity = trs.TransformDirection(particle.velocity);
        }

        void InverseTransformParticle(ref ParticleSystem.Particle particle, Transform trs)
        {
            particle.position = trs.InverseTransformPoint(particle.position);
            particle.velocity = trs.InverseTransformDirection(particle.velocity);
        }

        protected override void Reset()
        {
            base.Reset();
            updateMethod = UpdateMethod.LateUpdate;
            if (_particleSystem == null) _particleSystem = GetComponent<ParticleSystem>();
        }

        void HandleParticle(int index)
        {
            float lifePercent = _particles[index].remainingLifetime / _particles[index].startLifetime;
            if (motionType == MotionType.FollowBackward || motionType == MotionType.FollowForward || motionType == MotionType.None)
            {
                Evaluate(_controllers[index].GetSplinePercent(wrapMode, _particles[index], motionType), ref evalResult);
                ModifySample(ref evalResult);
                Vector3 resultRight = evalResult.right;
                _particles[index].position = evalResult.position;
                if (apply3DRotation)
                {
                    _particles[index].rotation3D = evalResult.rotation.eulerAngles;
                }
                Vector2 finalOffset = offset;
                if (volumetric)
                {
                    if (motionType != MotionType.None)
                    {
                        finalOffset += Vector2.Lerp(_controllers[index].startOffset, _controllers[index].endOffset, 1f - lifePercent);
                        finalOffset.x *= scale.x;
                        finalOffset.y *= scale.y;
                    } else
                    {
                        finalOffset += _controllers[index].startOffset;
                    }
                }
                _particles[index].position += resultRight * (finalOffset.x * evalResult.size) + evalResult.up * (finalOffset.y * evalResult.size);
                _particles[index].velocity = evalResult.forward;
                _particles[index].startColor = _controllers[index].startColor * evalResult.color;
            }
        }

        private void OnParticleBorn(int index)
        {
            Vector4 custom = _customParticleData[index];
            custom.w = 1;
            _customParticleData[index] = custom;
            double percent = 0.0;
            float emissionRate = Mathf.Lerp(_particleSystem.emission.rateOverTime.constantMin, _particleSystem.emission.rateOverTime.constantMax, 0.5f);
            float expectedParticleCount = emissionRate * _particleSystem.main.startLifetime.constantMax;
            _birthIndex++;
            if (_birthIndex > expectedParticleCount)
            {
                _birthIndex = 0;
            }

            switch (emitPoint)
            {
                case EmitPoint.Beginning: percent = 0f; break;
                case EmitPoint.Ending: percent = 1f; break;
                case EmitPoint.Random: percent = Random.Range(0f, 1f); break;
                case EmitPoint.Ordered: percent = expectedParticleCount > 0 ? (float)_birthIndex / expectedParticleCount : 0f;  break;
            }
            Evaluate(percent, ref evalResult);
            ModifySample(ref evalResult);
            _controllers[index].startColor = _particles[index].startColor;
            _controllers[index].startPercent = percent;

            _controllers[index].cycleSpeed = Random.Range(minCycles, maxCycles);
            Vector2 circle = Vector2.zero;
            if (volumetric)
            {
                if (emitFromShell) circle = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward) * Vector2.right;
                else circle = Random.insideUnitCircle;
            }
            _controllers[index].startOffset = circle * 0.5f;
            _controllers[index].endOffset = Random.insideUnitCircle * 0.5f;


            Vector3 right = Vector3.Cross(evalResult.forward, evalResult.up);
            _particles[index].position = evalResult.position + right * _controllers[index].startOffset.x * evalResult.size * scale.x + evalResult.up * _controllers[index].startOffset.y * evalResult.size * scale.y;

            float forceX = _particleSystem.forceOverLifetime.x.constantMax;
            float forceY = _particleSystem.forceOverLifetime.y.constantMax;
            float forceZ = _particleSystem.forceOverLifetime.z.constantMax;
            if (_particleSystem.forceOverLifetime.randomized)
            {
                forceX = Random.Range(_particleSystem.forceOverLifetime.x.constantMin, _particleSystem.forceOverLifetime.x.constantMax);
                forceY = Random.Range(_particleSystem.forceOverLifetime.y.constantMin, _particleSystem.forceOverLifetime.y.constantMax);
                forceZ = Random.Range(_particleSystem.forceOverLifetime.z.constantMin, _particleSystem.forceOverLifetime.z.constantMax);
            }

            float time = _particles[index].startLifetime - _particles[index].remainingLifetime;
            Vector3 forceDistance = new Vector3(forceX, forceY, forceZ) * 0.5f * (time * time);

            float startSpeed = _particleSystem.main.startSpeed.constantMax;

            if (motionType == MotionType.ByNormal)
            {
                _particles[index].position += evalResult.up * startSpeed * (_particles[index].startLifetime - _particles[index].remainingLifetime);
                _particles[index].position += forceDistance;
                _particles[index].velocity = evalResult.up * startSpeed + new Vector3(forceX, forceY, forceZ) * time;
            }
            else if (motionType == MotionType.ByNormalRandomized)
            {
                Vector3 normal = Quaternion.AngleAxis(Random.Range(0f, 360f), evalResult.forward) * evalResult.up;
                _particles[index].position += normal * startSpeed * (_particles[index].startLifetime - _particles[index].remainingLifetime);
                _particles[index].position += forceDistance;
                _particles[index].velocity = normal * startSpeed + new Vector3(forceX, forceY, forceZ) * time;
            }
            HandleParticle(index);
        }

        public class Particle
        {
            internal Vector2 startOffset = Vector2.zero;
            internal Vector2 endOffset = Vector2.zero;
            internal float cycleSpeed = 0f;
            internal Color startColor = Color.white;
            internal double startPercent = 0.0;

            internal double GetSplinePercent(Wrap wrap, ParticleSystem.Particle particle, MotionType motionType)
            {
                float lifePercent = particle.remainingLifetime / particle.startLifetime;
                if(motionType == MotionType.FollowBackward)
                {
                    lifePercent = 1f - lifePercent;
                }
                switch (wrap)
                {
                    case Wrap.Default: return DMath.Clamp01(startPercent + (1f - lifePercent) * cycleSpeed);
                    case Wrap.Loop:
                        double loopPoint = startPercent + (1.0 - lifePercent) * cycleSpeed;
                        if(loopPoint > 1.0) loopPoint -= Mathf.FloorToInt((float)loopPoint);
                        return loopPoint;
                }
                return 0.0;
            }
        }
    }
}
