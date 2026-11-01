using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Stage1Generated {
[InitializeOnLoad] public static class ForestReview {
const string R="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1 Generated";
static ForestReview(){EditorApplication.delayCall+=Review;}
static void Review(){if(!File.Exists(R+"/FINISH.success")||File.Exists(R+"/REVIEW.success"))return;var s=SceneManager.GetActiveScene();if(!s.path.EndsWith("03. Stage 1.unity"))return;try{
var root=s.GetRootGameObjects().First(o=>o.name.StartsWith("STAGE 1"));var line=root.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("Start Finish"));float a=-1.936f;var p=new Vector3(42*Mathf.Cos(a),.07f,30*Mathf.Sin(a));line.position=p;line.localScale=new Vector3(.45f,.045f,7.9f);line.rotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(a)/42,0,Mathf.Sin(a)/30));
var cam=root.GetComponentsInChildren<Camera>().First(c=>c.name.StartsWith("07"));cam.orthographicSize=39;cam.transform.position=new Vector3(0,66,-78);cam.transform.LookAt(Vector3.zero);
int missing=0;foreach(var t in root.GetComponentsInChildren<Transform>())missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);int badMaterials=root.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Count(m=>m==null||m.shader==null||!m.shader.isSupported));
EditorSceneManager.SaveScene(s);File.WriteAllText(R+"/Documentation/Validation.txt","Missing scripts: "+missing+"\nMissing/unsupported renderer materials: "+badMaterials+"\nTerrain collider: "+(root.GetComponentInChildren<TerrainCollider>()!=null)+"\nScene saved: "+s.path+"\n");File.WriteAllText(R+"/REVIEW.success","Reviewed");
EditorApplication.delayCall+=()=>{foreach(var c in root.GetComponentsInChildren<Camera>()){var rt=new RenderTexture(1600,1000,24);rt.Create();c.targetTexture=rt;c.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(R+"/Previews/"+(c.name.StartsWith("07")?"Overview":"Trackside")+".png",tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}AssetDatabase.Refresh();};
}catch(Exception e){Debug.LogException(e);}}
}}
