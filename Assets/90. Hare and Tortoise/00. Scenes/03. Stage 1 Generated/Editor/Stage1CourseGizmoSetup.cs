using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Stage1Generated {
[InitializeOnLoad] public static class Stage1CourseGizmoSetup {
const string R="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1 Generated/Course Gizmos";
static Stage1CourseGizmoSetup(){EditorApplication.delayCall+=Run;}
static void Run(){if(File.Exists(R+"/Complete.txt")||File.Exists(R+"/Error.txt")||EditorApplication.isPlayingOrWillChangePlaymode)return;var s=SceneManager.GetActiveScene();if(s.path!="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1.unity")return;try{
var root=s.GetRootGameObjects().Single(o=>o.name=="STAGE 1 | Woodland Footrace");if(root.transform.Find("11 Course Gizmos"))throw new Exception("Course gizmos already exist");var line=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Start Finish - chalk stripe");Directory.CreateDirectory(R);if(!EditorSceneManager.SaveScene(s,R+"/Stage1-before-course-gizmos.unity",true))throw new Exception("Backup failed");var holder=new GameObject("11 Course Gizmos");Undo.RegisterCreatedObjectUndo(holder,"Add two course guides");holder.transform.SetParent(root.transform,false);float start=Mathf.Atan2(line.position.z/30f,line.position.x/42f);
for(int lane=0;lane<2;lane++){float offset=lane==0?-2f:2f;var go=new GameObject(lane==0?"Inline - Cyan":"Outline - Magenta");go.transform.SetParent(holder.transform,false);var guide=go.AddComponent<Stage1CourseGizmo>();guide.lineColor=lane==0?new Color(.05f,1f,1f):new Color(1f,.15f,.8f);guide.points=new Vector3[256];for(int i=0;i<256;i++){float a=start+i*Mathf.PI*2/256;var center=new Vector3(42*Mathf.Cos(a),.16f,30*Mathf.Sin(a));var normal=new Vector3(Mathf.Cos(a)/42,0,Mathf.Sin(a)/30).normalized;guide.points[i]=holder.transform.InverseTransformPoint(center+normal*offset);}for(int i=0;i<256;i++){var p=guide.points[i];if(float.IsNaN(p.x)||Vector3.Distance(p,guide.points[(i+1)%256])>2f)throw new Exception("Invalid guide segment");}}
EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");Selection.activeGameObject=holder;if(SceneView.lastActiveSceneView){SceneView.lastActiveSceneView.drawGizmos=true;SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,0),Quaternion.Euler(90,0,0),62,true,true);}SceneView.RepaintAll();File.WriteAllText(R+"/Complete.txt","Two closed 256-point guide loops. Inline offset -2m, outline +2m from course centerline; height 0.16m. Start angle="+start+" radians, aligned to white line. Gizmos only; no runtime movement, NavMesh, collider or LineRenderer added.\n");AssetDatabase.Refresh();
}catch(Exception e){Directory.CreateDirectory(R);File.WriteAllText(R+"/Error.txt",e.ToString());Debug.LogException(e);}}
}}
