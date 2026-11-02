using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Unity.AI.Navigation;

namespace HareAndTortoise.MapGenerate
{
    // Editor-only fence construction. Visual cubes are merged; collision is per span.
    internal static class TutorialFence
    {
        const string Root = TutorialWoodland.Root;
        static Mesh cube;
        static Material posts, rails;
        static int cubeCount, spanCount;
        static readonly string[] Names = { "00. Tutorial A", "01. Tutorial B", "02. Tutorial C" };

        [InitializeOnLoadMethod]
        static void Schedule() { EditorApplication.delayCall += Run; }

        static void Run()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Root+"/FENCE.request") && File.Exists(Root+"/Reports/FenceComplete.txt") && !File.Exists(Root+"/Reports/FenceReviewComplete.txt"))
            { try { Review(); } catch(Exception e){Debug.LogException(e);} return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Root+"/FENCE.request") || File.Exists(Root+"/Reports/FenceComplete.txt")) return;
            try { Build(); }
            catch(Exception e) { File.WriteAllText(Root+"/Reports/FenceError.txt",e.ToString());Debug.LogException(e); }
        }

        static void Review()
        {
            Scene original=SceneManager.GetActiveScene();
            for(int n=0;n<Names.Length;n++)
            {
                Scene scene=SceneManager.GetSceneByPath(TutorialWoodland.Scenes+Names[n]+".unity");bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(TutorialWoodland.Scenes+Names[n]+".unity",OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    Terrain terrain=Find(scene,"Terrain_Make").GetComponent<Terrain>();
                    Transform spot=Find(scene,"SleepSpot");float center=spot!=null?spot.position.z:59.74f;
                    Capture(scene,terrain,n,terrain.transform.position.z+32,center-8,center+8);
                }
                finally{if(original.isLoaded)SceneManager.SetActiveScene(original);if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            File.WriteAllText(Root+"/Reports/FenceReviewComplete.txt","Each scene rendered in isolation; other loaded scenes hidden temporarily and restored.");
            AssetDatabase.Refresh();
        }

        static Transform Find(Scene scene, string name)
        {
            GameObject[] roots=scene.GetRootGameObjects();
            for(int i=0;i<roots.Length;i++)
            {
                Transform[] ts=roots[i].GetComponentsInChildren<Transform>(true);
                for(int j=0;j<ts.Length;j++)if(ts[j].name==name)return ts[j];
            }
            return null;
        }

        static void Build()
        {
            Directory.CreateDirectory(Root+"/Backups/Before Fences");
            Directory.CreateDirectory(Root+"/Meshes/Fences");
            AssetDatabase.Refresh();
            posts=Material("Fence Posts",new Color(.32f,.20f,.10f));
            rails=Material("Fence Rails",new Color(.48f,.32f,.16f));
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube=primitive.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(primitive);
            Scene original=SceneManager.GetActiveScene();
            var report=new StringBuilder("Tutorial trackside fences\nNo Rigidbody. No gameplay script, camera, Terrain or NavMesh asset edits.\n");
            for(int n=0;n<Names.Length;n++)
            {
                string path=TutorialWoodland.Scenes+Names[n]+".unity";
                Scene scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    if(scene.isDirty)throw new Exception("Save scene before fencing: "+Names[n]);
                    if(Find(scene,"Tutorial Fence")!=null)throw new Exception("Fence already exists; refusing duplicates: "+Names[n]);
                    SceneManager.SetActiveScene(scene);
                    string backup=Root+"/Backups/Before Fences/"+Names[n]+".unity.txt";
                    if(!File.Exists(backup))File.Copy(path,backup);
                    Transform terrainTransform=Find(scene,"Terrain_Make");Terrain terrain=terrainTransform.GetComponent<Terrain>();
                    Transform goal=Find(scene,"GoalPoint");
                    Transform sleep=Find(scene,"SleepSpot");
                    float center=sleep!=null?sleep.position.z:59.74f;
                    float start=terrainTransform.position.z+32f,end=goal.position.z+3f;
                    float gapStart=center-8f,gapEnd=center+8f;
                    var before=new Dictionary<Component,string>();
                    GameObject[] roots=scene.GetRootGameObjects();
                    for(int i=0;i<roots.Length;i++)
                    {
                        Component[] cs=roots[i].GetComponentsInChildren<Component>(true);
                        for(int j=0;j<cs.Length;j++)if(cs[j]!=null)before.Add(cs[j],EditorJsonUtility.ToJson(cs[j]));
                    }
                    var root=new GameObject("Tutorial Fence");SceneManager.MoveGameObjectToScene(root,scene);
                    root.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
                    cubeCount=0;spanCount=0;
                    Side(root.transform,terrain,-6.4f,start,end,n);
                    Side(root.transform,terrain,6.4f,start,Mathf.Min(end,gapStart-.15f),n);
                    Side(root.transform,terrain,6.4f,Mathf.Max(start,gapEnd+.15f),end,n);
                    // Validate the full opening, including posts and collider overlap margins.
                    Physics.SyncTransforms();
                    BoxCollider[] boxes=root.GetComponentsInChildren<BoxCollider>();
                    for(int i=0;i<boxes.Length;i++)
                        if(boxes[i].bounds.center.x>0 && boxes[i].bounds.max.z>gapStart+.05f && boxes[i].bounds.min.z<gapEnd-.05f)
                            throw new Exception("Sleep approach blocked by "+boxes[i].name);
                    var keys=new List<Component>(before.Keys);
                    for(int i=0;i<keys.Count;i++)if(keys[i]==null||EditorJsonUtility.ToJson(keys[i])!=before[keys[i]])throw new Exception("Original component changed: "+keys[i]);
                    PrefabUtility.SaveAsPrefabAssetAndConnect(root,Root+"/Prefabs/"+Names[n]+" Fence.prefab",InteractionMode.AutomatedAction);
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    report.AppendLine(Names[n]+": "+cubeCount+" visual cubes merged into "+spanCount+" renderers, "+boxes.Length+" static BoxColliders.");
                    report.AppendLine("  x=+-6.4m; z="+start+".."+end+"; right opening="+gapStart+".."+gapEnd+"; original component snapshots matched="+keys.Count);
                    Capture(scene,terrain,n,start,gapStart,gapEnd);
                }
                finally {if(original.isLoaded)SceneManager.SetActiveScene(original);if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            AssetDatabase.SaveAssets();File.WriteAllText(Root+"/Reports/FenceReport.txt",report.ToString());
            File.WriteAllText(Root+"/Reports/FenceComplete.txt",DateTime.Now.ToString("O"));
            AssetDatabase.Refresh();Debug.Log("TUTORIAL_FENCE_COMPLETE\n"+report);
        }

        static Material Material(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color,enableInstancing=true};m.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(m,path);}
            return m;
        }

        static void Side(Transform parent,Terrain terrain,float x,float start,float end,int n)
        {
            if(end-start<.2f)return;
            int count=Mathf.CeilToInt((end-start)/12f);
            for(int k=0;k<count;k++)
            {
                float a=Mathf.Lerp(start,end,k/(float)count),b=Mathf.Lerp(start,end,(k+1f)/count);
                float floor=terrain.SampleHeight(new Vector3(x,0,(a+b)*.5f))+terrain.transform.position.y;
                Vector3 origin=new Vector3(x,floor,(a+b)*.5f);
                var postParts=new List<CombineInstance>();var railParts=new List<CombineInstance>();
                int sections=Mathf.CeilToInt((b-a)/3f);
                for(int j=0;j<=sections;j++)
                {
                    float z=Mathf.Lerp(a,b,j/(float)sections);
                    float y=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
                    Add(postParts,new Vector3(x,y+.64f,z),new Vector3(.22f,1.28f,.22f),origin);
                }
                Add(railParts,new Vector3(x,floor+.48f,(a+b)*.5f),new Vector3(.14f,.15f,b-a),origin);
                Add(railParts,new Vector3(x,floor+1.01f,(a+b)*.5f),new Vector3(.14f,.17f,b-a),origin);
                var pm=new Mesh();pm.CombineMeshes(postParts.ToArray(),true,true);
                var rm=new Mesh();rm.CombineMeshes(railParts.ToArray(),true,true);
                string name="Fence_"+n+"_"+(x<0?"Left":"Right")+"_"+spanCount;
                var mesh=new Mesh{name=name};mesh.CombineMeshes(new[]{new CombineInstance{mesh=pm},new CombineInstance{mesh=rm}},false,false);mesh.RecalculateBounds();
                UnityEngine.Object.DestroyImmediate(pm);UnityEngine.Object.DestroyImmediate(rm);
                AssetDatabase.CreateAsset(mesh,Root+"/Meshes/Fences/"+name+".asset");
                var obj=new GameObject(name);SceneManager.MoveGameObjectToScene(obj,parent.gameObject.scene);obj.transform.SetParent(parent,false);obj.transform.position=origin;
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                MeshRenderer mr=obj.AddComponent<MeshRenderer>();mr.sharedMaterials=new[]{posts,rails};mr.lightProbeUsage=LightProbeUsage.Off;mr.reflectionProbeUsage=ReflectionProbeUsage.Off;
                BoxCollider box=obj.AddComponent<BoxCollider>();box.center=new Vector3(0,.64f,0);box.size=new Vector3(.24f,1.28f,b-a+.22f);
                GameObjectUtility.SetStaticEditorFlags(obj,StaticEditorFlags.BatchingStatic);spanCount++;
            }
        }

        static void Add(List<CombineInstance> parts,Vector3 center,Vector3 size,Vector3 origin)
        {parts.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(center-origin,Quaternion.identity,size)});cubeCount++;}

        static void Capture(Scene scene,Terrain terrain,int n,float start,float gapStart,float gapEnd)
        {
            var go=new GameObject("Temporary fence review camera");SceneManager.MoveGameObjectToScene(go,scene);Camera camera=go.AddComponent<Camera>();
            Light[] lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var hidden=new List<Light>();
            for(int i=0;i<lights.Length;i++)if(lights[i].gameObject.scene!=scene&&lights[i].enabled){lights[i].enabled=false;hidden.Add(lights[i]);}
            Renderer[] otherRenderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);var hiddenRenderers=new List<Renderer>();
            for(int i=0;i<otherRenderers.Length;i++)if(otherRenderers[i].gameObject.scene!=scene&&otherRenderers[i].enabled){otherRenderers[i].enabled=false;hiddenRenderers.Add(otherRenderers[i]);}
            Terrain[] otherTerrains=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);var hiddenTerrains=new List<Terrain>();
            for(int i=0;i<otherTerrains.Length;i++)if(otherTerrains[i].gameObject.scene!=scene&&otherTerrains[i].enabled){otherTerrains[i].enabled=false;hiddenTerrains.Add(otherTerrains[i]);}
            try
            {
                go.transform.position=new Vector3(-1,3,start-3);go.transform.LookAt(new Vector3(0,0,start+30));camera.fieldOfView=65;camera.farClipPlane=400;
                Shot(camera,Names[n]+"-Fence-Trackside");
                float center=(gapStart+gapEnd)*.5f;
                go.transform.position=new Vector3(-5,17,center-14);go.transform.LookAt(new Vector3(5,0,center));camera.fieldOfView=65;
                Shot(camera,Names[n]+"-Fence-SleepOpening");
            }
            finally
            {
                for(int i=0;i<hidden.Count;i++)if(hidden[i]!=null)hidden[i].enabled=true;
                for(int i=0;i<hiddenRenderers.Count;i++)if(hiddenRenderers[i]!=null)hiddenRenderers[i].enabled=true;
                for(int i=0;i<hiddenTerrains.Count;i++)if(hiddenTerrains[i]!=null)hiddenTerrains[i].enabled=true;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void Shot(Camera c,string name)
        {
            var rt=new RenderTexture(1440,900,24);RenderTexture previous=RenderTexture.active;Texture2D t=null;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;t=new Texture2D(1440,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1440,900),0,0);t.Apply();File.WriteAllBytes(Root+"/Previews/"+name+".png",t.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=previous;if(t!=null)UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}
