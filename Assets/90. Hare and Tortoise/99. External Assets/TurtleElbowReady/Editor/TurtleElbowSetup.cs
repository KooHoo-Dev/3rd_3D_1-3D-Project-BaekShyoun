using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HareAndTortoise.Rigging;

public static class TurtleElbowSetup
{
    [MenuItem("Tools/Turtle/Configure Elbow Support")]
    public static void ConfigureSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (!selected || !selected.scene.IsValid())
        {
            EditorUtility.DisplayDialog("Turtle elbow rig",
                "Select the root of the Turtle_ElbowReady model instance in the Scene hierarchy.", "OK");
            return;
        }
        Configure(selected);
    }

    public static void Configure(GameObject instance)
    {
        try
        {
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Configure a fresh FBX instance before unpacking it or saving a wrapper prefab.");
            if (importer.optimizeGameObjects)
                throw new InvalidOperationException("Turn off Optimize Game Objects in the FBX Rig settings, Apply, then try again.");
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!source) throw new InvalidOperationException("Could not load the source model.");
            Transform body = FindUnique(source.transform, "body");
            var left = MakeJoint(instance.transform, source.transform, body, "L");
            var right = MakeJoint(instance.transform, source.transform, body, "R");
            var component = instance.GetComponent<TurtleElbowSupport>();
            if (!component) component = Undo.AddComponent<TurtleElbowSupport>(instance);
            Undo.RecordObject(component, "Configure turtle elbow support");
            component.Configure(left, right);
            Undo.RecordObjects(new UnityEngine.Object[] { left.support, right.support }, "Pose elbow support bones");
            component.ApplyNow();
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(left.support);
            PrefabUtility.RecordPrefabInstancePropertyModifications(right.support);
            Debug.Log("Turtle elbows configured. Enable Drive Elbows to test the bend sliders; disable it for Animator control.", instance);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Turtle elbow setup", ex.Message, "OK");
        }
    }

    private static TurtleElbowSupport.Joint MakeJoint(Transform instance, Transform source, Transform body, string side)
    {
        Transform bindFore = FindUnique(source, "leg.F.01." + side);
        Transform bindSupport = FindUnique(source, "elbow.Support." + side);
        Vector3 axisWorld = Vector3.Cross(bindFore.up, body.up);
        if (axisWorld.sqrMagnitude < 0.000001f)
            throw new InvalidOperationException("Could not determine the elbow hinge from the model bind pose.");
        Transform fore = FindUnique(instance, bindFore.name);
        Transform support = FindUnique(instance, bindSupport.name);
        if (fore.parent != support.parent)
            throw new InvalidOperationException("Forearm and support must be siblings. Select the complete model instance.");
        return new TurtleElbowSupport.Joint {
            forearm = fore, support = support,
            bindForearm = bindFore.localRotation,
            bindSupport = bindSupport.localRotation,
            hingeAxis = bindFore.InverseTransformDirection(axisWorld.normalized).normalized
        };
    }

    private static Transform FindUnique(Transform root, string name)
    {
        var matches = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected exactly one bone named " + name + ". Use Turtle_ElbowReady.fbx and select its root.");
        return matches[0];
    }
}

[CustomEditor(typeof(TurtleElbowSupport))]
public sealed class TurtleElbowSupportInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var rig = (TurtleElbowSupport)target;
        if (!rig.IsConfigured)
            EditorGUILayout.HelpBox("Configure from a fresh model instance to record its original bind rotations.", MessageType.Info);
        if (GUILayout.Button("Configure From Model Asset")) TurtleElbowSetup.Configure(rig.gameObject);
        if (GUI.changed) rig.ApplyNow();
    }
}
