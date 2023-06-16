using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines.Editor;

namespace Dreamteck.Splines.Primitives
{
    public class LineEditor : PrimitiveEditor
    {
        public override string GetName()
        {
            return "Line";
        }

        public override void Open(DreamteckSplinesEditor editor)
        {
            base.Open(editor);
            primitive = new Line();
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            Line line = (Line)primitive;
            line.length = EditorGUILayout.FloatField("Length", line.length);
            line.mirror = EditorGUILayout.Toggle("Mirror", line.mirror);
            line.rotation = EditorGUILayout.Vector3Field("Rotation", line.rotation);
            line.segments = EditorGUILayout.IntField("Segments", line.segments);
        }
    }
}
