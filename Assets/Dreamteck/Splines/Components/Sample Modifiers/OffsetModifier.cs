namespace Dreamteck.Splines
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class OffsetModifier : SplineSampleModifier
    {
        [System.Serializable]
        public class OffsetKey : Key
        {
            public Vector2 offset = Vector2.zero;
            public OffsetKey(Vector2 o, double f, double t) : base(f, t)
            {
                offset = o;
            }
        }

        public OffsetKey[] keys = new OffsetKey[0];

        public OffsetModifier()
        {
            keys = new OffsetKey[0];
        }

        public override List<Key> GetKeys()
        {
            return new List<Key>(keys);
        }

        public override void SetKeys(List<Key> input)
        {
            keys = new OffsetKey[input.Count];
            for (int i = 0; i < input.Count; i++)
            {
                keys[i] = (OffsetKey)input[i];
            }
            base.SetKeys(input);
        }

        public void AddKey(Vector2 offset, double f, double t)
        {
            ArrayUtility.Add(ref keys, new OffsetKey(offset, f, t));
        }

        public override void Apply(ref SplineSample result)
        {
            if (keys.Length == 0) return;
            base.Apply(ref result);
            Vector2 offset = Evaluate(result.percent);
            result.position += result.right * offset.x + result.up * offset.y;
        }

        Vector2 Evaluate(double time)
        {
            if (keys.Length == 0) return Vector2.zero;
            Vector2 offset = Vector2.zero;
            for (int i = 0; i < keys.Length; i++)
            {
                offset += keys[i].offset * keys[i].Evaluate(time);
            }
            return offset * blend;
        }
    }
}
