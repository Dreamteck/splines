namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(SplineTracer), true)]
    public class SplineTracerEditor : SplineUserEditor
    {
        private bool cameraFoldout = false;
        private TransformModuleEditor motionEditor;
        private RenderTexture rt;
        private Texture2D renderCanvas = null;
        private Camera cam;
        SplineTracer[] tracers = new SplineTracer[0];

        public delegate void DistanceReceiver(float distance);

        protected override void OnEnable()
        {
            base.OnEnable();
            SplineTracer tracer = (SplineTracer)target;
            motionEditor = new TransformModuleEditor(tracer, this, tracer.motion);
            tracers = new SplineTracer[targets.Length];
            for (int i = 0; i < tracers.Length; i++)
            {
                tracers[i] = (SplineTracer)targets[i];
            }
        }

        private int GetRTWidth()
        {
            return Mathf.RoundToInt(EditorGUIUtility.currentViewWidth)-50;
        }

        private int GetRTHeight()
        {
            return Mathf.RoundToInt(GetRTWidth()/cam.aspect);
        }

        private void CreateRT()
        {
            if(rt != null)
            {
                DestroyImmediate(rt);
                DestroyImmediate(renderCanvas);
            }
            rt = new RenderTexture(GetRTWidth(), GetRTHeight(), 16, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            renderCanvas = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyImmediate(rt);
        }

        protected override void BodyGUI()
        {
            base.BodyGUI();
            EditorGUILayout.LabelField("Tracing", EditorStyles.boldLabel);
            SplineTracer tracer = (SplineTracer)target;
            serializedObject.Update();
            SerializedProperty useTriggers = serializedObject.FindProperty("useTriggers");
            SerializedProperty triggerGroup = serializedObject.FindProperty("triggerGroup");
            SerializedProperty direction = serializedObject.FindProperty("_direction");
            SerializedProperty physicsMode = serializedObject.FindProperty("_physicsMode");
            SerializedProperty dontLerpDirection = serializedObject.FindProperty("_dontLerpDirection");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useTriggers);
            if (useTriggers.boolValue) EditorGUILayout.PropertyField(triggerGroup);
            EditorGUILayout.PropertyField(direction, new GUIContent("Direction"));
            EditorGUILayout.PropertyField(dontLerpDirection, new GUIContent("Don't Lerp Direction"));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(physicsMode, new GUIContent("Physics Mode"));

            if (tracer.physicsMode == SplineTracer.PhysicsMode.Rigidbody)
            {
                Rigidbody rb = tracer.GetComponent<Rigidbody>();
                if (rb == null) EditorGUILayout.HelpBox("Assign a Rigidbody component.", MessageType.Error);
                else if (rb.interpolation == RigidbodyInterpolation.None && tracer.updateMethod != SplineUser.UpdateMethod.FixedUpdate) EditorGUILayout.HelpBox("Switch to FixedUpdate mode to ensure smooth update for non-interpolated rigidbodies", MessageType.Warning);

            }
            else if (tracer.physicsMode == SplineTracer.PhysicsMode.Rigidbody2D)
            {
                Rigidbody2D rb = tracer.GetComponent<Rigidbody2D>();
                if (rb == null) EditorGUILayout.HelpBox("Assign a Rigidbody2D component.", MessageType.Error);
                else if (rb.interpolation == RigidbodyInterpolation2D.None && tracer.updateMethod != SplineUser.UpdateMethod.FixedUpdate) EditorGUILayout.HelpBox("Switch to FixedUpdate mode to ensure smooth update for non-interpolated rigidbodies", MessageType.Warning);
            }
            if (tracers.Length == 1)
            {
                bool mightBe2d = false;
                if(tracers[0].spline != null)
                {
                    mightBe2d = tracers[0].spline.is2D;
                }
                if (!mightBe2d)
                {
                    mightBe2d = physicsMode.intValue == (int)SplineTracer.PhysicsMode.Rigidbody2D;
                }
                if (!mightBe2d)
                {
                    if(tracer.GetComponentInChildren<SpriteRenderer>() != null)
                    {
                        mightBe2d = true;
                    }
                }
                motionEditor.DrawInspector();

                if (mightBe2d && !tracer.motion.is2D)
                {
                    EditorGUILayout.HelpBox(
                        "The object is possibly set up for 2D but the rotation is applied in 3D. If the intention is for the object to be 2D, switch to 2D in the Motion panel.",
                        MessageType.Warning);
                }

                cameraFoldout = EditorGUILayout.Foldout(cameraFoldout, "Camera preview");
                if (cameraFoldout)
                {
                    if (cam == null)
                    {
                        cam = tracer.GetComponentInChildren<Camera>();
                    }
                    if (cam != null)
                    {
                        if (rt == null || rt.width != GetRTWidth() || rt.height != GetRTHeight()) CreateRT();
                        GUILayout.Box("", GUILayout.Width(rt.width), GUILayout.Height(rt.height));
                        RenderTexture prevTarget = cam.targetTexture;
                        RenderTexture prevActive = RenderTexture.active;
                        CameraClearFlags lastFlags = cam.clearFlags;
                        Color lastColor = cam.backgroundColor;
                        cam.targetTexture = rt;
                        cam.clearFlags = CameraClearFlags.Color;
                        cam.backgroundColor = Color.black;
                        cam.Render();
                        RenderTexture.active = rt;
                        renderCanvas.SetPixels(new Color[renderCanvas.width * renderCanvas.height]);
                        renderCanvas.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                        renderCanvas.Apply();
                        RenderTexture.active = prevActive;
                        cam.targetTexture = prevTarget;
                        cam.clearFlags = lastFlags;
                        cam.backgroundColor = lastColor;
                        GUI.DrawTexture(GUILayoutUtility.GetLastRect(), renderCanvas, ScaleMode.StretchToFill);
                    }
                    else EditorGUILayout.HelpBox("There is no camera attached to the selected object or its children.", MessageType.Info);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < tracers.Length; i++) tracers[i].Rebuild();
            }
        }

        protected override void DuringSceneGUI(SceneView currentSceneView)
        {
            base.DuringSceneGUI(currentSceneView);
            SplineTracer tracer = (SplineTracer)target;
        }

        protected void DrawResult(SplineSample result)
        {
            SplineTracer tracer = (SplineTracer)target;
            Handles.color = Color.white;
            Handles.DrawLine(tracer.transform.position, result.position);
            SplineEditorHandles.DrawSolidSphere(result.position, HandleUtility.GetHandleSize(result.position) * 0.2f);
            Handles.color = Color.blue;
            Handles.DrawLine(result.position, result.position + result.forward * HandleUtility.GetHandleSize(result.position) * 0.5f);
            Handles.color = Color.green;
            Handles.DrawLine(result.position, result.position + result.up * HandleUtility.GetHandleSize(result.position) * 0.5f);
            Handles.color = Color.red;
            Handles.DrawLine(result.position, result.position + result.right * HandleUtility.GetHandleSize(result.position) * 0.5f);
            Handles.color = Color.white;
        }


    }
}
