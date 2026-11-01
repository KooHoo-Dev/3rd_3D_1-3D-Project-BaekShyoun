using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Stage1Generated {
[InitializeOnLoad] public static class Stage1OptimizationVerify {
const string R="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1 Generated/Optimization 01";
static Stage1OptimizationVerify(){EditorApplication.delayCall+=Verify;}
static void Verify(){if(!File.Exists(R+"/COMPACT.txt")||File.Exists(R+"/VERIFIED.txt"))return;var s=SceneManager.GetActiveScene();if(s.path!="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1.unity")return;try{
var root=s.GetRootGameObjects().First(o=>o.name=="STAGE 1 | Woodland Footrace");var combined=root.transform.Find("10 Optimized small decor - 12m cells");int count=0;long tris=0;foreach(var f in combined.GetComponentsInChildren<MeshFilter>()){if(!f.sharedMesh)throw new Exception("Missing combined mesh");if(f.sharedMesh.isReadable)throw new Exception("Mesh still CPU-readable");for(int i=0;i<f.sharedMesh.subMeshCount;i++)tris+=(long)f.sharedMesh.GetIndexCount(i)/3;count++;}
foreach(var c in root.GetComponentsInChildren<Camera>()){if(!c.name.StartsWith("07")&&!c.name.StartsWith("08"))continue;var rt=new RenderTexture(1600,1000,24);rt.Create();var prev=c.targetTexture;var active=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1600,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,1000),0,0);t.Apply();File.WriteAllBytes(R+"/Previews/Final-"+(c.name.StartsWith("07")?"Overview":"Trackside")+".png",t.EncodeToPNG());c.targetTexture=prev;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
EditorSceneManager.SaveScene(s);File.WriteAllText(R+"/VERIFIED.txt","Final saved scene. Combined meshes: "+count+"; triangles: "+tris+". CPU read/write disabled. Both cameras rendered successfully.\n");AssetDatabase.Refresh();
}catch(Exception e){File.WriteAllText(R+"/VERIFY.error",e.ToString());Debug.LogException(e);}}
}}
