using System;
using UnityEngine;

namespace HareAndTortoise.Rigging
{
    // Original bone rotations remain usable. This follows the forearm with a
    // half-angle support bone to reduce pinching around the elbow.
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(10000)]
    public sealed class TurtleElbowSupport : MonoBehaviour
    {
        [Serializable]
        public sealed class Joint
        {
            public Transform forearm;
            public Transform support;
            public Quaternion bindForearm = Quaternion.identity;
            public Quaternion bindSupport = Quaternion.identity;
            public Vector3 hingeAxis = Vector3.right;
        }

        [Tooltip("When enabled, the sliders override forearm animation. Turn off to follow Animator motion.")]
        public bool driveElbows;
        [Range(-10f, 90f)] public float leftBend;
        [Range(-10f, 90f)] public float rightBend;
        [SerializeField, HideInInspector] private bool configured;
        [SerializeField, HideInInspector] private Joint left = new Joint();
        [SerializeField, HideInInspector] private Joint right = new Joint();
        public bool IsConfigured => configured;

        public void Configure(Joint leftJoint, Joint rightJoint)
        {
            left = leftJoint;
            right = rightJoint;
            configured = left != null && right != null && left.forearm && left.support
                && right.forearm && right.support;
            driveElbows = false;
        }

        private void LateUpdate() => ApplyNow();

        public void ApplyNow()
        {
            if (!configured || !gameObject.scene.IsValid()) return;
            Apply(left, leftBend);
            Apply(right, rightBend);
        }

        private void Apply(Joint joint, float bend)
        {
            if (joint == null || !joint.forearm || !joint.support) return;
            if (joint.forearm.parent != joint.support.parent) return;
            if (driveElbows)
            {
                joint.forearm.localRotation = joint.bindForearm *
                    Quaternion.AngleAxis(Mathf.Clamp(bend, -10f, 90f), joint.hingeAxis.normalized);
            }
            Quaternion delta = Quaternion.Inverse(joint.bindForearm) * joint.forearm.localRotation;
            joint.support.localRotation = joint.bindSupport *
                Quaternion.Slerp(Quaternion.identity, delta, 0.5f);
            joint.support.localPosition = joint.forearm.localPosition;
            joint.support.localScale = joint.forearm.localScale;
        }
    }
}
