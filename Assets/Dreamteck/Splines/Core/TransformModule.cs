using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Dreamteck.Splines
{

    [System.Serializable]
    public class TransformModule : ISerializationCallbackReceiver
    {
        public Vector2 offset
        {
            get { return _offset; }
            set
            {
                if (value != _offset)
                {
                    _offset = value;
                    _hasOffset = _offset != Vector2.zero;
                    if (targetUser != null)
                    {
                        targetUser.Rebuild();
                    }
                }
            }
        }
        public Vector3 rotationOffset
        {
            get { return _rotationOffset; }
            set
            {
                if (value != _rotationOffset)
                {
                    _rotationOffset = value;
                    _hasRotationOffset = _rotationOffset != Vector3.zero;
                    if (targetUser != null)
                    {
                        targetUser.Rebuild();
                    }
                }
            }
        }

        public bool hasOffset
        {
            get { return _hasOffset; }
        }

        public bool hasRotationOffset
        {
            get { return _hasRotationOffset; }
        }

        public Vector3 baseScale
        {
            get { return _baseScale; }
            set
            {
                if (value != _baseScale)
                {
                    _baseScale = value;
                    if (targetUser != null)
                    {
                        targetUser.Rebuild();
                    }
                }
            }
        }

        public bool is2D
        {
            get { return _2dMode; }
            set
            {
                _2dMode = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        private bool _hasOffset = false;
        [SerializeField]
        [HideInInspector]
        private bool _hasRotationOffset = false;

        [SerializeField]
        [HideInInspector]
        private Vector2 _offset;
        [SerializeField]
        [HideInInspector]
        private Vector3 _rotationOffset = Vector3.zero;
        [SerializeField]
        [HideInInspector]
        private Vector3 _baseScale = Vector3.one;
        [SerializeField]
        [HideInInspector]
        private bool _2dMode = false;
        public enum VelocityHandleMode { Zero, Preserve, Align, AlignRealistic }
        public VelocityHandleMode velocityHandleMode = VelocityHandleMode.Zero;
        public SplineSample splineResult
        {
            get
            {
                return _splineResult;
            }
            set
            {
                _splineResult = value;
            }
        }
        private SplineSample _splineResult;

        public bool applyPositionX = true;
        public bool applyPositionY = true;
        public bool applyPositionZ = true;
        public bool applyPosition2D = true;
        public bool retainLocalPosition = false;

        public Spline.Direction direction = Spline.Direction.Forward;
        public bool applyPosition
        {
            get
            {
                if (_2dMode)
                {
                    return applyPosition2D;
                }
                return applyPositionX || applyPositionY || applyPositionZ;
            }
            set
            {
                applyPositionX = applyPositionY = applyPositionZ = applyPosition2D = value;
            }
        }

        public bool applyRotationX = true;
        public bool applyRotationY = true;
        public bool applyRotationZ = true;
        public bool applyRotation2D = true;
        public bool retainLocalRotation = false;
        public bool applyRotation
        {
            get
            {
                if (_2dMode)
                {
                    return applyRotation2D;
                }
                return applyRotationX || applyRotationY || applyRotationZ;
            }
            set
            {
                applyRotationX = applyRotationY = applyRotationZ = applyRotation2D = value;
            }
        }

        public bool applyScaleX = false;
        public bool applyScaleY = false;
        public bool applyScaleZ = false;
        public bool applyScale
        {
            get
            {
                return applyScaleX || applyScaleY || applyScaleZ;
            }
            set
            {
                applyScaleX = applyScaleY = applyScaleZ = value;
            }
        }
        [HideInInspector]
        public SplineUser targetUser = null;

        //These are used to save allocations
        private static Vector3 position = Vector3.zero;
        private static Quaternion rotation = Quaternion.identity;

        public void ApplyTransform(Transform input)
        {
            input.position = GetPosition(input.position);
            input.rotation = GetRotation(input.rotation);
            input.localScale = GetScale(input.localScale);
        }

        public void ApplyRigidbody(Rigidbody input)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplyTransform(input.transform);
                return;
            }
#endif
            input.transform.localScale = GetScale(input.transform.localScale);
            input.MovePosition(GetPosition(input.position));
            input.velocity = HandleVelocity(input.velocity);
            input.MoveRotation(GetRotation(input.rotation));
            Vector3 angularVelocity = input.angularVelocity;
            if (applyRotationX)
            {
                angularVelocity.x = 0f;
            }
            if (applyRotationY)
            {
                angularVelocity.y = 0f;
            }
            if (applyRotationZ || applyRotation2D)
            {
                angularVelocity.z = 0f;
            }
            input.angularVelocity = angularVelocity;
        }

        public void ApplyRigidbody2D(Rigidbody2D input)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplyTransform(input.transform);
                input.transform.rotation = Quaternion.AngleAxis(GetRotation(Quaternion.Euler(0f, 0f, input.rotation)).eulerAngles.z, Vector3.forward);
                return;
            }
#endif
            input.transform.localScale = GetScale(input.transform.localScale);
            input.position = GetPosition(input.position);
            input.velocity = HandleVelocity(input.velocity);
            input.rotation = GetRotation(Quaternion.Euler(0f, 0f, input.rotation)).eulerAngles.z;
            if (applyRotationX)
            {
                input.angularVelocity = 0f;
            }
        }

        Vector3 HandleVelocity(Vector3 velocity)
        {
            Vector3 idealVelocity = Vector3.zero;
            Vector3 direction = Vector3.right;
            switch (velocityHandleMode)
            {
                case VelocityHandleMode.Preserve: idealVelocity = velocity; break;
                case VelocityHandleMode.Align:
                    direction = _splineResult.forward;
                    if (Vector3.Dot(velocity, direction) < 0f) direction *= -1f;
                    idealVelocity = direction * velocity.magnitude; break;
                case VelocityHandleMode.AlignRealistic:
                    direction = _splineResult.forward;
                    if (Vector3.Dot(velocity, direction) < 0f) direction *= -1f;
                    idealVelocity = direction * velocity.magnitude * Vector3.Dot(velocity.normalized, direction); break;
            }
            if (applyPositionX) velocity.x = idealVelocity.x;
            if (applyPositionY) velocity.y = idealVelocity.y;
            if (applyPositionZ) velocity.z = idealVelocity.z;
            return velocity;
        }

        private Vector3 GetPosition(Vector3 inputPosition)
        {
            position = _splineResult.position;
            Vector2 finalOffset = _offset;
            if (finalOffset != Vector2.zero)
            {
                position += _splineResult.right * finalOffset.x * _splineResult.size + _splineResult.up * finalOffset.y * _splineResult.size;
            }
            if (retainLocalPosition)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(position, _splineResult.rotation, Vector3.one);
                Vector3 splineLocalPosition = matrix.inverse.MultiplyPoint3x4(targetUser.transform.position);
                splineLocalPosition.x = applyPositionX ? 0f : splineLocalPosition.x;
                splineLocalPosition.y = applyPositionY ? 0f : splineLocalPosition.y;
                splineLocalPosition.z = applyPositionZ ? 0f : splineLocalPosition.z;
                inputPosition = matrix.MultiplyPoint3x4(splineLocalPosition);
            } else
            {
                if (applyPositionX) inputPosition.x = position.x;
                if (applyPositionY) inputPosition.y = position.y;
                if (applyPositionZ) inputPosition.z = position.z;
            }
            return inputPosition;
        }

        private Quaternion GetRotation(Quaternion inputRotation)
        {
            rotation = Quaternion.LookRotation(_splineResult.forward * (direction == Spline.Direction.Forward ? 1f : -1f), _splineResult.up);
            if (_2dMode)
            {
                if (applyRotation2D)
                {
                    rotation *= Quaternion.Euler(90, -90, 0);
                    inputRotation = Quaternion.AngleAxis(rotation.eulerAngles.z + _rotationOffset.z, Vector3.forward);
                }
                return inputRotation;
            }
            else
            {
                if (_rotationOffset != Vector3.zero)
                {
                    rotation = rotation * Quaternion.Euler(_rotationOffset);
                }
            }

            if (retainLocalRotation)
            {
                Quaternion localRotation = Quaternion.Inverse(rotation) * inputRotation;
                Vector3 targetEuler = localRotation.eulerAngles;
                targetEuler.x = applyRotationX ? 0f : targetEuler.x;
                targetEuler.y = applyRotationY ? 0f : targetEuler.y;
                targetEuler.z = applyRotationZ ? 0f : targetEuler.z;
                inputRotation = rotation * Quaternion.Euler(targetEuler);
            } else
            {
                if (!applyRotationX || !applyRotationY || !applyRotationZ)
                {
                    Vector3 targetEuler = rotation.eulerAngles;
                    Vector3 sourceEuler = inputRotation.eulerAngles;
                    if (!applyRotationX) targetEuler.x = sourceEuler.x;
                    if (!applyRotationY) targetEuler.y = sourceEuler.y;
                    if (!applyRotationZ) targetEuler.z = sourceEuler.z;
                    inputRotation.eulerAngles = targetEuler;
                }
                else 
                {
                    inputRotation = rotation;
                }
            }

            return inputRotation;
        }

        private Vector3 GetScale(Vector3 inputScale)
        {
            if (applyScaleX) inputScale.x = _baseScale.x * _splineResult.size;
            if (applyScaleY) inputScale.y = _baseScale.y * _splineResult.size;
            if (applyScaleZ) inputScale.z = _baseScale.z * _splineResult.size;
            return inputScale;
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            _hasRotationOffset = _rotationOffset != Vector3.zero;
            _hasOffset = _offset != Vector2.zero;
        }
    }
}
