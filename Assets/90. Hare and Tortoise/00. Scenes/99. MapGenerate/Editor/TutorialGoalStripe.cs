using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Unity.AI.Navigation;

namespace HareAndTortoise.MapGenerate
{
    internal static class TutorialGoalStripe
    {
        const string Root=TutorialWoodland.Root;
        static readonly string[] Names={"00. Tutorial A","01. Tutorial B","02. Tutorial C"};
        [InitializeOnLoadMethod]static void Schedule(){EditorApplication.delayCall+=Run;}
        static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Root+"/GOAL_STRIPE.request")||File.Exists(Root+"/Reports/GoalStripeComplete.txt"))return;
            try{Build();}catch(Exception e){File.WriteAllText(Root+"/Reports/GoalStripeError.txt",e.ToString());Debug.LogException(e);}
        }
        static Transform Find(Scene scene,string name)
        {
            GameObject[] roots=scene.GetRootGameObjects();
            for(int i=0;i<roots.Length;i++){Transform[] ts=roots[i].GetComponentsInChildren<Transform>(true);for(int j=0;j<ts.Length;j++)if(ts[j].name==name)return ts[j];}return null;
        }
        static void Build()
        {
            Directory.CreateDirectory(Root+"/Backups/Before Goal Stripes");
            string matPath=Root+"/Materials/Tutorial Goal Chalk.mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(TutorialWoodland.Scenes+"03. Stage 1 Generated/Materials/Chalk White.mat")){name="Tutorial Goal Chalk",enableInstancing=true};AssetDatabase.CreateAsset(material,matPath);}
            Scene original=SceneManager.GetActiveScene();var report=new StringBuilder("Tutorial goal stripes: Stage 1 chalk material style, actual trigger-aligned position.\n");
            for(int n=0;n<Names.Length;n++)
            {
                string path=TutorialWoodland.Scenes+Names[n]+".unity";
                Scene scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    if(scene.isDirty)throw new Exception("Save tutorial edits first: "+Names[n]);
                    if(Find(scene,"Tutorial Goal Stripe")!=null)throw new Exception("Goal stripe already exists.");
                    SceneManager.SetActiveScene(scene);
                    string backup=Root+"/Backups/Before Goal Stripes/"+Names[n]+".unity.txt";if(!File.Exists(backup))File.Copy(path,backup);
                    Transform stageRoot=Find(scene,"Stage");
                    var before=new Dictionary<Component,string>();GameObject[] roots=scene.GetRootGameObjects();
                    for(int i=0;i<roots.Length;i++){Component[] cs=roots[i].GetComponentsInChildren<Component>(true);for(int j=0;j<cs.Length;j++)if(cs[j]!=null&&cs[j]!=stageRoot)before.Add(cs[j],EditorJsonUtility.ToJson(cs[j]));}
                    Terrain terrain=Find(scene,"Terrain_Make").GetComponent<Terrain>();Transform goal=Find(scene,"RaceManager(GoalLine)");
                    GameObject stripe=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(stripe,scene);stripe.name="Tutorial Goal Stripe";
                    UnityEngine.Object.DestroyImmediate(stripe.GetComponent<Collider>());
                    stripe.transform.SetParent(stageRoot,true);
                    Vector3 p=goal.position;p.y=terrain.SampleHeight(p)+terrain.transform.position.y+.025f;
                    stripe.transform.SetPositionAndRotation(p,Quaternion.identity);stripe.transform.localScale=new Vector3(9.8f,.045f,.45f);
                    MeshRenderer mr=stripe.GetComponent<MeshRenderer>();mr.sharedMaterial=material;mr.shadowCastingMode=ShadowCastingMode.Off;mr.lightProbeUsage=LightProbeUsage.Off;mr.reflectionProbeUsage=ReflectionProbeUsage.Off;
                    stripe.AddComponent<NavMeshModifier>().ignoreFromBuild=true;GameObjectUtility.SetStaticEditorFlags(stripe,StaticEditorFlags.BatchingStatic);
                    var keys=new List<Component>(before.Keys);for(int i=0;i<keys.Count;i++)if(EditorJsonUtility.ToJson(keys[i])!=before[keys[i]])throw new Exception("Original component changed: "+keys[i]);
                    PrefabUtility.SaveAsPrefabAssetAndConnect(stripe,Root+"/Prefabs/"+Names[n]+" Goal Stripe.prefab",InteractionMode.AutomatedAction);
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    report.AppendLine(Names[n]+": position="+p.ToString("F3")+"; width=9.8m; depth=0.45m; new colliders=0; original component snapshots matched (excluding Stage child list)="+keys.Count);
                    Capture(scene,p,Names[n]);
                }
                finally{if(original.isLoaded)SceneManager.SetActiveScene(original);if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            AssetDatabase.SaveAssets();File.WriteAllText(Root+"/Reports/GoalStripeReport.txt",report.ToString());File.WriteAllText(Root+"/Reports/GoalStripeComplete.txt",DateTime.Now.ToString("O"));AssetDatabase.Refresh();Debug.Log("TUTORIAL_GOAL_STRIPES_COMPLETE\n"+report);
        }
        static void Capture(Scene scene,Vector3 target,string name)
        {
            var disabled=new List<Behaviour>();var hidden=new List<Renderer>();
            Light[] lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);Terrain[] terrains=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);Renderer[] renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for(int i=0;i<lights.Length;i++)if(lights[i].enabled&&lights[i].gameObject.scene!=scene){lights[i].enabled=false;disabled.Add(lights[i]);}
            for(int i=0;i<terrains.Length;i++)if(terrains[i].enabled&&terrains[i].gameObject.scene!=scene){terrains[i].enabled=false;disabled.Add(terrains[i]);}
            for(int i=0;i<renderers.Length;i++)if(renderers[i].enabled&&renderers[i].gameObject.scene!=scene){renderers[i].enabled=false;hidden.Add(renderers[i]);}
            var go=new GameObject("Temporary goal review");SceneManager.MoveGameObjectToScene(go,scene);Camera c=go.AddComponent<Camera>();c.transform.position=target+new Vector3(-1,4,-10);c.transform.LookAt(target+Vector3.forward*2);c.fieldOfView=65;c.farClipPlane=350;
            var rt=new RenderTexture(1440,900,24);RenderTexture previous=RenderTexture.active;Texture2D t=null;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;t=new Texture2D(1440,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1440,900),0,0);t.Apply();File.WriteAllBytes(Root+"/Previews/"+name+"-Goal-Stripe.png",t.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=previous;if(t!=null)UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);for(int i=0;i<disabled.Count;i++)if(disabled[i]!=null)disabled[i].enabled=true;for(int i=0;i<hidden.Count;i++)if(hidden[i]!=null)hidden[i].enabled=true;}
        }
    }
}
