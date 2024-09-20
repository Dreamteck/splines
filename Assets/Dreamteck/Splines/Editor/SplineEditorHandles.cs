namespace Dreamteck.Splines.Editor
{
    using UnityEditor;
    using UnityEngine;

    public static class SplineEditorHandles
    {
        public static bool SliderButton(Vector3 position, bool drawHandle, Color color, float size)
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            Vector3 localPos = cam.transform.InverseTransformPoint(position);
            if (localPos.z < 0f) return false;

            size *= HandleUtility.GetHandleSize(position);
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(position);
            Vector2 screenRectBase = HandleUtility.WorldToGUIPoint(position - cam.transform.right * size + cam.transform.up * size);
            Rect rect = new Rect(screenRectBase.x, screenRectBase.y, (screenPos.x - screenRectBase.x) * 2f, (screenPos.y - screenRectBase.y) * 2f);
            if (drawHandle)
            {
                Color previousColor = Handles.color;
                Handles.color = color;
                Handles.RectangleHandleCap(0, position, Quaternion.LookRotation(-cam.transform.forward), HandleUtility.GetHandleSize(position) * 0.1f, EventType.Repaint);
                Handles.color = previousColor;
            }
            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CircleButton(Vector3 position, Quaternion rotation, float size, float clickableAreaMultiplier, Color color)
        {
            Color prev = Handles.color;
            bool result = false;
            Handles.color = color;
            result = Handles.Button(position, rotation, size, size * clickableAreaMultiplier, Handles.CircleHandleCap);
            Handles.color = prev;
            return result;
        }

        public static Vector3 FreeMoveRectangle(Vector3 position, float size) 
        {
#if UNITY_2022_1_OR_NEWER
            return Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.CircleHandleCap);
#else
            return Handles.FreeMoveHandle(position, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
#endif
        }

        public static Vector3 FreeMoveCircle(Vector3 position, float size)
        {
#if UNITY_2022_1_OR_NEWER
            return Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.CircleHandleCap);
#else
            return Handles.FreeMoveHandle(position, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
#endif
        }

        public static Vector3 FreeMoveHandle(Vector3 position, float size, Vector3 snap, Handles.CapFunction capFunction)
        {
#if UNITY_2022_1_OR_NEWER
            return Handles.FreeMoveHandle(position, size, snap, capFunction);
#else
            return Handles.FreeMoveHandle(position, Quaternion.identity, size, snap, capFunction);
#endif
        }

        public static void DrawPoint(Vector3 position, bool selected)
        {
            DrawPoint(position, selected, Color.white);
        }

        public static void DrawPoint(Vector3 position, bool selected, Color tintColor)
        {
            if (selected)
            {
                Handles.color = SplinePrefs.highlightColor * tintColor;
                Handles.DrawSolidDisc(position, -SceneView.currentDrawingSceneView.camera.transform.forward, HandleUtility.GetHandleSize(position) * 0.16f);
            }

            Handles.color = SplinePrefs.outlineColor * tintColor;
            Handles.DrawSolidDisc(position, -SceneView.currentDrawingSceneView.camera.transform.forward, HandleUtility.GetHandleSize(position) * 0.12f);
            Handles.color = SplinePrefs.defaultColor * tintColor;
            Handles.DrawSolidDisc(position, -SceneView.currentDrawingSceneView.camera.transform.forward, HandleUtility.GetHandleSize(position) * 0.09f);
            Handles.color = Color.white;
        }

        public static void DrawSolidSphere(Vector3 position, float radius)
        {
            Handles.SphereHandleCap(0, position, Quaternion.identity, radius, EventType.Repaint);
        }

        public static void DrawCircle(Vector3 position, Quaternion rotation, float radius)
        {
            Handles.CircleHandleCap(0, position, rotation, radius, EventType.Repaint);
        }

        public static void DrawRectangle(Vector3 position, Quaternion rotation, float size)
        {
            Handles.RectangleHandleCap(0, position, rotation, size, EventType.Repaint);
        }

        public static void DrawArrowCap(Vector3 position, Quaternion rotation, float size)
        {
            Handles.ArrowHandleCap(0, position, rotation, size, EventType.Repaint);
        }

        public static bool HoverArea(Vector3 position, float size)
        {
            Camera cam = SceneView.currentDrawingSceneView.camera;
            Vector3 localPos = cam.transform.InverseTransformPoint(position);
            if (localPos.z < 0f) return false;

            size *= HandleUtility.GetHandleSize(position);
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(position);
            Vector2 screenRectBase = HandleUtility.WorldToGUIPoint(position - cam.transform.right * size + cam.transform.up * size);
            Rect rect = new Rect(screenRectBase.x, screenRectBase.y, (screenPos.x - screenRectBase.x) * 2f, (screenPos.y - screenRectBase.y) * 2f);
            if (rect.Contains(Event.current.mousePosition)) return true;
            else return false;
        }
    }
}

