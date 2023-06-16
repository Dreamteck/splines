namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class TransformModuleEditor : SplineUserSubEditor
    {
        private TransformModule motionApplier;
        private string[] toolStrings = new string[] { "3D", "2D" };

        public TransformModuleEditor(SplineUser user, SplineUserEditor parent, TransformModule input) : base(user, parent)
        {
            title = "Motion";
            motionApplier = input;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            EditorGUI.indentLevel = 1;
     
            int selected = GUILayout.Toolbar(motionApplier.is2D ? 1 : 0, toolStrings);
            motionApplier.is2D = selected == 1;

            if (motionApplier.is2D)
            {
                motionApplier.applyPosition2D = EditorGUILayout.Toggle("Apply Position", motionApplier.applyPosition2D);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Position", GUILayout.Width(EditorGUIUtility.labelWidth));
                motionApplier.applyPositionX = EditorGUILayout.Toggle(motionApplier.applyPositionX, GUILayout.Width(30));
                GUILayout.Label("X", GUILayout.Width(20));
                motionApplier.applyPositionY = EditorGUILayout.Toggle(motionApplier.applyPositionY, GUILayout.Width(30));
                GUILayout.Label("Y", GUILayout.Width(20));
                motionApplier.applyPositionZ = EditorGUILayout.Toggle(motionApplier.applyPositionZ, GUILayout.Width(30));
                GUILayout.Label("Z", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 150;
                motionApplier.retainLocalPosition = EditorGUILayout.Toggle("Retain Local Position", motionApplier.retainLocalPosition);
                EditorGUIUtility.labelWidth = 0;
                if (motionApplier.retainLocalPosition)
                {
                    EditorGUILayout.HelpBox("Retain Local Position is an experimental feature and will always accumulate an offset error based on how fast the follower is going.", MessageType.Info);
                }
            }

            if (motionApplier.applyPosition)
            {
                EditorGUI.indentLevel = 2;
                if (motionApplier.is2D)
                {
                    Vector2 offset2d = motionApplier.offset;
                    offset2d.y = EditorGUILayout.FloatField("Offset", offset2d.y);
                    motionApplier.offset = offset2d;
                } else
                {
                    motionApplier.offset = EditorGUILayout.Vector2Field("Offset", motionApplier.offset);
                }
                
            }
            EditorGUI.indentLevel = 1;

            if (motionApplier.is2D)
            {
                motionApplier.applyRotation2D = EditorGUILayout.Toggle("Apply Rotation", motionApplier.applyRotation2D);
            } else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Rotation", GUILayout.Width(EditorGUIUtility.labelWidth));
                motionApplier.applyRotationX = EditorGUILayout.Toggle(motionApplier.applyRotationX, GUILayout.Width(30));
                GUILayout.Label("X", GUILayout.Width(20));
                motionApplier.applyRotationY = EditorGUILayout.Toggle(motionApplier.applyRotationY, GUILayout.Width(30));
                GUILayout.Label("Y", GUILayout.Width(20));
                motionApplier.applyRotationZ = EditorGUILayout.Toggle(motionApplier.applyRotationZ, GUILayout.Width(30));
                GUILayout.Label("Z", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = 150; 
                motionApplier.retainLocalRotation = EditorGUILayout.Toggle("Retain Local Rotation", motionApplier.retainLocalRotation);
                EditorGUIUtility.labelWidth = 0;
                if (motionApplier.retainLocalRotation)
                {
                    EditorGUILayout.HelpBox("Retain Local Rotation is an experimental feature and will always accumulate an offset error based on how fast the follower is going.", MessageType.Info);
                }
            }

            if (motionApplier.applyRotation)
            {
                EditorGUI.indentLevel = 2;
                if (motionApplier.is2D)
                {
                    Vector3 rot2d = motionApplier.rotationOffset;
                    rot2d.z = EditorGUILayout.FloatField("Offset", rot2d.z);
                    motionApplier.rotationOffset = rot2d;
                } else
                {
                    motionApplier.rotationOffset = EditorGUILayout.Vector3Field("Offset", motionApplier.rotationOffset);
                }
            }
            EditorGUI.indentLevel = 1;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale", GUILayout.Width(EditorGUIUtility.labelWidth));
            motionApplier.applyScaleX = EditorGUILayout.Toggle(motionApplier.applyScaleX, GUILayout.Width(30));
            GUILayout.Label("X", GUILayout.Width(20));
            motionApplier.applyScaleY = EditorGUILayout.Toggle(motionApplier.applyScaleY, GUILayout.Width(30));
            GUILayout.Label("Y", GUILayout.Width(20));
            motionApplier.applyScaleZ = EditorGUILayout.Toggle(motionApplier.applyScaleZ, GUILayout.Width(30));
            GUILayout.Label("Z", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            if (motionApplier.applyScale)
            {
                EditorGUI.indentLevel = 2;
                motionApplier.baseScale = EditorGUILayout.Vector3Field("Base scale", motionApplier.baseScale);
            }

            motionApplier.velocityHandleMode = (TransformModule.VelocityHandleMode)EditorGUILayout.EnumPopup("Velocity Mode", motionApplier.velocityHandleMode);
            EditorGUI.indentLevel = 0;
        }
    }
}
