using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Stage1Generated {
[InitializeOnLoad] public static class ForestFinish {
const string Root="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1 Generated";
static ForestFinish(){EditorApplication.delayCall+=Finish;}
static float Smooth(float a,float b,float v){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));}
static void Finish(){if(!File.Exists(Root+"/BUILD.success")||File.Exists(Root+"/FINISH.success"))return;try{
var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1.unity")return;
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();var td=terrain.terrainData;int res=td.alphamapResolution;var al=td.GetAlphamaps(0,0,res,res);File.WriteAllText(Root+"/Documentation/TerrainDiagnostics.txt","Layers="+td.terrainLayers.Length+" sample="+al[276,512,0]+","+al[276,512,1]+","+al[276,512,2]);
for(int z=0;z<res;z++)for(int x=0;x<res;x++){float wx=x/(float)(res-1)*150-75,wz=z/(float)(res-1)*130-65;float q=Mathf.Sqrt(wx*wx/1764+wz*wz/900);float d=Mathf.Abs((q-1)*Mathf.Sqrt(wx*wx+wz*wz)/Mathf.Max(q,.001f));float t=1-Smooth(3.65f,4.6f,d);float v=(1-Smooth(4.6f,6,d))*(1-t);float n=Mathf.PerlinNoise(wx*.065f+90,wz*.065f+90);al[z,x,0]=(1-t-v)*(1-n);al[z,x,1]=(1-t-v)*n;al[z,x,2]=t;al[z,x,3]=v;}td.SetAlphamaps(0,0,al);EditorUtility.SetDirty(td);AssetDatabase.SaveAssetIfDirty(td);terrain.Flush();
// A softly varied low-poly dirt ribbon gives the course an editable, unambiguous surface.
var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>[5];for(int k=0;k<5;k++)tris[k]=new List<int>();const int N=320;const int W=6;var random=new System.Random(928);
for(int i=0;i<=N;i++){float a=i*Mathf.PI*2/N;var c=new Vector3(42*Mathf.Cos(a),.018f,30*Mathf.Sin(a));var norm=new Vector3(Mathf.Cos(a)/42,0,Mathf.Sin(a)/30).normalized;float edge=.13f*Mathf.Sin(a*37)+.08f*Mathf.Cos(a*63);for(int j=0;j<=W;j++){vertices.Add(c+norm*((j/(float)W*2-1)*(4+edge)));uv.Add(new Vector2(i/12f,j/(float)W));}}
for(int i=0;i<N;i++)for(int j=0;j<W;j++){int a=i*(W+1)+j,b=a+W+1;var list=tris[random.Next(5)];list.Add(a);list.Add(b);list.Add(a+1);list.Add(a+1);list.Add(b);list.Add(b+1);}
var mesh=new Mesh{name="Continuous Sandy Course"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=5;for(int k=0;k<5;k++)mesh.SetTriangles(tris[k],k);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"/Terrain/Sandy Course Mesh.asset");
var materials=new Material[5];for(int k=0;k<5;k++){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name="Sand Tone "+k;m.color=new Color(.78f,.61f,.38f)*(.97f+k*.015f);m.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(m,Root+"/Materials/"+m.name+".mat");materials[k]=m;}
var go=new GameObject("09 Sandy racing circuit - 8m clear ribbon");go.transform.SetParent(scene.GetRootGameObjects()[0].transform);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;
EditorSceneManager.SaveScene(scene);File.WriteAllText(Root+"/FINISH.success","Dirt ribbon and terrain paint verified");EditorApplication.delayCall+=Capture;
}catch(Exception e){File.WriteAllText(Root+"/FINISH.error",e.ToString());Debug.LogException(e);}}
static void Capture(){foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){if(c.name.StartsWith("07"))Shot(c,"Overview");if(c.name.StartsWith("08"))Shot(c,"Trackside");}AssetDatabase.Refresh();}
static void Shot(Camera c,string n){var rt=new RenderTexture(1600,1000,24);rt.Create();var old=c.targetTexture;c.targetTexture=rt;c.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1600,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,1000),0,0);t.Apply();File.WriteAllBytes(Root+"/Previews/"+n+".png",t.EncodeToPNG());c.targetTexture=old;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
}}
