using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines.Editor;

namespace Dreamteck.Splines.Primitives
{
    public class SpiralEditor : PrimitiveEditor
    {

        public override string GetName()
        {
            return "Spiral";
        }

        public override void Open(DreamteckSplinesEditor editor)
        {
            base.Open(editor);
            primitive = new Spiral();
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            Spiral spiral = (Spiral)primitive;
            spiral.clockwise = EditorGUILayout.Toggle("Clockwise", spiral.clockwise);
            spiral.curve = EditorGUILayout.CurveField("Radius Interpolation", spiral.curve);
            spiral.startRadius = EditorGUILayout.FloatField("Start Radius", spiral.startRadius);
            spiral.endRadius = EditorGUILayout.FloatField("End Radius", spiral.endRadius);
            spiral.stretch = EditorGUILayout.FloatField("Stretch", spiral.stretch);
            spiral.iterations = EditorGUILayout.IntField("Iterations", spiral.iterations);
        }

    }
}
