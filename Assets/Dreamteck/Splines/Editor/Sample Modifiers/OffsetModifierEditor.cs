namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;

    public class OffsetModifierEditor : SplineSampleModifierEditor
    {
        public bool allowSelection = true;
        private float addTime = 0f;
        Matrix4x4 matrix = new Matrix4x4();

        public OffsetModifierEditor(SplineUser user, SplineUserEditor editor) : base(user, editor, "_offsetModifier")
        {
            title = "Offset Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add New Offset"))
            {
                AddKey(addTime - 0.1f, addTime + 0.1f);
                UpdateValues();
            }
        }

        protected override void KeyGUI(SerializedProperty key)
        {
            SerializedProperty offset = key.FindPropertyRelative("offset");
            base.KeyGUI(key);
            EditorGUILayout.PropertyField(offset);
        }

        protected override bool KeyHandles(SerializedProperty key, bool edit)
        {
            if (!isOpen) return false;
            bool changed = false;
            bool is2D = user.spline != null && user.spline.is2D;
            SplineSample result = new SplineSample();
            SerializedProperty start = key.FindPropertyRelative("_featherStart");
            SerializedProperty end = key.FindPropertyRelative("_featherEnd");
            SerializedProperty centerStart = key.FindPropertyRelative("_centerStart");
            SerializedProperty centerEnd = key.FindPropertyRelative("_centerEnd");
            SerializedProperty offset = key.FindPropertyRelative("offset");

            float position = GetPosition(start.floatValue, end.floatValue, centerStart.floatValue, centerEnd.floatValue);

            user.spline.Evaluate(position, ref result);
            matrix.SetTRS(result.position, Quaternion.LookRotation(result.forward, result.up), Vector3.one * result.size);
            Vector3 pos = matrix.MultiplyPoint(offset.vector2Value);
            if (is2D)
            {
                Handles.DrawLine(result.position, result.position + result.right * offset.vector2Value.x * result.size);
                Handles.DrawLine(result.position, result.position - result.right * offset.vector2Value.x * result.size);
            }
            else Handles.DrawWireDisc(result.position, result.forward, offset.vector2Value.magnitude * result.size);
            Handles.DrawLine(result.position, pos);

            if (edit)
            {
                Vector3 lastPos = pos;
                pos = SplineEditorHandles.FreeMoveRectangle(pos, HandleUtility.GetHandleSize(pos) * 0.1f);
                if (pos != lastPos)
                {
                    MainPointModule.HoldInteraction();
                    changed = true;
                    pos = matrix.inverse.MultiplyPoint(pos);
                    pos.z = 0f;
                    if (is2D)
                    {
                        offset.vector2Value = Vector2.right * pos.x;
                    }
                    else
                    {
                        offset.vector2Value = pos;
                    }
                }
            }

            return base.KeyHandles(key, edit) || changed;
        }
    }
}
