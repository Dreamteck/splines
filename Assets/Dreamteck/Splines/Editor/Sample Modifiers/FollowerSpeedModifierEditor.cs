namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;

    public class FollowerSpeedModifierEditor : SplineSampleModifierEditor
    {
        public bool allowSelection = true;
        private float addTime = 0f;

        public FollowerSpeedModifierEditor(SplineUser user, SplineUserEditor editor) : base(user, editor, "_speedModifier")
        {
            title = "Speed Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add Speed Region"))
            {
                AddKey(addTime - 0.1f, addTime + 0.1f);
                UpdateValues();
            }
        }

        protected override void KeyGUI(SerializedProperty key)
        {
            SerializedProperty speed = key.FindPropertyRelative("speed");
            SerializedProperty mode = key.FindPropertyRelative("mode");
            base.KeyGUI(key);
            EditorGUILayout.PropertyField(mode);
            string text = (mode.intValue == (int)FollowerSpeedModifier.SpeedKey.Mode.Add ? "Add" : "Multiply") + " Speed";
            EditorGUILayout.PropertyField(speed, new GUIContent(text));
        }
    }
}
