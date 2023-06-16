namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(SplineFollower), true)]
    [CanEditMultipleObjects]
    public class SplineFollowerEditor : SplineTracerEditor
    {
        SplineSample result = new SplineSample();
        protected SplineFollower[] followers = new SplineFollower[0];
        protected FollowerSpeedModifierEditor speedModifierEditor;

        void OnSetDistance(float distance)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                SplineFollower follower = (SplineFollower)targets[i];
                double travel = follower.Travel(0.0, distance, Spline.Direction.Forward);
                var startPosition = serializedObject.FindProperty("_startPosition");
                startPosition.floatValue = (float)travel;
                follower.SetPercent(travel);
                EditorUtility.SetDirty(follower);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            followers = new SplineFollower[users.Length];
            for (int i = 0; i < followers.Length; i++)
            {
                followers[i] = (SplineFollower)users[i];
            }

            if (followers.Length == 1)
            {
                speedModifierEditor = new FollowerSpeedModifierEditor(followers[0], this);
            }
        }

        protected override void BodyGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Following", EditorStyles.boldLabel);
            SplineFollower follower = (SplineFollower)target;

            SerializedProperty followMode = serializedObject.FindProperty("followMode");
            SerializedProperty preserveUniformSpeedWithOffset = serializedObject.FindProperty("preserveUniformSpeedWithOffset");
            SerializedProperty wrapMode = serializedObject.FindProperty("wrapMode");
            SerializedProperty startPosition = serializedObject.FindProperty("_startPosition");
            SerializedProperty autoStartPosition = serializedObject.FindProperty("autoStartPosition");
            SerializedProperty follow = serializedObject.FindProperty("_follow");
            SerializedProperty direction = serializedObject.FindProperty("_direction");
            SerializedProperty unityOnEndReached = serializedObject.FindProperty("_unityOnEndReached");
            SerializedProperty unityOnBeginningReached = serializedObject.FindProperty("_unityOnBeginningReached");

            EditorGUI.BeginChangeCheck();

            bool lastFollow = follow.boolValue;
            EditorGUILayout.PropertyField(follow);
            if(lastFollow != follow.boolValue)
            {
                if (follow.boolValue)
                {
                    if (autoStartPosition.boolValue)
                    {
                        SplineSample sample = new SplineSample();
                        followers[0].Project(followers[0].transform.position, ref sample);
                        if (Application.isPlaying)
                        {
                            for (int i = 0; i < followers.Length; i++)
                            {
                                followers[i].SetPercent(sample.percent);
                            }
                        }
                    }
                }
            }
            EditorGUILayout.PropertyField(followMode);
            if (followMode.intValue == (int)SplineFollower.FollowMode.Uniform)
            {
                SerializedProperty followSpeed = serializedObject.FindProperty("_followSpeed");

                if(followSpeed.floatValue < 0f)
                {
                    direction.intValue = (int)Spline.Direction.Backward;
                } else if (followSpeed.floatValue > 0f)
                {
                    direction.intValue = (int)Spline.Direction.Forward;
                }

                SerializedProperty motion = serializedObject.FindProperty("_motion");
                SerializedProperty motionHasOffset = motion.FindPropertyRelative("_hasOffset");

                EditorGUILayout.PropertyField(followSpeed, new GUIContent("Follow Speed"));



                if (motionHasOffset.boolValue)
                {
                    EditorGUILayout.PropertyField(preserveUniformSpeedWithOffset, new GUIContent("Preserve Uniform Speed With Offset"));
                }
                if (followers.Length == 1)
                {
                    speedModifierEditor.DrawInspector();
                }
            }
            else
            {
                follower.followDuration = EditorGUILayout.FloatField("Follow duration", follower.followDuration);
            }
            


            EditorGUILayout.PropertyField(wrapMode);


            if (follower.motion.applyRotation)
            {
                follower.applyDirectionRotation = EditorGUILayout.Toggle("Face Direction", follower.applyDirectionRotation);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Start Position", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoStartPosition, new GUIContent("Automatic Start Position"));
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 100f;
            if (!follower.autoStartPosition && !Application.isPlaying)
            {
                float lastStartpos = startPosition.floatValue;
                EditorGUILayout.PropertyField(startPosition, new GUIContent("Start Position"));
                if (GUILayout.Button("Set Distance", GUILayout.Width(85)))
                {
                    DistanceWindow w = EditorWindow.GetWindow<DistanceWindow>(true);
                    w.Init(OnSetDistance, follower.CalculateLength());
                }
            }
            else
            {
                EditorGUILayout.LabelField("Start position", GUILayout.Width(EditorGUIUtility.labelWidth));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(unityOnBeginningReached);
            EditorGUILayout.PropertyField(unityOnEndReached);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                {
                    for (int i = 0; i < followers.Length; i++)
                    {
                        if(followers[i].spline.sampleCount > 0)
                        {
                            if (!followers[i].autoStartPosition)
                            {
                                followers[i].SetPercent(startPosition.floatValue);
                                if (!followers[i].follow) SceneView.RepaintAll();
                            }
                        }
                    }
                }
            }

            int lastDirection = direction.intValue;
            base.BodyGUI();

            if(lastDirection != direction.intValue)
            {
                SerializedProperty followSpeed = serializedObject.FindProperty("_followSpeed");
                if(direction.intValue == (int)Spline.Direction.Forward)
                {
                    followSpeed.floatValue = Mathf.Abs(followSpeed.floatValue);
                } else
                {
                    followSpeed.floatValue = -Mathf.Abs(followSpeed.floatValue);
                }
            }
        }


        protected override void DuringSceneGUI(SceneView currentSceneView)
        {
            base.DuringSceneGUI(currentSceneView);
            SplineFollower user = (SplineFollower)target;
            if (user == null) return;
            if (Application.isPlaying)
            {
                if (!user.follow) DrawResult(user.modifiedResult);
                return;
            }
            if (user.spline == null) return;
            if (user.autoStartPosition)
            {
                user.spline.Project(user.transform.position, ref result, user.clipFrom, user.clipTo);
                DrawResult(result);
            } else if(!user.follow) DrawResult(user.result);

            if (followers.Length == 1)
            {
                speedModifierEditor.DrawScene();
            }

        }
    }
}
