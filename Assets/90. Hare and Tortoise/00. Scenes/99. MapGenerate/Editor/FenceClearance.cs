using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Stage1Generated;
using Unity.Collections;

namespace HareAndTortoise.MapGenerate
{
    // Editor-only relocation; source assets and gameplay objects remain untouched.
    internal static class FenceClearance
    {
        const string Root=TutorialWoodland.Root;
        static readonly string[] Names={"00. Tutorial A","01. Tutorial B","02. Tutorial C","03. Stage 1"};
        static readonly Dictionary<Mesh,Vector3[]> Vertices=new Dictionary<Mesh,Vector3[]>();
        static readonly Dictionary<Mesh,int[]> Triangles=new Dictionary<Mesh,int[]>();
        static Scene scene;
        static Terrain terrain;
        static BoxCollider[] fences;
        static List<Transform> candidates;
        static Stage1CourseGizmo inner,outer;
        static bool stage;

        [InitializeOnLoadMethod]static void Schedule(){EditorApplication.delayCall+=Run;}
        static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Root+"/CLEAR_FENCE.request")||File.Exists(Root+"/Reports/FenceClearanceComplete.txt"))return;
            try{Build();}catch(Exception e){File.WriteAllText(Root+"/Reports/FenceClearanceError.txt",e.ToString());Debug.LogException(e);}
        }
        static Transform Find(string name)
        {
            GameObject[] roots=scene.GetRootGameObjects();
            for(int i=0;i<roots.Length;i++){Transform[] ts=roots[i].GetComponentsInChildren<Transform>(true);for(int j=0;j<ts.Length;j++)if(ts[j].name==name)return ts[j];}return null;
        }
        static void AddCandidates(Transform parent)
        {
            if(parent==null)return;
            for(int i=0;i<parent.childCount;i++)
            {
                Transform t=parent.GetChild(i);string name=t.name;
                if(name.StartsWith("SM_Rock_")||name.StartsWith("SM_Base_Tree_")||name.StartsWith("SM_Pine_")||name.StartsWith("SM_Stump_")||name.StartsWith("SM_Felled_tree_")||name.StartsWith("SM_Braches_"))candidates.Add(t);
            }
        }
        static void Build()
        {
            Directory.CreateDirectory(Root+"/Backups/Before Fence Clearance");Directory.CreateDirectory(Root+"/Meshes/Fence Clearance");AssetDatabase.Refresh();
            var report=new StringBuilder("Fence / nature clearance: triangle-vs-oriented-box checks, not whole-canopy AABB-only checks.\nSleepTree and all gameplay objects excluded.\n");
            Scene original=SceneManager.GetActiveScene();
            for(int n=0;n<Names.Length;n++)
            {
                string path=TutorialWoodland.Scenes+Names[n]+".unity";
                scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    if(scene.isDirty)throw new Exception("Save scene first: "+Names[n]);
                    SceneManager.SetActiveScene(scene);stage=n==3;
                    string backup=Root+"/Backups/Before Fence Clearance/"+Names[n]+".unity.txt";if(!File.Exists(backup))File.Copy(path,backup);
                    terrain=Find(stage?"01 Flat Terrain - 150 x 130m - 8m dirt circuit":"Terrain_Make").GetComponent<Terrain>();
                    Transform fence=Find(stage?"Stage 1 Fence":"Tutorial Fence");if(fence==null)throw new Exception("Missing fence: "+Names[n]);
                    fences=fence.GetComponentsInChildren<BoxCollider>(true);
                    if(stage){inner=Find("Inline - Cyan").GetComponent<Stage1CourseGizmo>();outer=Find("Outline - Magenta").GetComponent<Stage1CourseGizmo>();}
                    candidates=new List<Transform>();
                    Transform source=Find(stage?"03 Woodland floor - prefab instances":"02 Groundcover source prefabs - disabled after combining");
                    AddCandidates(Find(stage?"02 Forest - prefab instances":"01 Canopy - low poly prefab instances"));AddCandidates(source);
                    var originalCells=new Dictionary<MeshRenderer,Vector2Int>();
                    if(!stage)
                    {
                        MeshRenderer[] rs=source.GetComponentsInChildren<MeshRenderer>(true);
                        for(int i=0;i<rs.Length;i++){Vector3 p=rs[i].bounds.center-source.parent.position;originalCells.Add(rs[i],new Vector2Int(Mathf.FloorToInt(p.x/12),Mathf.FloorToInt(p.z/12)));}
                    }
                    var before=new Dictionary<Component,string>();
                    GameObject[] roots=scene.GetRootGameObjects();
                    for(int i=0;i<roots.Length;i++){Component[] cs=roots[i].GetComponentsInChildren<Component>(true);for(int j=0;j<cs.Length;j++)if(cs[j]!=null)before.Add(cs[j],EditorJsonUtility.ToJson(cs[j]));}
                    var allowed=new HashSet<Component>();var affectedCells=new HashSet<Vector2Int>();int moved=0;
                    Physics.SyncTransforms();
                    for(int i=0;i<candidates.Count;i++)
                    {
                        Transform t=candidates[i];if(!Intersects(t))continue;
                        Vector3 old=t.position;Vector3 direction=AwayFromTrack(BoundsOf(t).center);
                        bool success=false;
                        for(float distance=.5f;distance<=15f&&!success;distance+=.5f)
                        {
                            float[] angles={0,20,-20,40,-40};
                            for(int a=0;a<angles.Length;a++)
                            {
                                Vector3 delta=Quaternion.Euler(0,angles[a],0)*direction*distance;
                                t.position=old+delta;
                                float y0=terrain.SampleHeight(old),y1=terrain.SampleHeight(t.position);
                                t.position+=Vector3.up*(y1-y0);
                                if(SafeGround(t)&&!Intersects(t)&&!NewSolidOverlap(t,old)) {success=true;break;}
                            }
                        }
                        if(!success){t.position=old;throw new Exception("No safe relocation found for "+t.name);}
                        if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                        allowed.Add(t);moved++;
                        report.AppendLine(Names[n]+" MOVE "+t.name+" "+old.ToString("F3")+" -> "+t.position.ToString("F3"));
                        if(!stage&&t.IsChildOf(source))
                        {
                            MeshRenderer[] rs=t.GetComponentsInChildren<MeshRenderer>(true);
                            for(int j=0;j<rs.Length;j++)if(originalCells.ContainsKey(rs[j]))affectedCells.Add(originalCells[rs[j]]);
                        }
                    }
                    if(affectedCells.Count>0)RebuildCells(n,source,originalCells,affectedCells,allowed);
                    int remaining=0;
                    for(int i=0;i<candidates.Count;i++)if(Intersects(candidates[i]))remaining++;
                    if(remaining!=0)throw new Exception("Remaining fence intersections: "+remaining);
                    var keys=new List<Component>(before.Keys);
                    for(int i=0;i<keys.Count;i++)if(!allowed.Contains(keys[i])&&(keys[i]==null||EditorJsonUtility.ToJson(keys[i])!=before[keys[i]]))throw new Exception("Unintended component change: "+keys[i]);
                    if(moved>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                    report.AppendLine(Names[n]+": checked="+candidates.Count+", moved="+moved+", remaining="+remaining+", rebuilt cells="+affectedCells.Count+", original components preserved="+(keys.Count-allowed.Count));
                    Capture(n);
                }
                finally{if(original.isLoaded)SceneManager.SetActiveScene(original);if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            AssetDatabase.SaveAssets();File.WriteAllText(Root+"/Reports/FenceClearanceReport.txt",report.ToString());File.WriteAllText(Root+"/Reports/FenceClearanceComplete.txt",DateTime.Now.ToString("O"));AssetDatabase.Refresh();Debug.Log("FENCE_CLEARANCE_COMPLETE\n"+report);
        }
        static Bounds BoundsOf(Transform t)
        {Renderer[] rs=t.GetComponentsInChildren<Renderer>(true);Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
        static Vector3 AwayFromTrack(Vector3 point)
        {
            if(!stage)return point.x<0?Vector3.left:Vector3.right;
            float best=float.MaxValue;Vector3 selected=Vector3.zero,normal=Vector3.zero;
            for(int i=0;i<inner.points.Length;i++)
            {
                Vector3 a=inner.transform.TransformPoint(inner.points[i]),b=outer.transform.TransformPoint(outer.points[i]),c=(a+b)*.5f;
                float d=(new Vector2(point.x-c.x,point.z-c.z)).sqrMagnitude;
                if(d<best){best=d;selected=c;normal=(b-a).normalized;normal.y=0;normal.Normalize();}
            }
            return Vector3.Dot(point-selected,normal)<0?-normal:normal;
        }
        static bool SafeGround(Transform t)
        {
            Bounds b=BoundsOf(t);Vector3 p=terrain.transform.position,s=terrain.terrainData.size;
            float radius=Mathf.Max(b.extents.x,b.extents.z)+.4f;
            for(int i=0;i<9;i++)
            {
                float angle=i*Mathf.PI/4;Vector3 q=b.center+(i==8?Vector3.zero:new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius);
                float u=(q.x-p.x)/s.x,v=(q.z-p.z)/s.z;
                if(u<.01f||u>.99f||v<.01f||v>.99f||terrain.terrainData.GetSteepness(u,v)>12)return false;
                if(!stage&&terrain.terrainData.GetInterpolatedHeight(u,v)>1.1f)return false;
            }
            if(!stage)
            {
                if(b.min.x<7.4f&&b.max.x>-7.4f)return false;
                Transform sleep=Find("SleepSpot");Vector3 spot=sleep!=null?sleep.position:new Vector3(6.52f,0,59.74f);
                if(Vector2.Distance(new Vector2(b.center.x,b.center.z),new Vector2(spot.x,spot.z))<radius+5)return false;
            }
            return true;
        }
        static bool NewSolidOverlap(Transform moving,Vector3 old)
        {
            Bounds b=BoundsOf(moving);Vector2 p=new Vector2(b.center.x,b.center.z);
            float r=moving.name.StartsWith("SM_Rock_")?Mathf.Min(2.7f,Mathf.Max(b.extents.x,b.extents.z)):.45f;
            Vector3 delta=moving.position-old;
            for(int i=0;i<candidates.Count;i++)
            {
                Transform other=candidates[i];if(other==moving)continue;Bounds ob=BoundsOf(other);
                float otherR=other.name.StartsWith("SM_Rock_")?Mathf.Min(2.7f,Mathf.Max(ob.extents.x,ob.extents.z)):.45f;
                Vector2 q=new Vector2(ob.center.x,ob.center.z);
                if(Vector2.Distance(p,q)<r+otherR+.2f && Vector2.Distance(p,q)<Vector2.Distance(p-new Vector2(delta.x,delta.z),q)-.05f)return true;
            }
            return false;
        }
        static bool Intersects(Transform t)
        {
            MeshRenderer[] rs=t.GetComponentsInChildren<MeshRenderer>(true);
            for(int r=0;r<rs.Length;r++)
            {
                MeshFilter mf=rs[r].GetComponent<MeshFilter>();if(mf==null||mf.sharedMesh==null)continue;
                Mesh mesh=mf.sharedMesh;
                for(int f=0;f<fences.Length;f++)
                {
                    Bounds broad=fences[f].bounds;broad.Expand(.12f);if(!broad.Intersects(rs[r].bounds))continue;
                    if(!Vertices.ContainsKey(mesh))ReadMesh(mesh);
                    Vector3[] vv=Vertices[mesh];int[] tt=Triangles[mesh];
                    Matrix4x4 matrix=fences[f].transform.worldToLocalMatrix*rs[r].transform.localToWorldMatrix;
                    Vector3 center=fences[f].center,half=fences[f].size*.5f+Vector3.one*.04f;
                    for(int i=0;i<tt.Length;i+=3)
                        if(TriangleBox(matrix.MultiplyPoint3x4(vv[tt[i]])-center,matrix.MultiplyPoint3x4(vv[tt[i+1]])-center,matrix.MultiplyPoint3x4(vv[tt[i+2]])-center,half))return true;
                }
            }
            return false;
        }
        static void ReadMesh(Mesh mesh)
        {
            if(mesh.isReadable){Vertices.Add(mesh,mesh.vertices);Triangles.Add(mesh,mesh.triangles);return;}
            // Editor-only read access preserves the source model's Read/Write optimization.
            using(var array=MeshUtility.AcquireReadOnlyMeshData(mesh))
            {
                Mesh.MeshData data=array[0];
                using(var vertices=new NativeArray<Vector3>(data.vertexCount,Allocator.Temp))
                {
                    data.GetVertices(vertices);Vertices.Add(mesh,vertices.ToArray());
                }
                var triangles=new List<int>();
                for(int s=0;s<data.subMeshCount;s++)
                {
                    using(var indices=new NativeArray<int>(data.GetSubMesh(s).indexCount,Allocator.Temp))
                    {
                        data.GetIndices(indices,s,true);triangles.AddRange(indices.ToArray());
                    }
                }
                Triangles.Add(mesh,triangles.ToArray());
            }
        }
        // Separating-axis triangle/AABB intersection in each fence box's local coordinates.
        static bool TriangleBox(Vector3 a,Vector3 b,Vector3 c,Vector3 half)
        {
            if(Mathf.Min(a.x,Mathf.Min(b.x,c.x))>half.x||Mathf.Max(a.x,Mathf.Max(b.x,c.x))<-half.x)return false;
            if(Mathf.Min(a.y,Mathf.Min(b.y,c.y))>half.y||Mathf.Max(a.y,Mathf.Max(b.y,c.y))<-half.y)return false;
            if(Mathf.Min(a.z,Mathf.Min(b.z,c.z))>half.z||Mathf.Max(a.z,Mathf.Max(b.z,c.z))<-half.z)return false;
            Vector3 e0=b-a,e1=c-b,e2=a-c;
            if(Separates(Vector3.Cross(e0,e1),a,b,c,half))return false;
            Vector3[] edges={e0,e1,e2};
            for(int i=0;i<3;i++)if(Separates(Vector3.Cross(edges[i],Vector3.right),a,b,c,half)||Separates(Vector3.Cross(edges[i],Vector3.up),a,b,c,half)||Separates(Vector3.Cross(edges[i],Vector3.forward),a,b,c,half))return false;
            return true;
        }
        static bool Separates(Vector3 axis,Vector3 a,Vector3 b,Vector3 c,Vector3 half)
        {
            if(axis.sqrMagnitude<1e-12f)return false;
            float x=Vector3.Dot(a,axis),y=Vector3.Dot(b,axis),z=Vector3.Dot(c,axis),r=half.x*Mathf.Abs(axis.x)+half.y*Mathf.Abs(axis.y)+half.z*Mathf.Abs(axis.z);
            return Mathf.Min(x,Mathf.Min(y,z))>r||Mathf.Max(x,Mathf.Max(y,z))<-r;
        }
        static void RebuildCells(int n,Transform sources,Dictionary<MeshRenderer,Vector2Int> originalCells,HashSet<Vector2Int> affected,HashSet<Component> allowed)
        {
            Transform holder=Find("03 Groundcover - combined 12m cells");var cellKeys=new List<Vector2Int>(affected);var sourceKeys=new List<MeshRenderer>(originalCells.Keys);
            for(int k=0;k<cellKeys.Count;k++)
            {
                Vector2Int key=cellKeys[k];Transform target=holder.Find((n==2?"C":"AB")+"_Floor_"+key.x+"_"+key.y);if(target==null)throw new Exception("Missing combined cell");
                var byMat=new Dictionary<Material,List<CombineInstance>>();
                for(int i=0;i<sourceKeys.Count;i++)
                {
                    MeshRenderer r=sourceKeys[i];if(originalCells[r]!=key)continue;Mesh mesh=r.GetComponent<MeshFilter>().sharedMesh;Material[] mats=r.sharedMaterials;
                    for(int s=0;s<mesh.subMeshCount;s++){if(!byMat.TryGetValue(mats[s],out List<CombineInstance> list)){list=new List<CombineInstance>();byMat.Add(mats[s],list);}list.Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=target.worldToLocalMatrix*r.transform.localToWorldMatrix});}
                }
                var matKeys=new List<Material>(byMat.Keys);var parts=new List<CombineInstance>();var temp=new List<Mesh>();
                for(int i=0;i<matKeys.Count;i++){var m=new Mesh{indexFormat=IndexFormat.UInt32};m.CombineMeshes(byMat[matKeys[i]].ToArray(),true,true);temp.Add(m);parts.Add(new CombineInstance{mesh=m});}
                var output=new Mesh{name=Names[n]+" relocated floor "+key,indexFormat=IndexFormat.UInt32};output.CombineMeshes(parts.ToArray(),false,false);output.RecalculateBounds();output.UploadMeshData(true);AssetDatabase.CreateAsset(output,Root+"/Meshes/Fence Clearance/"+n+"_"+key.x+"_"+key.y+".asset");
                for(int i=0;i<temp.Count;i++)UnityEngine.Object.DestroyImmediate(temp[i]);
                MeshFilter mf=target.GetComponent<MeshFilter>();MeshRenderer mr=target.GetComponent<MeshRenderer>();mf.sharedMesh=output;mr.sharedMaterials=matKeys.ToArray();allowed.Add(mf);allowed.Add(mr);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mr);
            }
        }
        static void Capture(int n)
        {
            Renderer[] rr=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);Terrain[] tt=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);Light[] ll=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var hiddenR=new List<Renderer>();var hiddenT=new List<Terrain>();var hiddenL=new List<Light>();
            for(int i=0;i<rr.Length;i++)if(rr[i].enabled&&rr[i].gameObject.scene!=scene){rr[i].enabled=false;hiddenR.Add(rr[i]);}
            for(int i=0;i<tt.Length;i++)if(tt[i].enabled&&tt[i].gameObject.scene!=scene){tt[i].enabled=false;hiddenT.Add(tt[i]);}
            for(int i=0;i<ll.Length;i++)if(ll[i].enabled&&ll[i].gameObject.scene!=scene){ll[i].enabled=false;hiddenL.Add(ll[i]);}
            var g=new GameObject("Temporary clearance review");SceneManager.MoveGameObjectToScene(g,scene);Camera c=g.AddComponent<Camera>();c.farClipPlane=400;
            try
            {
                if(stage){Transform spawn=Find("SpawnPoint_Outline");Vector3 d=spawn.forward;d.y=0;c.transform.position=spawn.position-d*5+Vector3.up*3;c.transform.LookAt(spawn.position+d*20);}
                else{float start=terrain.transform.position.z+32;c.transform.position=new Vector3(-1,3,start-3);c.transform.LookAt(new Vector3(0,0,start+30));}
                c.fieldOfView=65;Shot(c,Names[n]+"-Fence-Cleared-Trackside");
                if(stage){c.transform.position=new Vector3(0,81,-94);c.transform.LookAt(Vector3.zero);c.orthographic=true;c.orthographicSize=55;Shot(c,"Stage1-Fence-Cleared-Overview");}
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
