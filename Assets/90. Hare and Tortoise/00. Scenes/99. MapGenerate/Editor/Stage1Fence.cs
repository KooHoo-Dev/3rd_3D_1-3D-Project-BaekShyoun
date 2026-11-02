using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Stage1Generated;

namespace HareAndTortoise.MapGenerate
{
    internal static class Stage1Fence
    {
        const string Root=TutorialWoodland.Root;
        const string Path=TutorialWoodland.Scenes+"03. Stage 1.unity";
        static Mesh cube;
        static Scene scene;
        static Terrain terrain;
        static Material posts,rails;
        static int pieces,renderers,colliders;
        static readonly List<Vector3> edgeA=new List<Vector3>(),edgeB=new List<Vector3>();

        [InitializeOnLoadMethod]
        static void Schedule(){EditorApplication.delayCall+=Run;}
        static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Root+"/STAGE1_FENCE.request")||File.Exists(Root+"/Reports/Stage1FenceComplete.txt"))return;
            try{Build();}catch(Exception e){File.WriteAllText(Root+"/Reports/Stage1FenceError.txt",e.ToString());Debug.LogException(e);}
        }
        static Transform Find(string name)
        {
            GameObject[] roots=scene.GetRootGameObjects();
            for(int i=0;i<roots.Length;i++){Transform[] ts=roots[i].GetComponentsInChildren<Transform>(true);for(int j=0;j<ts.Length;j++)if(ts[j].name==name)return ts[j];}return null;
        }
        static void Build()
        {
            Scene original=SceneManager.GetActiveScene();scene=SceneManager.GetSceneByPath(Path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
            try
            {
                if(scene.isDirty)throw new Exception("Save Stage 1 edits before adding fences.");
                if(Find("Stage 1 Fence")!=null)throw new Exception("Stage 1 Fence already exists; refusing duplicates.");
                SceneManager.SetActiveScene(scene);
                Directory.CreateDirectory(Root+"/Meshes/Stage1 Fences");Directory.CreateDirectory(Root+"/Backups/Before Fences");AssetDatabase.Refresh();
                string backup=Root+"/Backups/Before Fences/03. Stage 1.unity.txt";if(!File.Exists(backup))File.Copy(Path,backup);
                var snapshot=new Dictionary<Component,string>();
                GameObject[] roots=scene.GetRootGameObjects();
                for(int i=0;i<roots.Length;i++){Component[] cs=roots[i].GetComponentsInChildren<Component>(true);for(int j=0;j<cs.Length;j++)if(cs[j]!=null)snapshot.Add(cs[j],EditorJsonUtility.ToJson(cs[j]));}
                var inner=Find("Inline - Cyan").GetComponent<Stage1CourseGizmo>();
                var outer=Find("Outline - Magenta").GetComponent<Stage1CourseGizmo>();
                if(inner.points.Length!=outer.points.Length||inner.points.Length<32)throw new Exception("Invalid course guide data.");
                terrain=Find("01 Flat Terrain - 150 x 130m - 8m dirt circuit").GetComponent<Terrain>();
                posts=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Fence Posts.mat");rails=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Fence Rails.mat");
                if(posts==null||rails==null)throw new Exception("Tutorial wood materials missing.");
                var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(primitive);
                var root=new GameObject("Stage 1 Fence");SceneManager.MoveGameObjectToScene(root,scene);root.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
                Transform physics=Child(root.transform,"03 Static collision - curve segments");
                const int count=64;
                for(int side=0;side<2;side++)
                {
                    var points=new Vector3[count];
                    for(int i=0;i<count;i++)
                    {
                        int index=i*inner.points.Length/count;
                        Vector3 a=inner.transform.TransformPoint(inner.points[index]),b=outer.transform.TransformPoint(outer.points[index]);
                        Vector3 normal=b-a;normal.y=0;normal.Normalize();
                        Vector3 center=(a+b)*.5f;
                        Vector3 point=center+normal*(side==0?-6.2f:6.2f);point.y=terrain.SampleHeight(point)+terrain.transform.position.y;
                        points[i]=point;
                    }
                    Transform visual=Child(root.transform,side==0?"01 Inner wooden fence":"02 Outer wooden fence");
                    for(int i=0;i<count;i++)
                    {
                        Vector3 a=points[i],b=points[(i+1)%count];Vector3 delta=b-a;delta.y=0;
                        edgeA.Add(a);edgeB.Add(b);
                        Transform solid=Child(physics,(side==0?"Inner ":"Outer ")+i.ToString("00"));solid.position=(a+b)*.5f;solid.rotation=Quaternion.LookRotation(delta.normalized,Vector3.up);
                        BoxCollider box=solid.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.64f,0);box.size=new Vector3(.24f,1.28f,delta.magnitude+.13f);colliders++;
                    }
                    for(int first=0;first<count;first+=8)Chunk(visual,points,first,8,side);
                }
                float clearance=CheckClearance();
                var keys=new List<Component>(snapshot.Keys);
                for(int i=0;i<keys.Count;i++)if(keys[i]==null||EditorJsonUtility.ToJson(keys[i])!=snapshot[keys[i]])throw new Exception("Original component changed: "+keys[i]);
                PrefabUtility.SaveAsPrefabAssetAndConnect(root,Root+"/Prefabs/Stage 1 Fence.prefab",InteractionMode.AutomatedAction);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Capture();
                string report="Stage 1 inner and outer wooden fences\nGuide-driven closed curves; offset +-6.2m from course center. Height 1.28m.\nVisual cubes="+pieces+"; combined renderers="+renderers+"; material slots="+(renderers*2)+"; static BoxColliders="+colliders+".\nMinimum planar clearance from lane waypoints / spawns / goals to fence center segments="+clearance+"m.\nOriginal component snapshots matched="+keys.Count+".\nNo gameplay, NavMesh, Terrain, camera or source prefab changes. No Rigidbody.\nMeshes use UInt16 indices and no retained CPU-readable copy.\n";
                File.WriteAllText(Root+"/Reports/Stage1FenceReport.txt",report);File.WriteAllText(Root+"/Reports/Stage1FenceComplete.txt",DateTime.Now.ToString("O"));AssetDatabase.Refresh();Debug.Log("STAGE1_FENCE_COMPLETE\n"+report);
            }
            finally{if(original.isLoaded)SceneManager.SetActiveScene(original);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static Transform Child(Transform parent,string name)
        {var g=new GameObject(name);SceneManager.MoveGameObjectToScene(g,scene);g.transform.SetParent(parent,false);return g.transform;}
        static void Chunk(Transform parent,Vector3[] points,int first,int count,int side)
        {
            Vector3 origin=points[first];var postsList=new List<CombineInstance>();var railsList=new List<CombineInstance>();
            for(int i=first;i<first+count;i++)
            {
                Vector3 a=points[i%points.Length],b=points[(i+1)%points.Length];Vector3 delta=b-a;delta.y=0;Quaternion rotation=Quaternion.LookRotation(delta.normalized,Vector3.up);
                postsList.Add(Part(a+Vector3.up*.64f,Quaternion.identity,new Vector3(.22f,1.28f,.22f),origin));
                railsList.Add(Part((a+b)*.5f+Vector3.up*.48f,rotation,new Vector3(.14f,.15f,delta.magnitude+.12f),origin));
                railsList.Add(Part((a+b)*.5f+Vector3.up*1.01f,rotation,new Vector3(.14f,.17f,delta.magnitude+.12f),origin));
            }
            var pm=new Mesh();pm.CombineMeshes(postsList.ToArray(),true,true);var rm=new Mesh();rm.CombineMeshes(railsList.ToArray(),true,true);
            string name="Stage1_Fence_"+side+"_"+first;
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt16};mesh.CombineMeshes(new[]{new CombineInstance{mesh=pm},new CombineInstance{mesh=rm}},false,false);mesh.RecalculateBounds();
            UnityEngine.Object.DestroyImmediate(pm);UnityEngine.Object.DestroyImmediate(rm);mesh.UploadMeshData(true);AssetDatabase.CreateAsset(mesh,Root+"/Meshes/Stage1 Fences/"+name+".asset");
            Transform t=Child(parent,name);t.position=origin;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            MeshRenderer mr=t.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterials=new[]{posts,rails};mr.lightProbeUsage=LightProbeUsage.Off;mr.reflectionProbeUsage=ReflectionProbeUsage.Off;GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);renderers++;
        }
        static CombineInstance Part(Vector3 p,Quaternion q,Vector3 size,Vector3 origin)
        {pieces++;return new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p-origin,q,size)};}
        static float CheckClearance()
        {
            float min=float.MaxValue;var markers=new List<Transform>();
            string[] parents={"InlineWaypoints","OutlineWaypoints"};
            for(int p=0;p<parents.Length;p++){Transform root=Find(parents[p]);if(root==null)throw new Exception("Missing lane route");for(int j=0;j<root.childCount;j++)markers.Add(root.GetChild(j));}
            string[] names={"SpawnPoint_Inline","SpawnPoint_Outline","GoalPoint_Inline","GoalPoint_Outline"};
            for(int i=0;i<names.Length;i++){Transform t=Find(names[i]);if(t==null)throw new Exception("Missing marker "+names[i]);markers.Add(t);}
            for(int i=0;i<markers.Count;i++)for(int j=0;j<edgeA.Count;j++)
            {
                Vector2 p=new Vector2(markers[i].position.x,markers[i].position.z),a=new Vector2(edgeA[j].x,edgeA[j].z),b=new Vector2(edgeB[j].x,edgeB[j].z);
                float distance=Vector2.Distance(p,a+(b-a)*Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude));min=Mathf.Min(min,distance);
                if(distance<1.1f)throw new Exception("Fence too close to "+markers[i].name+": "+distance);
            }
            return min;
        }
        static void Capture()
        {
            Renderer[] rr=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);var hiddenR=new List<Renderer>();
            Terrain[] tt=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);var hiddenT=new List<Terrain>();
            Light[] ll=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var hiddenL=new List<Light>();
            for(int i=0;i<rr.Length;i++)if(rr[i].enabled&&rr[i].gameObject.scene!=scene){rr[i].enabled=false;hiddenR.Add(rr[i]);}
            for(int i=0;i<tt.Length;i++)if(tt[i].enabled&&tt[i].gameObject.scene!=scene){tt[i].enabled=false;hiddenT.Add(tt[i]);}
            for(int i=0;i<ll.Length;i++)if(ll[i].enabled&&ll[i].gameObject.scene!=scene){ll[i].enabled=false;hiddenL.Add(ll[i]);}
            var g=new GameObject("Temporary stage fence review");SceneManager.MoveGameObjectToScene(g,scene);var c=g.AddComponent<Camera>();c.farClipPlane=400;
            try
            {
                c.transform.position=new Vector3(0,81,-94);c.transform.LookAt(Vector3.zero);c.orthographic=true;c.orthographicSize=55;Shot(c,"Stage1-Fence-Overview");
                Transform spawn=Find("SpawnPoint_Outline");Vector3 forward=spawn.forward;forward.y=0;c.transform.position=spawn.position-forward*5+Vector3.up*3;c.transform.LookAt(spawn.position+forward*20);c.orthographic=false;c.fieldOfView=65;Shot(c,"Stage1-Fence-Trackside");
            }
            finally{for(int i=0;i<hiddenR.Count;i++)if(hiddenR[i]!=null)hiddenR[i].enabled=true;for(int i=0;i<hiddenT.Count;i++)if(hiddenT[i]!=null)hiddenT[i].enabled=true;for(int i=0;i<hiddenL.Count;i++)if(hiddenL[i]!=null)hiddenL[i].enabled=true;UnityEngine.Object.DestroyImmediate(g);}
        }
        static void Shot(Camera c,string name)
        {
            var rt=new RenderTexture(1440,900,24);RenderTexture previous=RenderTexture.active;Texture2D t=null;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;t=new Texture2D(1440,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1440,900),0,0);t.Apply();File.WriteAllBytes(Root+"/Previews/"+name+".png",t.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=previous;if(t!=null)UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}
