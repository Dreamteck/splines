using UnityEngine;
using System.Collections;
using UnityEditor;
using Dreamteck.Splines.Editor;

namespace Dreamteck.Splines.Primitives
{
    public class RectangleEditor : PrimitiveEditor
    {
        public override string GetName()
        {
            return "Rectangle";
        }

        public override void Open(DreamteckSplinesEditor editor)
        {
            base.Open(editor);
            primitive = new Rectangle();
            primitive.offset = origin;
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            Rectangle rect = (Rectangle)primitive;
            rect.size = EditorGUILayout.Vector2Field("Size", rect.size);
        }
    }
}
