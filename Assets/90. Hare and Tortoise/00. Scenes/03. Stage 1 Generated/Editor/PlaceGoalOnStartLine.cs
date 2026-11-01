using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Stage1Generated {
[InitializeOnLoad] public static class PlaceGoalOnStartLine {
const string R="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1 Generated/GoalLine Placement";
static PlaceGoalOnStartLine(){EditorApplication.delayCall+=Run;}
static void Run(){if(File.Exists(R+"/Complete.txt")||File.Exists(R+"/Error.txt")||EditorApplication.isPlayingOrWillChangePlaymode)return;var s=SceneManager.GetActiveScene();if(s.path!="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1.unity")return;
try{Directory.CreateDirectory(R);var all=s.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();var goals=all.Where(t=>t.name=="RaceManager(GoalLine)").ToArray();var lines=all.Where(t=>t.name=="Start Finish - chalk stripe").ToArray();if(goals.Length!=1||lines.Length!=1)throw new Exception("Expected one goal and one startline; found "+goals.Length+" and "+lines.Length);
var g=goals[0];var line=lines[0];var box=g.GetComponent<BoxCollider>();if(!box||box.size.x<=0||box.size.y<=0||box.size.z<=0)throw new Exception("Goal BoxCollider missing or invalid");if(g.parent&&(g.parent.lossyScale-Vector3.one).sqrMagnitude>.00001f)throw new Exception("Non-unit parent scale requires manual inspection");var race=g.GetComponents<MonoBehaviour>().FirstOrDefault(c=>c&&c.GetType().Name=="RaceManager");string originalRace=race?EditorJsonUtility.ToJson(race):"";string originalBox=EditorJsonUtility.ToJson(box);
if(!EditorSceneManager.SaveScene(s,R+"/Stage1-before-goalline.unity",true))throw new Exception("Backup failed");string before="Position: "+g.position+"; rotation: "+g.eulerAngles+"; scale: "+g.localScale;
float height=Mathf.Max(1,Mathf.Abs(g.lossyScale.y*box.size.y));float width=Mathf.Abs(line.lossyScale.z);float thickness=Mathf.Max(.3f,Mathf.Abs(line.lossyScale.x));float ground=0;var terrain=s.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Terrain>()).FirstOrDefault();if(terrain)ground=terrain.SampleHeight(line.position)+terrain.transform.position.y;
Undo.RecordObject(g,"Align GoalLine to white startline");g.rotation=line.rotation*Quaternion.Euler(0,90,0);g.localScale=new Vector3(width/box.size.x,height/box.size.y,thickness/box.size.z);g.position=new Vector3(line.position.x,ground+height*.5f,line.position.z)-g.TransformVector(box.center);if(PrefabUtility.IsPartOfPrefabInstance(g))PrefabUtility.RecordPrefabInstancePropertyModifications(g);
if((race?EditorJsonUtility.ToJson(race):"")!=originalRace||EditorJsonUtility.ToJson(box)!=originalBox)throw new Exception("Unexpected component change");Physics.SyncTransforms();var center=g.TransformPoint(box.center);if(Vector2.Distance(new Vector2(center.x,center.z),new Vector2(line.position.x,line.position.z))>.001f)throw new Exception("Goal not centered");if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");
File.WriteAllText(R+"/Complete.txt",before+"\nAfter position: "+g.position.ToString("F4")+"; rotation: "+g.eulerAngles.ToString("F3")+"; scale: "+g.localScale.ToString("F3")+"\nTrigger dimensions: "+width+" x "+height+" x "+thickness+" m; isTrigger="+box.isTrigger+"; collider enabled="+box.enabled+"\nRaceManager and BoxCollider serialized properties unchanged. Source prefab untouched. Scene saved.\n");Selection.activeGameObject=g.gameObject;if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.Frame(new Bounds(center,new Vector3(13,8,13)),false);AssetDatabase.Refresh();Debug.Log("GoalLine aligned with startline and saved.");
}catch(Exception e){Directory.CreateDirectory(R);File.WriteAllText(R+"/Error.txt",e.ToString());Debug.LogException(e);}}
}}
