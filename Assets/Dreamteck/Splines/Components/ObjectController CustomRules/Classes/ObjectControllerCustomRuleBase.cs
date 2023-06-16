namespace Dreamteck.Splines
{
    using UnityEngine;

    public class ObjectControllerCustomRuleBase : ScriptableObject
    {
        protected ObjectController currentController;
        protected SplineSample currentSample;
        protected int currentObjectIndex;
        protected int totalObjects;
        protected float currentObjectPercent
        {
            get { return (float)currentObjectIndex / (totalObjects - 1); }
        }

        public void SetContext(ObjectController context, SplineSample sample, int currentObject, int totalObjects)
        {
            currentController = context;
            currentSample = sample;
            this.currentObjectIndex = currentObject;
            this.totalObjects = totalObjects;
        }

        /// <summary>
        /// Implement this method to create custom positioning behaviors. The returned offset should be in local coordinates.
        /// </summary>
        /// <returns>Vector3 offset in local coordinates</returns>
        public virtual Vector3 GetOffset()
        {
            return currentSample.position;
        }

        /// <summary>
        /// Implement this method to create custom rotation behaviors. The returned rotation is in world space
        /// </summary>
        /// <returns>Quaternion rotation in world coordinates</returns>
        public virtual Quaternion GetRotation()
        {
            return currentSample.rotation;
        }

        /// <summary>
        /// Implement this method to create custom scaling behaviors.
        /// </summary>
        /// <returns>Vector3 scale</returns>
        public virtual Vector3 GetScale()
        {
            return Vector3.one * currentSample.size;
        }
    }
}
