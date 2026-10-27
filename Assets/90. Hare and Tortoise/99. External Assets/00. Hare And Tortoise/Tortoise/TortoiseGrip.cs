using UnityEngine;

// Add to the character root. Assign the two hand SkinnedMeshRenderers.
// One slider per hand; intermediate shapes are mixed automatically.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class TortoiseGrip : MonoBehaviour
{
    public SkinnedMeshRenderer leftHand;
    public SkinnedMeshRenderer rightHand;
    [Range(0f, 100f)] public float leftGrip;
    [Range(0f, 100f)] public float rightGrip;

    void LateUpdate()
    {
        Apply(leftHand, leftGrip);
        Apply(rightHand, rightGrip);
    }

    static void Apply(SkinnedMeshRenderer hand, float value)
    {
        if (hand == null || hand.sharedMesh == null) return;
        float grip = Mathf.Clamp01(value / 100f);
        Mesh mesh = hand.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string name = mesh.GetBlendShapeName(i);
            int split = name.LastIndexOf('.');
            if (split >= 0) name = name.Substring(split + 1);
            float target;
            switch (name)
            {
                case "Grip_25": target = 0.25f; break;
                case "Grip_50": target = 0.50f; break;
                case "Grip_75": target = 0.75f; break;
                case "Grip_100": target = 1f; break;
                default: continue;
            }
            float weight = Mathf.Max(0f, 1f - Mathf.Abs(grip - target) / 0.25f) * 100f;
            if (Mathf.Abs(hand.GetBlendShapeWeight(i) - weight) > 0.001f)
                hand.SetBlendShapeWeight(i, weight);
        }
    }
}
