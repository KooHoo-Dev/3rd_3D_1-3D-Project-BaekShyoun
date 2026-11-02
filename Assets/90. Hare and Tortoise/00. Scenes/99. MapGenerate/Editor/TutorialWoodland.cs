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
    // Editor only. No runtime generation and no changes to shared source assets.
    internal static class TutorialWoodland
    {
        internal const string Root = "Assets/90. Hare and Tortoise/00. Scenes/99. MapGenerate";
        internal const string Scenes = "Assets/90. Hare and Tortoise/00. Scenes/";
        internal const string Source = "Assets/90. Hare and Tortoise/99. External Assets/Low Poly Nature/Prefab/";
        static readonly string[] Names = { "00. Tutorial A", "01. Tutorial B", "02. Tutorial C", "03. Stage 1" };

        [InitializeOnLoadMethod]
        static void Schedule() { EditorApplication.delayCall += Dispatch; }

        static void Dispatch()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Root + "/BUILD.request") && !File.Exists(Root + "/Reports/Complete.txt"))
            {
                try { Build(); }
                catch (Exception e) { File.WriteAllText(Root + "/Reports/Error.txt", e.ToString()); Debug.LogException(e); }
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Root + "/INSPECT.request")) return;
            if (File.Exists(Root + "/Reports/Inspection.txt")) return;
            try { Inspect(); }
            catch (Exception e) { Directory.CreateDirectory(Root + "/Reports"); File.WriteAllText(Root + "/Reports/Error.txt", e.ToString()); Debug.LogException(e); }
        }

        static System.Random random;
        static Terrain ground;
        static Scene currentScene;
        static Transform decoration, trees, sources;
        static readonly Dictionary<Material, Material> materialCopies = new Dictionary<Material, Material>();
        static readonly List<Vector2> treePoints = new List<Vector2>();
        static readonly List<Vector3> protectedPoints = new List<Vector3>();
        static readonly List<Bounds> placedBounds = new List<Bounds>();
        static int placed, rejected;
        static float Rand(float a, float b) => a + (b-a)*(float)random.NextDouble();

        static void Build()
        {
            for(int i=0;i<3;i++)
            {
                Scene loaded=SceneManager.GetSceneByPath(Scenes+Names[i]+".unity");
                if(loaded.isLoaded && loaded.isDirty) throw new InvalidOperationException("Save tutorial scene edits first: "+Names[i]);
            }
            string[] folders={"Materials","Terrain","Meshes","Prefabs"};
            for(int i=0;i<folders.Length;i++)Directory.CreateDirectory(Root+"/"+folders[i]);
            AssetDatabase.Refresh();
            Scene original=SceneManager.GetActiveScene();
            var report=new StringBuilder("Tutorial woodland decoration\nOriginal gameplay, geometry, NavMesh, colliders and cameras preserved.\n");
            try
            {
                GameObject sharedPrefab=null;
                for(int n=0;n<3;n++)
                {
                    currentScene=SceneManager.GetSceneByPath(Scenes+Names[n]+".unity");
                    bool opened=!currentScene.isLoaded;
                    if(opened)currentScene=EditorSceneManager.OpenScene(Scenes+Names[n]+".unity",OpenSceneMode.Additive);
                    try
                    {
                        SceneManager.SetActiveScene(currentScene);
                        ground=Find<Terrain>(currentScene);
                        if(ground==null)throw new Exception("Missing terrain");
                        if(Array.Exists(currentScene.GetRootGameObjects(),g=>g.name.StartsWith("Tutorial Woodland")))throw new Exception("Decoration already exists; refusing duplicate generation");
                        var snapshots=new Dictionary<Component,string>();
                        GameObject[] roots=currentScene.GetRootGameObjects();
                        for(int i=0;i<roots.Length;i++)
                        {
                            Component[] components=roots[i].GetComponentsInChildren<Component>(true);
                            for(int j=0;j<components.Length;j++)
                                if((components[j] is Transform || components[j] is Collider || components[j] is MonoBehaviour || components[j] is Camera)
                                    && !(components[j] is TerrainCollider) && components[j].GetComponent<Light>() == null)
                                    snapshots.Add(components[j],EditorJsonUtility.ToJson(components[j]));
                        }
                        PaintTerrain(n);
                        Lighting(n);
                        if(n==1)
                        {
                            GameObject instance=(GameObject)PrefabUtility.InstantiatePrefab(sharedPrefab,currentScene);
                            instance.transform.position=ground.transform.position;
                            decoration=instance.transform;
                            report.AppendLine(Names[n]+": identical A/B decoration prefab, independent terrain copy.");
                        }
                        else
                        {
                            random=new System.Random(n==0?110273:110293);
                            treePoints.Clear();placedBounds.Clear();protectedPoints.Clear();placed=0;rejected=0;
                            Vector3 origin=ground.transform.position;
                            // A also reserves B's sleep glade, so both landscapes remain identical.
                            protectedPoints.Add(n==0?new Vector3(7.32f,0,60.54f):new Vector3(8,0,21.3f));
                            protectedPoints.Add(n==0?new Vector3(0,0,14.5f):Vector3.zero);
                            protectedPoints.Add(n==0?new Vector3(0,0,82f):new Vector3(0,0,68f));
                            decoration=Make("Tutorial Woodland - "+(n==0?"A B - Sunlit Meadow":"C - Pine Hollow"),null).transform;
                            decoration.position=origin;
                            NavMeshModifier modifier=decoration.gameObject.AddComponent<NavMeshModifier>();
                            modifier.ignoreFromBuild=true;
                            trees=Make("01 Canopy - low poly prefab instances",decoration).transform;
                            sources=Make("02 Groundcover source prefabs - disabled after combining",decoration).transform;
                            Populate(n==2);
                            int unsafeBounds=0;
                            for(int i=0;i<placedBounds.Count;i++)
                            {
                                Bounds b=placedBounds[i];
                                if(b.min.x<7.4f && b.max.x>-7.4f)unsafeBounds++;
                            }
                            if(unsafeBounds!=0)throw new Exception("Decoration entered the race corridor: "+unsafeBounds);
                            int sourceRenderers=sources.GetComponentsInChildren<MeshRenderer>(true).Length;
                            CombineGroundcover(n==0?"AB":"C");
                            string prefabPath=Root+"/Prefabs/"+(n==0?"Tutorial AB Woodland":"Tutorial C Woodland")+".prefab";
                            GameObject asset=PrefabUtility.SaveAsPrefabAssetAndConnect(decoration.gameObject,prefabPath,InteractionMode.AutomatedAction);
                            if(n==0)sharedPrefab=asset;
                            report.AppendLine(Names[n]+": placed="+placed+", trees="+treePoints.Count+", groundcover source renderers="+sourceRenderers+", rejected unsafe candidates="+rejected);
                        }
                        int changed=0;
                        var snapshotKeys=new List<Component>(snapshots.Keys);
                        for(int i=0;i<snapshotKeys.Count;i++)if(snapshotKeys[i]==null||EditorJsonUtility.ToJson(snapshotKeys[i])!=snapshots[snapshotKeys[i]]){changed++;report.AppendLine("UNEXPECTED CHANGE: "+snapshotKeys[i]);}
                        if(changed!=0)throw new Exception("Gameplay protection check failed: "+changed);
                        report.AppendLine("Original transform/collider/script/camera snapshots matched: "+snapshots.Count);
                        report.AppendLine("Decoration colliders="+decoration.GetComponentsInChildren<Collider>(true).Length+", active renderers="+decoration.GetComponentsInChildren<Renderer>().Length+", NavMesh ignore="+decoration.GetComponent<NavMeshModifier>().ignoreFromBuild);
                        int missing=0,slots=0;long triangles=0;
                        Transform[] hierarchy=decoration.GetComponentsInChildren<Transform>(true);
                        for(int i=0;i<hierarchy.Length;i++)missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(hierarchy[i].gameObject);
                        MeshRenderer[] activeMeshes=decoration.GetComponentsInChildren<MeshRenderer>();
                        for(int i=0;i<activeMeshes.Length;i++)
                        {
                            Material[] mm=activeMeshes[i].sharedMaterials;slots+=mm.Length;
                            for(int j=0;j<mm.Length;j++)if(mm[j]==null||mm[j].shader==null||!mm[j].shader.isSupported)throw new Exception("Invalid material on "+activeMeshes[i].name);
                            triangles+=activeMeshes[i].GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
                        }
                        if(missing!=0)throw new Exception("Missing scripts in decoration: "+missing);
                        report.AppendLine("Active material slots="+slots+", triangles="+triangles+", missing scripts="+missing+" (structural figures; not an FPS benchmark)");
                        EditorSceneManager.MarkSceneDirty(currentScene);
                        EditorSceneManager.SaveScene(currentScene);
                        Capture(currentScene,ground,Names[n]+"-After-Overview",true);
                        Capture(currentScene,ground,Names[n]+"-After-Trackside",false);
                    }
                    finally {if(original.isLoaded)SceneManager.SetActiveScene(original);if(opened)EditorSceneManager.CloseScene(currentScene,true);}
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText(Root+"/Reports/BuildReport.txt",report.ToString());
                File.WriteAllText(Root+"/Reports/Complete.txt",DateTime.Now.ToString("O"));
                AssetDatabase.Refresh();Debug.Log("TUTORIAL_WOODLAND_COMPLETE\n"+report);
            }
            finally { if(original.isLoaded)SceneManager.SetActiveScene(original); }
        }

        static GameObject Make(string name,Transform parent)
        {
            var g=new GameObject(name);SceneManager.MoveGameObjectToScene(g,currentScene);
            if(parent!=null)g.transform.SetParent(parent,false);
            return g;
        }

        static bool Safe(float x,float z,float radius,bool canopy)
        {
            Vector3 p=ground.transform.position,s=ground.terrainData.size;
            float margin=radius+2.5f;
            if(x<p.x+margin||x>p.x+s.x-margin||z<p.z+margin||z>p.z+s.z-margin)return false;
            // Entire race corridor stays clear, including canopies and decorative rocks.
            if(Mathf.Abs(x)<7.4f+radius)return false;
            for(int i=0;i<protectedPoints.Count;i++)
            {
                Vector3 q=protectedPoints[i];
                float safeRadius=i==0?9f:i==1?8f:10f;
                if(Vector2.Distance(new Vector2(x,z),new Vector2(q.x,q.z))<safeRadius+radius)return false;
            }
            float min=float.MaxValue,max=float.MinValue;
            for(int i=0;i<13;i++)
            {
                float angle=(i-1)*Mathf.PI/6;
                float ox=i==0?0:Mathf.Cos(angle)*margin,oz=i==0?0:Mathf.Sin(angle)*margin;
                float u=(x+ox-p.x)/s.x,v=(z+oz-p.z)/s.z;
                float h=ground.terrainData.GetInterpolatedHeight(u,v);
                // Reject slopes, cliff lips AND the flat high plateaus above the cliffs.
                if(h>1.1f||ground.terrainData.GetSteepness(u,v)>12)return false;
                min=Mathf.Min(min,h);max=Mathf.Max(max,h);
            }
            return max-min<.45f;
        }

        static Bounds BoundsOf(GameObject go)
        {
            Renderer[] rr=go.GetComponentsInChildren<Renderer>();
            Bounds b=rr[0].bounds;for(int i=1;i<rr.Length;i++)b.Encapsulate(rr[i].bounds);return b;
        }

        static GameObject Place(string name,float x,float z,float height,bool canopy)
        {
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Source+name+".prefab");
            if(prefab==null)throw new Exception("Missing nature prefab: "+name);
            GameObject obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,currentScene);
            obj.transform.SetParent(canopy?trees:sources,false);
            obj.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,Rand(0,360),0));
            Bounds b=BoundsOf(obj);obj.transform.localScale*=height/Mathf.Max(.01f,b.size.y);b=BoundsOf(obj);
            float radius=Mathf.Max(b.extents.x,b.extents.z);
            if(!Safe(x,z,radius,canopy)){UnityEngine.Object.DestroyImmediate(obj);rejected++;return null;}
            float y=ground.SampleHeight(new Vector3(x,0,z))+ground.transform.position.y;
            obj.transform.position+=new Vector3(x-b.center.x,y-b.min.y-.025f,z-b.center.z);
            Collider[] colliders=obj.GetComponentsInChildren<Collider>(true);
            for(int i=0;i<colliders.Length;i++)UnityEngine.Object.DestroyImmediate(colliders[i]);
            Renderer[] rr=obj.GetComponentsInChildren<Renderer>(true);
            for(int i=0;i<rr.Length;i++)
            {
                Material[] mats=rr[i].sharedMaterials;
                for(int j=0;j<mats.Length;j++)
                {
                    Material source=mats[j];
                    if(!materialCopies.TryGetValue(source,out Material copy))
                    {
                        string assetPath=Root+"/Materials/Woodland "+source.name+".mat";
                        copy=AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                        if(copy==null)
                        {
                            copy=new Material(source){name="Woodland "+source.name,enableInstancing=true};
                            if(copy.HasProperty("_Smoothness"))copy.SetFloat("_Smoothness",0);
                            AssetDatabase.CreateAsset(copy,assetPath);
                        }
                        materialCopies.Add(source,copy);
                    }
                    mats[j]=copy;
                }
                rr[i].sharedMaterials=mats;rr[i].shadowCastingMode=canopy?ShadowCastingMode.On:ShadowCastingMode.Off;
                rr[i].lightProbeUsage=LightProbeUsage.Off;rr[i].reflectionProbeUsage=ReflectionProbeUsage.Off;
                GameObjectUtility.SetStaticEditorFlags(rr[i].gameObject,StaticEditorFlags.BatchingStatic);
            }
            if(canopy)
            {
                var lod=obj.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,rr)});lod.RecalculateBounds();
                treePoints.Add(new Vector2(x,z));
            }
            placedBounds.Add(BoundsOf(obj));placed++;return obj;
        }

        static void Populate(bool c)
        {
            string[] broad={"SM_Base_Tree_1","SM_Base_Tree_2","SM_Base_Tree_3","SM_Base_Tree_6","SM_Base_Tree_7"};
            string[] pine={"SM_Pine_1","SM_Pine_3","SM_Pine_4","SM_Pine_7","SM_Pine_10"};
            float oz=ground.transform.position.z;
            for(int i=0;i<6500&&treePoints.Count<(c?105:100);i++)
            {
                float x=Rand(-34,34),z=oz+Rand(22,131);
                bool close=false;for(int j=0;j<treePoints.Count;j++)if(Vector2.Distance(treePoints[j],new Vector2(x,z))<5.7f){close=true;break;}
                if(close)continue;
                bool conifer=Rand(0,1)<(c?.76f:.35f);
                string[] pool=conifer?pine:broad;
                Place(pool[random.Next(pool.Length)],x,z,Rand(c?6.5f:5.8f,c?10f:9f),true);
            }
            for(int i=0;i<240;i++)
            {
                float x=Rand(8.5f,33)*(i%2==0?-1:1),z=oz+Rand(23,130);
                int type=random.Next(10);
                string name=type<5?"SM_Bush_"+random.Next(1,11):type<9?"SM_Rock_"+random.Next(1,11):c?"SM_Felled_tree_"+random.Next(1,6):"SM_Stump_"+random.Next(1,6);
                Place(name,x,z,type<5?Rand(.55f,1.2f):type<9?Rand(.6f,c?2.1f:1.65f):Rand(.4f,.7f),false);
            }
            // Patches instead of uniform noise: denser flower verges, sparse forest floor.
            for(int patch=0;patch<110;patch++)
            {
                float cx=Rand(9,28)*(patch%2==0?-1:1),cz=oz+Rand(24,128);
                int kind=random.Next(10);
                for(int j=0;j<random.Next(4,10);j++)
                {
                    float x=cx+Rand(-1.8f,1.8f),z=cz+Rand(-2.4f,2.4f);
                    string name=kind<6?"SM_Grass_"+random.Next(1,11):kind<(c?7:9)?"SM_Flower_"+random.Next(1,16):"SM_Mushroom_"+random.Next(1,16);
                    Place(name,x,z,Rand(.23f,.48f),false);
                }
            }
        }

        static void PaintTerrain(int n)
        {
            TerrainData original=ground.terrainData;
            TerrainData copy=UnityEngine.Object.Instantiate(original);copy.name=Names[n]+" Woodland Terrain";
            AssetDatabase.CreateAsset(copy,Root+"/Terrain/"+copy.name+".asset");
            var layers=new TerrainLayer[5];
            string[] names={"Meadow Olive","Sunlit Moss","Warm Sand Course","Dry Verge"};
            for(int i=0;i<4;i++)
            {
                string path=Root+"/Terrain/"+names[i]+".terrainlayer";
                layers[i]=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if(layers[i]==null)
                {
                    layers[i]=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TerrainLayer>(Scenes+"03. Stage 1 Generated/Terrain/"+names[i]+".terrainlayer"));
                    layers[i].name=names[i];AssetDatabase.CreateAsset(layers[i],path);
                }
            }
            string cliffPath=Root+"/Terrain/Mossy Escarpment.terrainlayer";
            layers[4]=AssetDatabase.LoadAssetAtPath<TerrainLayer>(cliffPath);
            if(layers[4]==null)
            {
                var texture=new Texture2D(64,64,TextureFormat.RGB24,true){name="Mossy Escarpment"};
                var px=new Color[4096];for(int z=0;z<64;z++)for(int x=0;x<64;x++)px[z*64+x]=Color.Lerp(new Color(.32f,.35f,.25f),new Color(.49f,.48f,.34f),Mathf.PerlinNoise(x*.14f,z*.14f));
                texture.SetPixels(px);texture.Apply();AssetDatabase.CreateAsset(texture,Root+"/Terrain/Mossy Escarpment Texture.asset");
                layers[4]=new TerrainLayer{diffuseTexture=texture,tileSize=new Vector2(6,6),smoothness=0};AssetDatabase.CreateAsset(layers[4],cliffPath);
            }
            copy.terrainLayers=layers;
            int res=copy.alphamapResolution;
            float[,,] alpha=new float[res,res,5];
            Vector3 p=ground.transform.position,size=copy.size;
            for(int z=0;z<res;z++)for(int x=0;x<res;x++)
            {
                float u=x/(res-1f),v=z/(res-1f),wx=p.x+u*size.x,wz=p.z+v*size.z;
                float slope=copy.GetSteepness(u,v),h=copy.GetInterpolatedHeight(u,v);
                float cliff=Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,48,slope));
                float edge=Mathf.PerlinNoise(wx*.7f+43,wz*.7f+12)*.65f-.325f;
                float start=p.z+35f,end=p.z+107f;
                float along=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(end,end+3,wz)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(start-3,start,wz));
                float distance=Mathf.Abs(wx)+edge;
                float path=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.5f,5.3f,distance)))*along*(h<1?1:0);
                float verge=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.4f,7.6f,distance)))*along*(1-path)*(1-cliff);
                path*=1-cliff;
                float meadow=Mathf.Max(0,1-path-verge-cliff);
                float blend=Mathf.PerlinNoise(wx*.075f+80,wz*.075f+90)*.65f;
                alpha[z,x,0]=meadow*(1-blend);alpha[z,x,1]=meadow*blend;alpha[z,x,2]=path;alpha[z,x,3]=verge;alpha[z,x,4]=cliff;
            }
            copy.SetAlphamaps(0,0,alpha);
            float[,] oldHeights=original.GetHeights(0,0,original.heightmapResolution,original.heightmapResolution);
            float[,] newHeights=copy.GetHeights(0,0,copy.heightmapResolution,copy.heightmapResolution);
            for(int z=0;z<copy.heightmapResolution;z++)for(int x=0;x<copy.heightmapResolution;x++)
                if(oldHeights[z,x]!=newHeights[z,x])throw new Exception("Terrain height changed unexpectedly");
            ground.terrainData=copy;ground.GetComponent<TerrainCollider>().terrainData=copy;
            // TerrainCollider asset reference is the only intended collider change (identical heightmap).
            Material mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Woodland Terrain.mat");
            if(mat==null){mat=new Material(ground.materialTemplate){name="Woodland Terrain"};AssetDatabase.CreateAsset(mat,Root+"/Materials/Woodland Terrain.mat");}
            ground.materialTemplate=mat;
        }

        static void Lighting(int n)
        {
            Light sun=Find<Light>(currentScene);
            sun.transform.rotation=Quaternion.Euler(48,-35,0);
            sun.intensity=n==2?1.25f:1.4f;sun.color=n==2?new Color(1,.90f,.75f):new Color(1,.93f,.79f);
            RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.56f,.68f,.79f);RenderSettings.ambientEquatorColor=new Color(.46f,.52f,.34f);RenderSettings.ambientGroundColor=new Color(.26f,.30f,.17f);
            RenderSettings.fog=false;
            Material sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Woodland Sky.mat");
            if(sky==null)
            {
                sky=new Material(AssetDatabase.LoadAssetAtPath<Material>(Scenes+"03. Stage 1 Generated/Materials/Forest Sky.mat")){name="Woodland Sky"};
                AssetDatabase.CreateAsset(sky,Root+"/Materials/Woodland Sky.mat");
            }
            RenderSettings.skybox=sky;
        }

        static void CombineGroundcover(string variant)
        {
            MeshRenderer[] renderers=sources.GetComponentsInChildren<MeshRenderer>();
            var cells=new Dictionary<Vector2Int,List<MeshRenderer>>();
            for(int i=0;i<renderers.Length;i++)
            {
                Vector3 p=renderers[i].bounds.center-decoration.position;
                var key=new Vector2Int(Mathf.FloorToInt(p.x/12),Mathf.FloorToInt(p.z/12));
                if(!cells.TryGetValue(key,out List<MeshRenderer> list)){list=new List<MeshRenderer>();cells.Add(key,list);}list.Add(renderers[i]);
            }
            Transform combined=Make("03 Groundcover - combined 12m cells",decoration).transform;
            var cellKeys=new List<Vector2Int>(cells.Keys);
            for(int cellIndex=0;cellIndex<cellKeys.Count;cellIndex++)
            {
                Vector2Int cellKey=cellKeys[cellIndex];List<MeshRenderer> cellRenderers=cells[cellKey];
                var byMaterial=new Dictionary<Material,List<CombineInstance>>();
                Vector3 origin=decoration.position+new Vector3((cellKey.x+.5f)*12,0,(cellKey.y+.5f)*12);
                for(int i=0;i<cellRenderers.Count;i++)
                {
                    MeshRenderer r=cellRenderers[i];Mesh mesh=r.GetComponent<MeshFilter>().sharedMesh;Material[] mats=r.sharedMaterials;
                    for(int s=0;s<mesh.subMeshCount;s++)
                    {
                        if(!byMaterial.TryGetValue(mats[s],out List<CombineInstance> list)){list=new List<CombineInstance>();byMaterial.Add(mats[s],list);}
                        list.Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=Matrix4x4.Translate(-origin)*r.transform.localToWorldMatrix});
                    }
                }
                var parts=new List<CombineInstance>();var materials=new List<Material>();var temporary=new List<Mesh>();
                var materialKeys=new List<Material>(byMaterial.Keys);
                for(int materialIndex=0;materialIndex<materialKeys.Count;materialIndex++)
                {
                    Material materialKey=materialKeys[materialIndex];
                    var m=new Mesh{indexFormat=IndexFormat.UInt32};m.CombineMeshes(byMaterial[materialKey].ToArray(),true,true,false);
                    parts.Add(new CombineInstance{mesh=m,transform=Matrix4x4.identity});materials.Add(materialKey);temporary.Add(m);
                }
                string name=variant+"_Floor_"+cellKey.x+"_"+cellKey.y;
                var meshFinal=new Mesh{name=name,indexFormat=IndexFormat.UInt32};meshFinal.CombineMeshes(parts.ToArray(),false,false,false);meshFinal.RecalculateBounds();
                AssetDatabase.CreateAsset(meshFinal,Root+"/Meshes/"+name+".asset");
                for(int i=0;i<temporary.Count;i++)UnityEngine.Object.DestroyImmediate(temporary[i]);
                GameObject go=Make(name,combined);go.transform.position=origin;go.AddComponent<MeshFilter>().sharedMesh=meshFinal;
                MeshRenderer mr=go.AddComponent<MeshRenderer>();mr.sharedMaterials=materials.ToArray();mr.shadowCastingMode=ShadowCastingMode.Off;mr.lightProbeUsage=LightProbeUsage.Off;mr.reflectionProbeUsage=ReflectionProbeUsage.Off;
                GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
            }
            sources.gameObject.SetActive(false);
        }

        static T Find<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T value = roots[i].GetComponentInChildren<T>(true);
                if (value != null) return value;
            }
            return null;
        }

        static string PathOf(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        static void Inspect()
        {
            Directory.CreateDirectory(Root + "/Reports");
            Directory.CreateDirectory(Root + "/Previews");
            Directory.CreateDirectory(Root + "/Backups");
            var report = new StringBuilder();
            Scene original = SceneManager.GetActiveScene();
            for (int n = 0; n < Names.Length; n++)
            {
                string path = Scenes + Names[n] + ".unity";
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    report.AppendLine("\n=== " + Names[n] + " dirty=" + scene.isDirty + " ===");
                    report.AppendLine("Ambient " + RenderSettings.ambientMode + " intensity=" + RenderSettings.ambientIntensity + " fog=" + RenderSettings.fog + " sky=" + RenderSettings.skybox);
                    Terrain terrain = Find<Terrain>(scene);
                    if (terrain != null)
                    {
                        Vector3 p = terrain.transform.position, size = terrain.terrainData.size;
                        report.AppendLine("TERRAIN " + PathOf(terrain.transform) + " position=" + p + " size=" + size + " material=" + terrain.materialTemplate);
                        TerrainLayer[] layers = terrain.terrainData.terrainLayers;
                        for (int i = 0; i < layers.Length; i++) report.AppendLine("LAYER " + i + " " + AssetDatabase.GetAssetPath(layers[i]));
                        if (n < 3)
                        {
                            File.Copy(path, Root + "/Backups/" + Names[n] + ".unity.txt", false);
                            HeightMap(terrain, Names[n]);
                            Capture(scene, terrain, Names[n] + "-Before-Overview", true);
                            Capture(scene, terrain, Names[n] + "-Before-Trackside", false);
                        }
                    }
                    GameObject[] roots = scene.GetRootGameObjects();
                    for (int r = 0; r < roots.Length; r++)
                    {
                        Transform[] ts = roots[r].GetComponentsInChildren<Transform>(true);
                        for (int i = 0; i < ts.Length; i++)
                        {
                            string name = ts[i].name;
                            string full = PathOf(ts[i]);
                            bool key = name.Contains("Sleep") || name.Contains("Goal") || name == "Hare" || name == "Tortoise" || name.Contains("Spawn") || name.Contains("Waypoint");
                            if (n < 3 && (key || full.Split('/').Length <= 3))
                                report.AppendLine("OBJECT " + full + " active=" + ts[i].gameObject.activeSelf + " pos=" + ts[i].position + " scale=" + ts[i].lossyScale);
                        }
                        Collider[] cs = roots[r].GetComponentsInChildren<Collider>(true);
                        if (n < 3) for (int i = 0; i < cs.Length; i++)
                            if (!(cs[i] is TerrainCollider) && PathOf(cs[i].transform).Contains("Props"))
                                report.AppendLine("COLLIDER " + PathOf(cs[i].transform) + " trigger=" + cs[i].isTrigger + " bounds=" + cs[i].bounds);
                        Light[] lights = roots[r].GetComponentsInChildren<Light>(true);
                        for (int i = 0; i < lights.Length; i++) report.AppendLine("LIGHT " + PathOf(lights[i].transform) + " rotation=" + lights[i].transform.eulerAngles + " intensity=" + lights[i].intensity + " color=" + lights[i].color);
                    }
                }
                finally { if (original.isLoaded) SceneManager.SetActiveScene(original); if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            string[] prefabs = { "SM_Base_Tree_1", "SM_Base_Tree_3", "SM_Pine_1", "SM_Pine_3", "SM_Bush_1", "SM_Rock_1", "SM_Grass_1", "SM_Flower_1", "SM_Stump_1", "SM_Felled_tree_1" };
            for (int i = 0; i < prefabs.Length; i++)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(Source + prefabs[i] + ".prefab");
                MeshFilter[] ms = p.GetComponentsInChildren<MeshFilter>(true);
                long tris = 0;
                for (int j = 0; j < ms.Length; j++) if (ms[j].sharedMesh != null) tris += ms[j].sharedMesh.triangles.Length / 3;
                report.AppendLine("PREFAB " + prefabs[i] + " triangles=" + tris + " localMeshBounds=" + (ms.Length > 0 ? ms[0].sharedMesh.bounds.ToString() : "none"));
            }
            File.WriteAllText(Root + "/Reports/Inspection.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("TUTORIAL_WOODLAND_INSPECTED");
        }

        static void HeightMap(Terrain terrain, string name)
        {
            const int res = 512;
            var t = new Texture2D(res, res, TextureFormat.RGB24, false);
            var colors = new Color[res * res];
            Vector3 size = terrain.terrainData.size;
            var grid = new StringBuilder("x,z,height,slope\n");
            for (int z = 0; z < res; z++) for (int x = 0; x < res; x++)
            {
                float u = x / (res - 1f), v = z / (res - 1f);
                float h = terrain.terrainData.GetInterpolatedHeight(u, v);
                float slope = terrain.terrainData.GetSteepness(u, v);
                colors[z * res + x] = slope > 25 ? new Color(.6f,.15f,.1f) : Color.Lerp(new Color(.15f,.3f,.12f), new Color(.8f,.85f,.5f), h / Mathf.Max(1,size.y));
                if (x % 16 == 0 && z % 16 == 0) grid.AppendLine((terrain.transform.position.x+u*size.x)+","+(terrain.transform.position.z+v*size.z)+","+(terrain.transform.position.y+h)+","+slope);
            }
            t.SetPixels(colors); t.Apply();
            File.WriteAllBytes(Root + "/Previews/" + name + "-Slope.png", t.EncodeToPNG());
            File.WriteAllText(Root + "/Reports/" + name + "-Height.csv", grid.ToString());
            UnityEngine.Object.DestroyImmediate(t);
        }

        static void Capture(Scene scene, Terrain terrain, string file, bool overview)
        {
            var go = new GameObject("Temporary Woodland Review Camera");
            SceneManager.MoveGameObjectToScene(go, scene);
            var c = go.AddComponent<Camera>();
            Vector3 p = terrain.transform.position, size = terrain.terrainData.size;
            if (overview)
            {
                Vector3 center = p + new Vector3(size.x*.5f,0,size.z*.5f);
                go.transform.position = center + new Vector3(0,Mathf.Max(size.x,size.z)*.85f,-size.z*.45f);
                go.transform.LookAt(center);
                c.orthographic = true; c.orthographicSize = Mathf.Max(size.z*.52f,size.x*.33f);
            }
            else
            {
                Transform spawn = null, goal = null;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i=0;i<roots.Length;i++)
                {
                    Transform[] ts=roots[i].GetComponentsInChildren<Transform>(true);
                    for(int j=0;j<ts.Length;j++) { if(ts[j].name=="Tortoise") spawn=ts[j]; if(ts[j].name=="GoalPoint")goal=ts[j]; }
                }
                Vector3 a=spawn!=null?spawn.position:p+size*.5f;
                Vector3 b=goal!=null?goal.position:a+Vector3.forward*50;
                Vector3 direction = new Vector3(b.x-a.x,0,b.z-a.z).normalized;
                go.transform.position=a-direction*5+Vector3.up*4;
                go.transform.LookAt(a+direction*28+Vector3.up*1.5f);
                c.fieldOfView=65;
            }
            c.nearClipPlane=.1f;c.farClipPlane=1500;
            var rt = new RenderTexture(1440,900,24);
            var old=RenderTexture.active;
            Texture2D tex=null;
            Light[] allLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var hiddenLights=new List<Light>();
            for(int i=0;i<allLights.Length;i++)if(allLights[i].gameObject.scene!=scene&&allLights[i].enabled){allLights[i].enabled=false;hiddenLights.Add(allLights[i]);}
            try
            {
                c.targetTexture=rt;c.Render();RenderTexture.active=rt;
                tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();
                File.WriteAllBytes(Root+"/Previews/"+file+".png",tex.EncodeToPNG());
            }
            finally { for(int i=0;i<hiddenLights.Count;i++)if(hiddenLights[i]!=null)hiddenLights[i].enabled=true;RenderTexture.active=old;c.targetTexture=null;if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
