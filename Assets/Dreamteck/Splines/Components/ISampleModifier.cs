using UnityEngine;

namespace Dreamteck.Splines
{
    public interface ISampleModifier
    {
        public void ApplySampleModifiers(ref SplineSample sample);

        public Vector3 GetModifiedSamplePosition(ref SplineSample sample);
    }
}
