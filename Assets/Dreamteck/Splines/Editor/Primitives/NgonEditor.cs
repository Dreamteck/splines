using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines.Editor;

namespace Dreamteck.Splines.Primitives
{
    public class NgonEditor : PrimitiveEditor
    {
        public override string GetName()
        {
            return "Ngon";
        }

        public override void Open(DreamteckSplinesEditor editor)
        {
            base.Open(editor);
            primitive = new Ngon();
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            Ngon ngon = (Ngon)primitive;
            ngon.radius = EditorGUILayout.FloatField("Radius", ngon.radius);
            ngon.sides = EditorGUILayout.IntField("Sides", ngon.sides);
            if (ngon.sides < 3) ngon.sides = 3;
        }
    }
}
