using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Splines
{
    [System.Serializable]
    public class RotationModifier : SplineSampleModifier
    {
        [System.Serializable]
        public class RotationKey : Key
        {
            public bool useLookTarget = false;
            public Transform target = null;
            public Vector3 rotation = Vector3.zero;

            public RotationKey(Vector3 rotation, double f, double t) : base(f, t)
            {
                this.rotation = rotation;
            }
        }

        public RotationKey[] keys = new RotationKey[0];

        public RotationModifier()
        {
            keys = new RotationKey[0];
        }

        public override List<Key> GetKeys()
        {
            return new List<Key>(keys);
        }

        public override void SetKeys(List<Key> input)
        {
            keys = new RotationKey[input.Count];
            for (int i = 0; i < input.Count; i++)
            {
                keys[i] = (RotationKey)input[i];
            }
            base.SetKeys(input);
        }

        public void AddKey(Vector3 rotation, double f, double t)
        {
            ArrayUtility.Add(ref keys, new RotationKey(rotation, f, t));
        }

        public override void Apply(ref SplineSample result)
        {
            if (keys.Length == 0) return;
            base.Apply(ref result);

            Quaternion offset = Quaternion.identity, look = result.rotation;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].useLookTarget && keys[i].target != null)
                {
                    Quaternion lookDir = Quaternion.LookRotation(keys[i].target.position - result.position);
                    look = Quaternion.Slerp(look, lookDir, keys[i].Evaluate(result.percent) * blend);
                }
                else
                {
                    Quaternion euler = Quaternion.Euler(keys[i].rotation.x, keys[i].rotation.y, keys[i].rotation.z);
                    offset = Quaternion.Slerp(offset, offset * euler, keys[i].Evaluate(result.percent) * blend);
                }
            }
            Quaternion rotation = look * offset;
            Vector3 invertedNormal = Quaternion.Inverse(result.rotation) * result.up;
            result.forward = rotation * Vector3.forward;
            result.up = rotation * invertedNormal;
        }
    }
}
