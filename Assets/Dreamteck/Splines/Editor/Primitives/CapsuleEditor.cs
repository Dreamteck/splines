using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines.Editor;

namespace Dreamteck.Splines.Primitives
{
    public class CapsuleEditor : PrimitiveEditor
    {
        public override string GetName()
        {
            return "Capsule";
        }

        public override void Open(DreamteckSplinesEditor editor)
        {
            base.Open(editor);
            primitive = new Capsule();
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            Capsule capsule = (Capsule)primitive;
            capsule.radius = EditorGUILayout.FloatField("Radius", capsule.radius);
            capsule.height = EditorGUILayout.FloatField("Height", capsule.height);
        }
    }
}
