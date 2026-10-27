// Unity 6 / URP. Run explicitly from Tools > Bunny > Set up Humanoid and URP.
// This utility has not been executed in Unity in the authoring environment.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BunnyUnitySetup
{
    const string Folder = "Assets/BunnyHumanoid";
    const string Model = Folder + "/Bunny_Humanoid.fbx";

    [MenuItem("Tools/Bunny/Set up Humanoid and URP")]
    public static void Setup()
    {
        var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Bunny", "Copy the supplied BunnyHumanoid folder into Assets first.", "OK");
            return;
        }
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Bunny", "URP/Lit was not found. Open this package in your URP project.", "OK");
            return;
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        if (!model) throw new InvalidOperationException("The FBX has not imported yet.");
        var transforms = model.GetComponentsInChildren<Transform>(true);
        var human = new List<HumanBone>();
        Action<HumanBodyBones, string> map = (humanBone, suffix) =>
        {
            var t = transforms.FirstOrDefault(x => x.name == "mixamorig:" + suffix || x.name == suffix || x.name == "mixamorig_" + suffix);
            if (t == null) throw new InvalidOperationException("Missing bone: " + suffix);
            human.Add(new HumanBone { boneName = t.name, humanName = HumanTrait.BoneName[(int)humanBone], limit = new HumanLimit { useDefaultValues = true } });
        };
        map(HumanBodyBones.Hips,"Hips"); map(HumanBodyBones.Spine,"Spine");
        map(HumanBodyBones.Chest,"Spine1"); map(HumanBodyBones.UpperChest,"Spine2");
        map(HumanBodyBones.Neck,"Neck"); map(HumanBodyBones.Head,"Head");
        foreach (var side in new [] { "Left", "Right" })
        {
            foreach (var pair in new [] {
                new [] {"Shoulder","Shoulder"}, new [] {"UpperArm","Arm"},
                new [] {"LowerArm","ForeArm"}, new [] {"Hand","Hand"},
                new [] {"UpperLeg","UpLeg"}, new [] {"LowerLeg","Leg"},
                new [] {"Foot","Foot"}, new [] {"Toes","ToeBase"} })
                map((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),side+pair[0]), side+pair[1]);
            foreach (var finger in new [] { "Thumb", "Index", "Middle", "Ring" })
            {
                string[] joints = { "Proximal", "Intermediate", "Distal" };
                for (int i=0; i<3; i++)
                    map((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),side+finger+joints[i]),side+"Hand"+finger+(i+1));
            }
        }
        HumanDescription hd = importer.humanDescription;
        hd.human = human.ToArray();
        hd.skeleton = transforms.Select(t => new SkeletonBone { name=t.name, position=t.localPosition, rotation=t.localRotation, scale=t.localScale }).ToArray();
        hd.upperArmTwist=.5f; hd.lowerArmTwist=.5f; hd.upperLegTwist=.5f; hd.lowerLegTwist=.5f;
        hd.armStretch=.05f; hd.legStretch=.05f; hd.feetSpacing=0; hd.hasTranslationDoF=false;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.humanDescription = hd;
        importer.importAnimation = false;
        importer.optimizeGameObjects = false; // Keep ear/tail transforms available.
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();

        SetTexture("Bunny_BaseColor.png", false);
        SetTexture("Bunny_Normal.png", true);
        string materials = Folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(materials)) AssetDatabase.CreateFolder(Folder,"Materials");
        Material body = GetMaterial(materials+"/Bunny_Body_URP.mat", shader);
        body.SetColor("_BaseColor",Color.white);
        body.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/Bunny_BaseColor.png"));
        body.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/Bunny_Normal.png"));
        body.SetFloat("_BumpScale",1f); body.EnableKeyword("_NORMALMAP");
        body.SetFloat("_Metallic",0); body.SetFloat("_Smoothness",.2f);
        Material thumb = GetMaterial(materials+"/Bunny_Thumb_URP.mat", shader);
        thumb.SetColor("_BaseColor",new Color(.65f,.48f,.37f,1));
        thumb.SetFloat("_Metallic",0); thumb.SetFloat("_Smoothness",.2f);
        EditorUtility.SetDirty(body); EditorUtility.SetDirty(thumb);
        importer = (ModelImporter)AssetImporter.GetAtPath(Model);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Bunny_Symmetric_Dark_Lashes.002"),body);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Bunny_Thumb_Skin"),thumb);
        AssetDatabase.SaveAssets(); importer.SaveAndReimport();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().FirstOrDefault();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            Debug.LogError("Bunny: Unity did not validate this Humanoid Avatar. Open FBX > Rig > Configure, check mappings, and use Pose > Enforce T-Pose. Do not use an invalid Avatar.");
            EditorUtility.DisplayDialog("Bunny: Avatar needs review", "Open the selected FBX > Rig > Configure. Check required bones and Pose > Enforce T-Pose, then Apply/Done.", "OK");
            return;
        }
        Debug.Log("Bunny: Unity reports a valid Humanoid Avatar. URP materials assigned. Add this model to the scene and assign your Mixamo Animator Controller.");
        EditorUtility.DisplayDialog("Bunny", "Unity validated the Humanoid Avatar. URP materials assigned. Now assign your Animator Controller with Humanoid Mixamo clips.", "OK");
    }

    static Material GetMaterial(string path, Shader shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m,path); }
        else m.shader=shader;
        return m;
    }
    static void SetTexture(string name, bool normal)
    {
        string path = Folder+"/Textures/"+name;
        var t = AssetImporter.GetAtPath(path) as TextureImporter;
        if (t == null) throw new InvalidOperationException("Missing texture: "+path);
        t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        t.sRGBTexture = !normal; t.maxTextureSize=2048; t.mipmapEnabled=true;
        t.SaveAndReimport();
    }
}
#endif
