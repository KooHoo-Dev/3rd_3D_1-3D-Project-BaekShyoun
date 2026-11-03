using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
#endif

namespace Stage1Generated
{
    /// <summary>Scene-view course guide only. No movement, navigation or runtime line renderer.</summary>
    [DisallowMultipleComponent]
    public sealed class Stage1CourseGizmo : MonoBehaviour
    {
        [Tooltip("Closed course points in this object's local coordinates; the final point connects to the first.")]
        public Vector3[] points = new Vector3[0];
        public Color lineColor = Color.cyan;
        [Range(1f, 8f)] public float lineWidth = 3.5f;
        public bool showGuide = true;
        public bool showDirectionArrows = true;
        public bool showLabel = true;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGuide || points == null || points.Length < 3) return;
            Color oldColor = Handles.color;
            CompareFunction oldDepth = Handles.zTest;
            Handles.color = lineColor;
            Handles.zTest = CompareFunction.LessEqual;
            var world = new Vector3[points.Length + 1];
            for (int i = 0; i < points.Length; i++) world[i] = transform.TransformPoint(points[i]);
            world[points.Length] = world[0];
            Handles.DrawAAPolyLine(lineWidth, world);
            if (showDirectionArrows)
            {
                int step = Mathf.Max(1, points.Length / 12);
                for (int i = 0; i < points.Length; i += step)
                {
                    Vector3 forward = (world[(i + 1) % points.Length] - world[i]).normalized;
                    Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                    Vector3 tip = world[i] + forward * .65f;
                    Handles.DrawAAPolyLine(lineWidth, tip - forward * 1.1f + side * .4f, tip, tip - forward * 1.1f - side * .4f);
                }
            }
            Handles.DrawWireDisc(world[0], Vector3.up, .25f);
            if (showLabel)
            {
                var style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = lineColor;
                Handles.Label(world[0] + Vector3.up * .6f, gameObject.name, style);
            }
            Handles.color = oldColor;
            Handles.zTest = oldDepth;
        }
#endif
    }
}
