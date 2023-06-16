namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    public class DistanceWindow : EditorWindow
    {
        float distance = 0f;
        DistanceReceiver rcv;
        float length = 0f;
        public delegate void DistanceReceiver(float distance);
        public void Init(DistanceReceiver receiver, float totalLength)
        {
            rcv = receiver;
            length = totalLength;
            titleContent = new GUIContent("Set Distance");
            minSize = maxSize = new Vector2(240, 90);
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return))
            {
                rcv(distance);
                Close();
            }
            distance = EditorGUILayout.FloatField("Distance", distance);
            if (distance < 0f) distance = 0f;
            else if (distance > length) distance = length;
            if (distance > 0f)
            {
                EditorGUILayout.LabelField("Press Enter to set.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.HelpBox("Enter the distance and press Enter. Current spline length: " + length, MessageType.Info);
        }
    }
}
