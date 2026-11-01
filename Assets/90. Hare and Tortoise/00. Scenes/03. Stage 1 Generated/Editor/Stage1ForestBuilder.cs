using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace Stage1Generated {
[InitializeOnLoad] public static class ForestBuilder {
 const string Scenes="Assets/90. Hare and Tortoise/00. Scenes";
 const string Root=Scenes+"/03. Stage 1 Generated";
 const string ScenePath=Scenes+"/03. Stage 1.unity";
 const string Source="Assets/90. Hare and Tortoise/99. External Assets/Low Poly Nature/Prefab";
 static System.Random rng; static Scene scene; static Transform forest, details; static int count;
 static ForestBuilder(){EditorApplication.delayCall+=Auto;}
 static void Auto(){if(File.Exists(Root+"/BUILD.request")&&!File.Exists(ScenePath)&&!EditorApplication.isPlayingOrWillChangePlaymode) Build();}
 static float R(float a,float b){return a+(b-a)*(float)rng.NextDouble();}
 static float Smooth(float a,float b,float v){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));}
 static float Distance(float x,float z){float a=x/42f,b=z/30f; float q=Mathf.Sqrt(a*a+b*b);return (q-1)*Mathf.Sqrt(x*x+z*z)/Mathf.Max(q,.001f);}
 static GameObject GO(string name,Transform parent=null){var o=new GameObject(name);SceneManager.MoveGameObjectToScene(o,scene);if(parent)o.transform.SetParent(parent);return o;}
 static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.color=color;m.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m;}
 static TerrainLayer Layer(string name,Color c,float size){var t=new Texture2D(128,128,TextureFormat.RGB24,true);t.name=name;var px=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float n=Mathf.PerlinNoise(x*.13f,y*.13f);px[y*128+x]=c*Mathf.Lerp(.87f,1.11f,n);}t.SetPixels(px);t.Apply();AssetDatabase.CreateAsset(t,Root+"/Terrain/"+name+" Texture.asset");var l=new TerrainLayer{diffuseTexture=t,tileSize=new Vector2(size,size),smoothness=0};AssetDatabase.CreateAsset(l,Root+"/Terrain/"+name+".terrainlayer");return l;}
 static Bounds BoundsOf(GameObject o){var rr=o.GetComponentsInChildren<Renderer>();var b=rr.Length>0?rr[0].bounds:new Bounds(o.transform.position,Vector3.one);foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
 static GameObject Place(string file,Vector3 pos,float height,Transform parent,bool solid=false){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"/"+file+".prefab");if(!prefab)throw new Exception("Missing prefab "+file);var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);o.transform.SetParent(parent);o.transform.position=Vector3.zero;o.transform.rotation=Quaternion.Euler(0,R(0,360),0);var b=BoundsOf(o);o.transform.localScale*=height/Mathf.Max(b.size.y,.01f);b=BoundsOf(o);o.transform.position=pos-new Vector3(b.center.x,b.min.y,b.center.z);if(solid){var c=o.AddComponent<CapsuleCollider>();var local=o.GetComponent<MeshFilter>();if(local&&local.sharedMesh){var lb=local.sharedMesh.bounds;c.center=lb.center;c.height=lb.size.y;c.radius=Mathf.Min(lb.size.x,lb.size.z)*.14f;}else{c.height=height;c.radius=.25f;}}count++;return o;}
 static void Cube(string name,Vector3 p,Vector3 scale,Material mat,Transform parent){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(o,scene);o.name=name;o.transform.SetParent(parent);o.transform.position=p;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());}
 [MenuItem("Tools/Stage 1/Create Forest Course (New Only)")]
 public static void Build(){
 if(File.Exists(ScenePath)){Debug.LogWarning("Stage 1 exists; refusing overwrite.");return;}
 if(SessionState.GetBool("Stage1Building",false))return;SessionState.SetBool("Stage1Building",true);
 try{
 rng=new System.Random(7319);count=0;foreach(var d in new[]{"Materials","Terrain","Previews","Documentation"})Directory.CreateDirectory(Root+"/"+d);AssetDatabase.Refresh();
 scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
 var world=GO("STAGE 1 | Woodland Footrace");forest=GO("02 Forest - prefab instances",world.transform).transform;details=GO("03 Woodland floor - prefab instances",world.transform).transform;
 var td=new TerrainData{heightmapResolution=257,alphamapResolution=1024,size=new Vector3(150,8,130)};var hh=new float[257,257];td.SetHeights(0,0,hh);td.terrainLayers=new[]{Layer("Meadow Olive",new Color(.42f,.52f,.14f),7),Layer("Sunlit Moss",new Color(.55f,.59f,.22f),9),Layer("Warm Sand Course",new Color(.76f,.57f,.32f),5),Layer("Dry Verge",new Color(.59f,.53f,.25f),5)};
 var alpha=new float[1024,1024,4];for(int z=0;z<1024;z++)for(int x=0;x<1024;x++){float wx=x/1023f*150-75,wz=z/1023f*130-65;float d=Mathf.Abs(Distance(wx,wz));float noise=Mathf.PerlinNoise(wx*.65f+100,wz*.65f+100);float track=1-Smooth(3.65f,4.55f,d+(noise-.5f)*.65f);float verge=(1-Smooth(4.7f,6.2f,d))*(1-track);float meadow=1-track-verge;float n=Mathf.PerlinNoise(wx*.065f+90,wz*.065f+90);alpha[z,x,0]=meadow*(1-n);alpha[z,x,1]=meadow*n;alpha[z,x,2]=track;alpha[z,x,3]=verge;}td.SetAlphamaps(0,0,alpha);AssetDatabase.CreateAsset(td,Root+"/Terrain/Stage 1 Flat Terrain.asset");var terrain=Terrain.CreateTerrainGameObject(td);SceneManager.MoveGameObjectToScene(terrain,scene);terrain.name="01 Flat Terrain - 150 x 130m - 8m dirt circuit";terrain.transform.SetParent(world.transform);terrain.transform.position=new Vector3(-75,0,-65);var tm=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));AssetDatabase.CreateAsset(tm,Root+"/Materials/Forest Terrain.mat");terrain.GetComponent<Terrain>().materialTemplate=tm;terrain.GetComponent<Terrain>().heightmapPixelError=3;
 // Spaced canopy clusters, with a clear running corridor.
 var points=new List<Vector2>();for(int i=0;i<10000&&points.Count<360;i++){float x=R(-73,73),z=R(-63,63);if(Mathf.Abs(Distance(x,z))<8.3f)continue;var p=new Vector2(x,z);if(points.Any(v=>(v-p).sqrMagnitude<25))continue;points.Add(p);bool pine=rng.NextDouble()<.5;Place(pine?"SM_Pine_"+rng.Next(1,13):"SM_Base_Tree_"+rng.Next(1,9),new Vector3(x,0,z),R(6.7f,12.0f),forest,true);}
 for(int i=0;i<145;i++){float a=R(0,Mathf.PI*2),r=R(0,1)<.6f?R(.2f,.74f):R(1.27f,1.75f);float x=42*Mathf.Cos(a)*r,z=30*Mathf.Sin(a)*r;if(Mathf.Abs(x)>72||Mathf.Abs(z)>62||Mathf.Abs(Distance(x,z))<6.5f)continue;Place("SM_Bush_"+rng.Next(1,11),new Vector3(x,0,z),R(.7f,1.65f),details);}
 for(int i=0;i<70;i++){float a=R(0,Mathf.PI*2),side=i%2==0?-1:1;var p=new Vector3(42*Mathf.Cos(a),0,30*Mathf.Sin(a));var normal=new Vector3(Mathf.Cos(a)/42,0,Mathf.Sin(a)/30).normalized;p+=normal*side*R(6.3f,11);Place("SM_Rock_"+rng.Next(1,11),p,R(.6f,2.7f),details);}
 for(int i=0;i<36;i++){float a=R(0,Mathf.PI*2);var p=new Vector3(42*Mathf.Cos(a),0,30*Mathf.Sin(a));p+=new Vector3(Mathf.Cos(a)/42,0,Mathf.Sin(a)/30).normalized*(i%2==0?-1:1)*R(6,10);Place(i%3==0?"SM_Felled_tree_"+rng.Next(1,6):"SM_Stump_"+rng.Next(1,6),p,i%3==0?R(.5f,.85f):R(.5f,1.1f),details);}
 for(int i=0;i<2100;i++){float a=R(0,Mathf.PI*2);float side=i%2==0?-1:1;var p=new Vector3(42*Mathf.Cos(a),0,30*Mathf.Sin(a));p+=new Vector3(Mathf.Cos(a)/42,0,Mathf.Sin(a)/30).normalized*side*R(4.9f,15);int type=rng.Next(10);string name=type<6?"SM_Grass_"+rng.Next(1,11):type<9?"SM_Flower_"+rng.Next(1,16):"SM_Mushroom_"+rng.Next(1,16);Place(name,p,type<6?R(.24f,.65f):type<9?R(.22f,.52f):R(.18f,.4f),details);}
 var course=GO("04 Course markers",world.transform);var white=Mat("Chalk White",new Color(.97f,.94f,.79f));Cube("Start Finish - chalk stripe",new Vector3(0,.025f,-30),new Vector3(.32f,.04f,7.8f),white,course.transform);
 var pebble=Mat("Trail Pebbles",new Color(.55f,.42f,.25f));for(int i=0;i<360;i++){float a=R(0,Mathf.PI*2);var p=new Vector3(42*Mathf.Cos(a),.015f,30*Mathf.Sin(a));p+=new Vector3(Mathf.Cos(a)/42,0,Mathf.Sin(a)/30).normalized*R(-3.6f,3.6f);Cube("Small dirt fleck",p,new Vector3(R(.07f,.19f),.025f,R(.1f,.25f)),pebble,course.transform);}
 var way=GO("05 Route guide - 48 ordered waypoints",world.transform);for(int i=0;i<48;i++){float a=-Mathf.PI/2+i*Mathf.PI*2/48;var w=GO("Waypoint "+i.ToString("00"),way.transform);w.transform.position=new Vector3(42*Mathf.Cos(a),.1f,30*Mathf.Sin(a));w.transform.forward=new Vector3(-42*Mathf.Sin(a),0,30*Mathf.Cos(a)).normalized;}var spawn=GO("Runner Start",world.transform);spawn.transform.position=new Vector3(-2,.15f,-30);spawn.transform.forward=Vector3.right;
 var light=GO("06 Warm afternoon sun",world.transform).AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.93f,.79f);light.intensity=1.4f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(48,-35,0);RenderSettings.sun=light;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.56f,.68f,.79f);RenderSettings.ambientEquatorColor=new Color(.46f,.52f,.34f);RenderSettings.ambientGroundColor=new Color(.26f,.30f,.17f);RenderSettings.fog=false;
 var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.45f,.65f,.85f));sky.SetFloat("_AtmosphereThickness",.8f);AssetDatabase.CreateAsset(sky,Root+"/Materials/Forest Sky.mat");RenderSettings.skybox=sky;
 var cam=GO("07 Overview Camera",world.transform).AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(0,81,-94);cam.transform.LookAt(new Vector3(0,0,0));cam.orthographic=true;cam.orthographicSize=47;cam.farClipPlane=300;cam.nearClipPlane=.1f;cam.backgroundColor=new Color(.57f,.73f,.85f);cam.clearFlags=CameraClearFlags.Skybox;cam.depth=10;
 var eye=GO("08 Trackside Camera - disabled",world.transform).AddComponent<Camera>();eye.transform.position=new Vector3(0,1.75f,-30);eye.transform.LookAt(new Vector3(24,1.6f,-25));eye.fieldOfView=65;eye.farClipPlane=240;eye.enabled=false;
 // Save only the new scene and assets created by this builder. Never save all assets/scenes.
 foreach(var guid in AssetDatabase.FindAssets("",new[]{Root})){var path=AssetDatabase.GUIDToAssetPath(guid);var obj=AssetDatabase.LoadMainAssetAtPath(path);if(obj)AssetDatabase.SaveAssetIfDirty(obj);}
 if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Scene save failed");
 File.WriteAllText(Root+"/Documentation/BuildReport.txt","Stage 1 forest course\nSeed: 7319\nTerrain: 150 x 130 m, flat y=0\nCourse center radii: 42 x 30 m; width ~8 m; length ~228 m\nNature prefab instances: "+count+"\nCanopy trees: "+points.Count+"\n48 clockwise route markers + runner start\nExisting source prefabs/materials/settings were not edited.\nNo gameplay scripts or build-settings changes.\n");
 File.WriteAllText(Root+"/BUILD.success","Completed "+DateTime.Now.ToString("O"));
 // Close only clean, previously open scenes, without saving them.
 for(int i=SceneManager.sceneCount-1;i>=0;i--){var s=SceneManager.GetSceneAt(i);if(s!=scene&&!s.isDirty)EditorSceneManager.CloseScene(s,true);}
 if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,0),Quaternion.Euler(52,0,0),85,false,true);
 EditorApplication.delayCall+=()=>Capture(cam,eye);
 Debug.Log("STAGE1_BUILD_SUCCESS - "+count+" prefab instances");
 }catch(Exception e){File.WriteAllText(Root+"/BUILD.error",e.ToString());Debug.LogException(e);}finally{SessionState.SetBool("Stage1Building",false);}}
 static void Capture(Camera cam,Camera eye){try{Shot(cam,"Overview");Shot(eye,"Trackside");AssetDatabase.Refresh();}catch(Exception e){Debug.LogException(e);}}
 static void Shot(Camera c,string name){var rt=new RenderTexture(1600,1000,24);rt.Create();var previous=c.targetTexture;c.targetTexture=rt;c.Render();var active=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Root+"/Previews/"+name+".png",tex.EncodeToPNG());c.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
}}

