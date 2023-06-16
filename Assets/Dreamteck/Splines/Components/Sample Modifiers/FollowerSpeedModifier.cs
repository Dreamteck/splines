namespace Dreamteck.Splines
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class FollowerSpeedModifier : SplineSampleModifier
    {
        [System.Serializable]
        public class SpeedKey : Key
        {
            public enum Mode { Add, Multiply }
            public float speed = 0f;
            public Mode mode = Mode.Add;

            public SpeedKey(double f, double t) : base(f, t)
            {
            }
        }
        public List<SpeedKey> keys = new List<SpeedKey>();

        public FollowerSpeedModifier()
        {
            keys = new List<SpeedKey>();
        }

        public override List<Key> GetKeys()
        {
            List<Key> output = new List<Key>();
            for (int i = 0; i < keys.Count; i++)
            {
                output.Add(keys[i]);
            }
            return output;
        }

        public override void SetKeys(List<Key> input)
        {
            keys = new List<SpeedKey>();
            for (int i = 0; i < input.Count; i++)
            {
                //input[i]._modifier = this;
                keys.Add((SpeedKey)input[i]);
            }
        }

        public void AddKey(double f, double t)
        {
            keys.Add(new SpeedKey(f, t));
        }

        public override void Apply(ref SplineSample result)
        {
        }

        public float GetSpeed(float input, double percent)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                float lerp = keys[i].Evaluate(percent);
                if(keys[i].mode == SpeedKey.Mode.Add)
                {
                    input += keys[i].speed * lerp;
                } else
                {
                    input *= Mathf.Lerp(1f, keys[i].speed, lerp);
                }
            }
            return input;
        }
    }
}
