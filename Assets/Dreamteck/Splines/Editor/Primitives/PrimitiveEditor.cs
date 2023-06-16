namespace Dreamteck.Splines.Primitives
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using Dreamteck.Splines.Editor;

    [System.Serializable]
    public class PrimitiveEditor
    {
        [System.NonSerialized]
        protected DreamteckSplinesEditor editor;
        [System.NonSerialized]
        public Vector3 origin = Vector3.zero;

        protected SplinePrimitive primitive = new SplinePrimitive();

        public virtual string GetName()
        {
            return "Primitive";
        }

        public virtual void Open(DreamteckSplinesEditor editor)
        {
            this.editor = editor;
            primitive.is2D = editor.is2D;
            primitive.Calculate();
        }

        public void Draw()
        {
            EditorGUI.BeginChangeCheck();
            OnGUI();
            if (EditorGUI.EndChangeCheck())
            {
                Update();
            }
        }

        public void Update()
        {
            primitive.is2D = editor.is2D;
            primitive.Calculate();
            editor.SetPointsArray(primitive.GetPoints());
            editor.SetSplineType(primitive.GetSplineType());
            editor.SetSplineClosed(primitive.GetIsClosed());
            editor.ApplyModifiedProperties(true);
        }

        protected virtual void OnGUI()
        {
            primitive.is2D = editor.is2D;
            primitive.offset = EditorGUILayout.Vector3Field("Offset", primitive.offset);
            if (editor.is2D)
            {
                float rot = primitive.rotation.z;
                rot = EditorGUILayout.FloatField("Rotation", rot);
                primitive.rotation = new Vector3(0f, 0f, rot);
            }
             else primitive.rotation = EditorGUILayout.Vector3Field("Rotation", primitive.rotation);
        }
    }
}
