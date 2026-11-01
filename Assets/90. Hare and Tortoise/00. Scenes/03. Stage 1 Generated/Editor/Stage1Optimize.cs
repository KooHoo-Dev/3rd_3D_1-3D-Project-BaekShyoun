using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Stage1Generated {
[InitializeOnLoad] public static class Stage1Optimize {
 const string ScenePath="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1.unity";
 const string Root="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1 Generated/Optimization 01";
 static Stage1Optimize(){EditorApplication.delayCall+=Run;}
 static bool Active(Renderer r){return r.enabled&&r.gameObject.activeInHierarchy;}
 static long Triangles(Mesh m){long n=0;for(int i=0;i<m.subMeshCount;i++)n+=(long)m.GetIndexCount(i)/3;return n;}
 static int Slots(IEnumerable<MeshRenderer> rr){return rr.Sum(r=>r.sharedMaterials.Length);}
 static bool Eligible(MeshRenderer r){var n=r.gameObject.name;return n.StartsWith("SM_Grass_")||n.StartsWith("SM_Flower_")||n.StartsWith("SM_Mushroom_")||n=="Small dirt fleck";}
 static void Record(Renderer r){if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 [MenuItem("Tools/Stage 1/Optimize Small Decor (Once)")]
 public static void Run(){
 if(File.Exists(Root+"/COMPLETE.txt")||File.Exists(Root+"/STARTED.txt")||EditorApplication.isPlayingOrWillChangePlaymode)return;
 var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)return;
 var root=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="STAGE 1 | Woodland Footrace");if(!root)return;
 try{
 foreach(var d in new[]{"Meshes","Backups","Previews"})Directory.CreateDirectory(Root+"/"+d);
 File.WriteAllText(Root+"/STARTED.txt",DateTime.Now.ToString("O"));
 File.Copy(ScenePath,Root+"/Backups/Stage1-disk-before.unity.bytes",false);
 if(!EditorSceneManager.SaveScene(scene,Root+"/Backups/Stage1-live-before.unity",true))throw new Exception("Live scene backup failed; stopping");
 AssetDatabase.Refresh();
 var all=root.GetComponentsInChildren<MeshRenderer>(true);var before=all.Where(Active).ToArray();var candidates=before.Where(Eligible).Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null&&!r.HasPropertyBlock()&&r.GetComponentInParent<LODGroup>()==null&&r.GetComponentInChildren<Collider>()==null&&r.GetComponents<MonoBehaviour>().Length==0).ToArray();
 var cameras=root.GetComponentsInChildren<Camera>(true);foreach(var c in cameras)Capture(c,"Before");
 int beforeShadow=before.Count(r=>r.shadowCastingMode!=ShadowCastingMode.Off);int beforeSlots=Slots(before);
 var holder=new GameObject("10 Optimized small decor - 12m cells");holder.transform.SetParent(root.transform,false);
 int cells=0,mergedSources=0;long sourceTris=0,combinedTris=0,totalVertices=0;var consumed=new List<MeshRenderer>();
 var groups=candidates.GroupBy(r=>new Vector2Int(Mathf.FloorToInt(r.bounds.center.x/12),Mathf.FloorToInt(r.bounds.center.z/12))).OrderBy(g=>g.Key.x).ThenBy(g=>g.Key.y);
 foreach(var group in groups){var renderers=group.ToArray();if(renderers.Length<2)continue;var pos=new Vector3((group.Key.x+.5f)*12,0,(group.Key.y+.5f)*12);var worldToCell=Matrix4x4.Translate(-pos);var byMaterial=new Dictionary<Material,List<CombineInstance>>();long inputTris=0;
 foreach(var r in renderers){var mesh=r.GetComponent<MeshFilter>().sharedMesh;if(r.sharedMaterials.Length!=mesh.subMeshCount)throw new Exception("Material/submesh mismatch: "+r.name);for(int s=0;s<mesh.subMeshCount;s++){var mat=r.sharedMaterials[s];if(!mat)throw new Exception("Missing material "+r.name);if(!byMaterial.ContainsKey(mat))byMaterial.Add(mat,new List<CombineInstance>());byMaterial[mat].Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=worldToCell*r.transform.localToWorldMatrix});}inputTris+=Triangles(mesh);}
 var ordered=byMaterial.OrderBy(k=>AssetDatabase.GetAssetPath(k.Key)).ToArray();var temp=new List<Mesh>();var finalParts=new List<CombineInstance>();foreach(var pair in ordered){var m=new Mesh{indexFormat=IndexFormat.UInt32};m.CombineMeshes(pair.Value.ToArray(),true,true,false);temp.Add(m);finalParts.Add(new CombineInstance{mesh=m,transform=Matrix4x4.identity});}
 string name="Decor_"+group.Key.x+"_"+group.Key.y;var combined=new Mesh{name=name,indexFormat=IndexFormat.UInt32};combined.CombineMeshes(finalParts.ToArray(),false,false,false);combined.RecalculateBounds();long outputTris=Triangles(combined);if(inputTris!=outputTris)throw new Exception("Triangle count mismatch in "+name);foreach(var t in temp)UnityEngine.Object.DestroyImmediate(t);
 AssetDatabase.CreateAsset(combined,Root+"/Meshes/"+name+".asset");var go=new GameObject(name);go.transform.SetParent(holder.transform,true);go.transform.position=pos;go.AddComponent<MeshFilter>().sharedMesh=combined;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterials=ordered.Select(v=>v.Key).ToArray();mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=true;mr.lightProbeUsage=LightProbeUsage.Off;mr.reflectionProbeUsage=ReflectionProbeUsage.Off;
 foreach(var r in renderers){r.enabled=false;Record(r);consumed.Add(r);}cells++;mergedSources+=renderers.Length;sourceTris+=inputTris;combinedTris+=outputTris;totalVertices+=combined.vertexCount;
 }
 // Retain source prefab instances for editing/rebuilding; only their renderers are disabled.
 var after=root.GetComponentsInChildren<MeshRenderer>(true).Where(Active).ToArray();int missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));int invalid=after.Sum(r=>r.sharedMaterials.Count(m=>!m||!m.shader||!m.shader.isSupported));if(missing>0||invalid>0)throw new Exception("Validation failed: missing scripts="+missing+", invalid materials="+invalid);
 foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{Root})){var m=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));if(m)AssetDatabase.SaveAssetIfDirty(m);}
 if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Optimized scene save failed");
 var sb=new StringBuilder();sb.AppendLine("Stage 1 optimization - structural measurements, NOT FPS benchmark");sb.AppendLine("Active MeshRenderers: "+before.Length+" -> "+after.Length);sb.AppendLine("Active material slots (not actual draw calls): "+beforeSlots+" -> "+Slots(after));sb.AppendLine("Shadow-casting MeshRenderers: "+beforeShadow+" -> "+after.Count(r=>r.shadowCastingMode!=ShadowCastingMode.Off));sb.AppendLine("Merged source renderers: "+mergedSources+"; spatial cells: "+cells+"; cell size: 12m");sb.AppendLine("Merged triangles: "+sourceTris+" -> "+combinedTris+" (must match)");sb.AppendLine("Combined vertices: "+totalVertices);sb.AppendLine("Missing scripts: "+missing+"; missing/unsupported materials: "+invalid);sb.AppendLine("Original source prefab renderers retained disabled in scene. Original assets unchanged.");sb.AppendLine("Trees, rocks, route, cameras, Terrain, colliders and project settings unchanged.");sb.AppendLine("Small decor receives shadows but no longer casts shadows.");sb.AppendLine("Tradeoff: additional combined mesh memory; culling is per 12m cell, not per plant.");sb.AppendLine("Live pre-optimization scene (including unsaved changes) backed up separately from disk version.");sb.AppendLine("No platform-specific FPS guarantee; player-build CPU/GPU profiling still needed.");File.WriteAllText(Root+"/OptimizationReport.txt",sb.ToString());
 File.WriteAllText(Root+"/COMPLETE.txt",DateTime.Now.ToString("O"));
 EditorApplication.delayCall+=()=>{foreach(var c in cameras)Capture(c,"After");AssetDatabase.Refresh();};Debug.Log("STAGE1_OPTIMIZATION_COMPLETE\n"+sb);
 }catch(Exception e){File.WriteAllText(Root+"/ERROR.txt",e.ToString());Debug.LogException(e);}}
 static void Capture(Camera c,string phase){if(!c.name.StartsWith("07")&&!c.name.StartsWith("08"))return;var prevTarget=c.targetTexture;var prevActive=RenderTexture.active;var rt=new RenderTexture(1600,1000,24);Texture2D tex=null;try{rt.Create();c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Root+"/Previews/"+phase+"-"+(c.name.StartsWith("07")?"Overview":"Trackside")+".png",tex.EncodeToPNG());}finally{c.targetTexture=prevTarget;RenderTexture.active=prevActive;if(tex)UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}}
