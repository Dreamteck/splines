namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;

    public class MeshScaleModifierEditor : SplineSampleModifierEditor
    {
        public bool allowSelection = true;
        private float addTime = 0f;

        public MeshScaleModifierEditor(MeshGenerator user, SplineUserEditor editor, int channelIndex) : base(user, editor, "_channels/["+channelIndex+"]/_scaleModifier")
        {
            title = "Scale Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add New Scale"))
            {
                var key = AddKey(addTime - 0.1f, addTime + 0.1f);
                key.FindPropertyRelative("scale").vector3Value = Vector3.one;
                UpdateValues();
            }
        }

        protected override void KeyGUI(SerializedProperty key)
        {
            SerializedProperty scale = key.FindPropertyRelative("scale");
            base.KeyGUI(key);
            EditorGUILayout.PropertyField(scale);
        }
    }
}
