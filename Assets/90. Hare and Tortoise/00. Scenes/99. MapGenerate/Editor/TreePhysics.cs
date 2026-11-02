using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Reflection;
using UnityEngine.AI;

namespace HareAndTortoise.MapGenerate
{
    // Static low-poly mesh collision follows the actual trunk, branches and canopy.
    // Source prefab meshes/materials and baked NavMesh assets are never modified.
    internal static class TreePhysics
    {
        const string Root=TutorialWoodland.Root;
        static readonly string[] Names={"00. Tutorial A","01. Tutorial B","02. Tutorial C","03. Stage 1"};
        static Scene scene;
        static PhysicsMaterial material;
        static readonly StringBuilder report=new StringBuilder();
        [InitializeOnLoadMethod]static void Schedule()
        {
            EditorApplication.playModeStateChanged+=OnPlayState;
            EditorApplication.delayCall+=Run;
        }
        static void OnPlayState(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("TreePhysics.PendingTests",false))EditorApplication.delayCall+=PlayTests;
        }
        static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Root+"/TREE_PHYSICS.request")||File.Exists(Root+"/Reports/TreePhysicsComplete.txt"))return;
            if(File.Exists(Root+"/Reports/TreePhysicsInstalled.txt")){StartTests();return;}
            try{Build();}catch(Exception e){File.WriteAllText(Root+"/Reports/TreePhysicsError.txt",e.ToString());Debug.LogException(e);}
        }
        static Transform Find(string name)
        {
            GameObject[] roots=scene.GetRootGameObjects();
            for(int i=0;i<roots.Length;i++){Transform[] ts=roots[i].GetComponentsInChildren<Transform>(true);for(int j=0;j<ts.Length;j++)if(ts[j].name==name)return ts[j];}return null;
        }
        static void AddTrees(Transform parent,List<Transform> trees)
        {
            if(parent==null)return;
            for(int i=0;i<parent.childCount;i++){Transform t=parent.GetChild(i);if(t.name.StartsWith("SM_Base_Tree_")||t.name.StartsWith("SM_Pine_"))trees.Add(t);}
        }
        static void Build()
        {
            Directory.CreateDirectory(Root+"/Backups/Before Tree Collision");
            string matPath=Root+"/Materials/Tree Sliding.physicMaterial";
            material=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(matPath);
            if(material==null){material=new PhysicsMaterial("Tree Sliding"){dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};AssetDatabase.CreateAsset(material,matPath);}
            Scene original=SceneManager.GetActiveScene();
            report.AppendLine("4-2: tree collision and fast ragdoll protection\nStatic non-convex collision uses existing low-poly meshes; no dynamic tree bodies.\n");
            for(int n=0;n<Names.Length;n++)
            {
                string path=TutorialWoodland.Scenes+Names[n]+".unity";
                scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    if(scene.isDirty)throw new Exception("Save scene edits first: "+Names[n]);
                    SceneManager.SetActiveScene(scene);
                    string backup=Root+"/Backups/Before Tree Collision/"+Names[n]+".unity.txt";if(!File.Exists(backup))File.Copy(path,backup);
                    var trees=new List<Transform>();AddTrees(Find(n==3?"02 Forest - prefab instances":"01 Canopy - low poly prefab instances"),trees);
                    Transform sleep=Find("SleepTree");if(sleep!=null)trees.Add(sleep);
                    int added=0,disabled=0;var allowed=new HashSet<Component>();
                    var before=new Dictionary<Component,string>();GameObject[] roots=scene.GetRootGameObjects();
                    for(int i=0;i<roots.Length;i++){Component[] cs=roots[i].GetComponentsInChildren<Component>(true);for(int j=0;j<cs.Length;j++)if(cs[j]!=null)before.Add(cs[j],EditorJsonUtility.ToJson(cs[j]));}
                    for(int i=0;i<trees.Count;i++)
                    {
                        Transform tree=trees[i];
                        MeshCollider[] existing=tree.GetComponentsInChildren<MeshCollider>(true);
                        bool installed=false;for(int c=0;c<existing.Length;c++)if(existing[c].name=="Tree Collision"){installed=true;added++;}
                        if(installed)continue;
                        Collider[] old=tree.GetComponentsInChildren<Collider>(true);
                        for(int c=0;c<old.Length;c++)if(old[c].enabled&&!old[c].isTrigger){old[c].enabled=false;disabled++;allowed.Add(old[c]);if(PrefabUtility.IsPartOfPrefabInstance(old[c]))PrefabUtility.RecordPrefabInstancePropertyModifications(old[c]);}
                        MeshFilter[] meshes=tree.GetComponentsInChildren<MeshFilter>(true);
                        for(int m=0;m<meshes.Length;m++)
                        {
                            if(meshes[m].sharedMesh==null)continue;
                            var go=new GameObject("Tree Collision");SceneManager.MoveGameObjectToScene(go,scene);go.layer=meshes[m].gameObject.layer;go.transform.SetParent(meshes[m].transform,false);allowed.Add(meshes[m].transform);
                            MeshCollider collider=go.AddComponent<MeshCollider>();collider.sharedMesh=meshes[m].sharedMesh;collider.convex=false;collider.sharedMaterial=material;collider.contactOffset=.01f;added++;
                        }
                    }
                    Physics.SyncTransforms();
                    var keys=new List<Component>(before.Keys);for(int i=0;i<keys.Count;i++)if(!allowed.Contains(keys[i])&&(keys[i]==null||EditorJsonUtility.ToJson(keys[i])!=before[keys[i]]))throw new Exception("Unintended original component change: "+keys[i]);
                    report.AppendLine(Names[n]+": trees="+trees.Count+", static mesh colliders="+added+", old trunk proxies disabled="+disabled+", original unrelated components matched="+(keys.Count-allowed.Count));
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                finally{if(original.isLoaded)SceneManager.SetActiveScene(original);if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            AssetDatabase.SaveAssets();File.WriteAllText(Root+"/Reports/TreePhysicsReport.txt",report.ToString());File.WriteAllText(Root+"/Reports/TreePhysicsInstalled.txt",DateTime.Now.ToString("O"));AssetDatabase.Refresh();Debug.Log("TREE_PHYSICS_INSTALLED\n"+report);StartTests();
        }
        static void StartTests()
        {
            SessionState.SetBool("TreePhysics.PendingTests",true);
            EditorApplication.isPlaying=true;
        }
        static async void PlayTests()
        {
            SessionState.SetBool("TreePhysics.PendingTests",false);
            bool previousBackground=Application.runInBackground;
            Application.runInBackground=true;
            try
            {
                material=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root+"/Materials/Tree Sliding.physicMaterial");
                string previousReport=File.ReadAllText(Root+"/Reports/TreePhysicsReport.txt");int firstPass=previousReport.IndexOf("  PASS ",StringComparison.Ordinal);
                report.Clear();report.Append(firstPass<0?previousReport:previousReport.Substring(0,firstPass));
                for(int n=2;n<4;n++)
                {
                    AsyncOperation loading=SceneManager.LoadSceneAsync(Names[n],LoadSceneMode.Single);
                    while(!loading.isDone)await UniTask.Yield();
                    await UniTask.NextFrame();
                    scene=SceneManager.GetSceneByName(Names[n]);
                    if(!scene.isLoaded)throw new Exception("Physics test scene did not finish loading: "+Names[n]);
                    var trees=new List<Transform>();AddTrees(Find(n==3?"02 Forest - prefab instances":"01 Canopy - low poly prefab instances"),trees);
                    Transform hare=Find("Hare");int tested=0;bool pine=false,broad=false;
                    if(trees.Count<2||hare==null)throw new Exception("Physics test inputs missing: "+Names[n]);
                    for(int i=0;i<trees.Count;i++)
                    {
                        bool isPine=trees[i].name.StartsWith("SM_Pine_");if((isPine&&pine)||(!isPine&&broad))continue;
                        TestTree(trees[i],hare,Names[n]);
                        if(tested==0)await TestRecovery(trees[i],hare,Names[n]);
                        File.WriteAllText(Root+"/Reports/TreePhysicsReport.txt",report.ToString());tested++;if(isPine)pine=true;else broad=true;if(tested==2)break;
                    }
                    Transform sleep=Find("SleepTree");if(sleep!=null)TestTree(sleep,hare,Names[n]+" SleepTree");
                }
                File.WriteAllText(Root+"/Reports/TreePhysicsReport.txt",report.ToString());File.WriteAllText(Root+"/Reports/TreePhysicsComplete.txt",DateTime.Now.ToString("O"));Debug.Log("TREE_PHYSICS_COMPLETE\n"+report);
            }
            catch(Exception e){File.WriteAllText(Root+"/Reports/TreePhysicsError.txt",e.ToString());Debug.LogException(e);}
            finally{Application.runInBackground=previousBackground;EditorApplication.isPlaying=false;}
        }
        static async UniTask TestRecovery(Transform tree,Transform hare,string label)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            HareController controller=hare.GetComponent<HareController>();NavMeshAgent agent=hare.GetComponent<NavMeshAgent>();
            var type=typeof(HareController);Rigidbody hips=(Rigidbody)type.GetField("ragdollHips",flags).GetValue(controller);
            MeshCollider mc=tree.GetComponentInChildren<MeshCollider>();Bounds b=mc.bounds;
            Vector3 origin=b.center;origin.y=b.max.y+3;
            if(!mc.Raycast(new Ray(origin,Vector3.down),out RaycastHit hit,b.size.y+6))throw new Exception("Recovery test canopy ray missed");
            type.GetField("attackedDuration",flags).SetValue(controller,.4f);
            typeof(RaceManager).GetProperty("canStartHare").GetSetMethod(true).Invoke(RaceManager.Instance,new object[]{true});
            Vector3 prev=agent.nextPosition;
            controller.ApplyKnockback(hare.position-Vector3.right*2);
            hare.position+=hit.point+Vector3.up*1.2f-hips.position;
            Rigidbody[] bones=hips.GetComponentsInChildren<Rigidbody>(true);
            for(int i=0;i<bones.Length;i++){bones[i].linearVelocity=Vector3.zero;bones[i].angularVelocity=Vector3.zero;}
            Physics.SyncTransforms();
            float elapsed=0;
            while(elapsed<25)
            {
                await UniTask.NextFrame();elapsed+=Time.unscaledDeltaTime;
                string state=type.GetField("state",flags).GetValue(controller).ToString();
                if(agent.enabled&&agent.isOnNavMesh&&(state=="Returning"||state=="ToGoal"))
                {
                    Terrain terrain=Terrain.activeTerrain;float ground=terrain.SampleHeight(hare.position)+terrain.transform.position.y;
                    if(Mathf.Abs(agent.nextPosition.y-ground)>.6f)throw new Exception("Recovered above the ground instead of descending: "+label);
                    report.AppendLine("  PASS "+label+" live canopy recovery to ground NavMesh, state="+state+", elapsed="+elapsed.ToString("F2")+"s; original return point="+prev);
                    return;
                }
            }
            throw new Exception("Live canopy recovery timed out: "+label+" state="+type.GetField("state",flags).GetValue(controller));
        }
        static void TestTree(Transform source,Transform hare,string label)
        {
            Scene test=SceneManager.CreateScene("Temporary isolated tree physics "+Guid.NewGuid().ToString("N"),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            try
            {
                PhysicsScene physics=test.GetPhysicsScene();if(!physics.IsValid())throw new Exception("Isolated physics scene unavailable");
                var targetColliders=new List<MeshCollider>();MeshCollider[] sourceColliders=source.GetComponentsInChildren<MeshCollider>();
                Bounds bounds=new Bounds();bool first=true;
                for(int i=0;i<sourceColliders.Length;i++)
                {
                    if(sourceColliders[i].name!="Tree Collision")continue;
                    Transform st=sourceColliders[i].transform;var g=new GameObject("Test tree");SceneManager.MoveGameObjectToScene(g,test);g.transform.SetPositionAndRotation(st.position,st.rotation);g.transform.localScale=st.lossyScale;
                    MeshCollider mc=g.AddComponent<MeshCollider>();mc.sharedMesh=sourceColliders[i].sharedMesh;mc.sharedMaterial=material;targetColliders.Add(mc);
                    if(first){bounds=sourceColliders[i].bounds;first=false;}else bounds.Encapsulate(sourceColliders[i].bounds);
                }
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(ground,test);ground.transform.position=new Vector3(bounds.center.x,bounds.min.y-.11f,bounds.center.z);ground.transform.localScale=new Vector3(80,.2f,80);
                Physics.SyncTransforms();
                RaycastHit top=new RaycastHit();bool found=false;
                for(int sample=0;sample<9;sample++)
                {
                    Vector3 p=bounds.center+new Vector3((sample%3-1)*bounds.extents.x*.35f,0,(sample/3-1)*bounds.extents.z*.35f);p.y=bounds.max.y+2;
                    for(int i=0;i<targetColliders.Count;i++)if(targetColliders[i].Raycast(new Ray(p,Vector3.down),out RaycastHit hit,bounds.size.y+4)&&(!found||hit.point.y>top.point.y)){top=hit;found=true;}
                }
                if(!found)throw new Exception("Cannot find canopy test surface: "+source.name);
                Probe(test,physics,top.point+Vector3.up*2,Vector3.down*35,true,top.point.y,"canopy drop",label,source.name);
                Vector3 from=new Vector3(bounds.min.x-2,source.position.y+.65f,source.position.z);bool trunk=false;RaycastHit trunkHit=new RaycastHit();
                for(int i=0;i<targetColliders.Count;i++)if(targetColliders[i].Raycast(new Ray(from,Vector3.right),out trunkHit,bounds.size.x+5)){trunk=true;break;}
                if(trunk)Probe(test,physics,trunkHit.point-Vector3.right*1.5f,Vector3.right*25,false,trunkHit.point.y,"trunk strike",label,source.name);
                TestRagdoll(test,physics,top.point,hare,label,source.name,bounds.min.y);
            }
            finally{SceneManager.UnloadSceneAsync(test);}
        }
        static void Probe(Scene test,PhysicsScene physics,Vector3 position,Vector3 velocity,bool gravity,float contactHeight,string testName,string label,string tree)
        {
            var g=new GameObject("CCD capsule test");SceneManager.MoveGameObjectToScene(g,test);g.transform.position=position;
            CapsuleCollider collider=g.AddComponent<CapsuleCollider>();collider.radius=.1f;collider.height=.4f;
            Rigidbody body=g.AddComponent<Rigidbody>();body.useGravity=gravity;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=velocity;
            Physics.SyncTransforms();bool contact=false;
            for(int step=0;step<150;step++)
            {
                Vector3 before=body.linearVelocity;physics.Simulate(.02f);
                if(gravity){if(body.position.y>contactHeight-.7f&&before.y<-3&&body.linearVelocity.y-before.y>2)contact=true;}
                else if(before.x>3&&before.x-body.linearVelocity.x>2)contact=true;
            }
            UnityEngine.Object.DestroyImmediate(g);
            if(!contact)throw new Exception("CCD test failed: "+label+" "+tree+" "+testName);
            report.AppendLine("  PASS "+label+" "+tree+" "+testName+" at "+velocity.magnitude+"m/s");
        }
        static void TestRagdoll(Scene test,PhysicsScene physics,Vector3 top,Transform source,string label,string tree,float groundY)
        {
            GameObject clone=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(clone,test);
            var controller=clone.GetComponent<HareController>();var so=new SerializedObject(controller);Rigidbody hips=so.FindProperty("ragdollHips").objectReferenceValue as Rigidbody;
            if(hips==null)throw new Exception("Missing ragdoll in physics test");
            var agent=clone.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)UnityEngine.Object.DestroyImmediate(agent);
            Animator animator=clone.GetComponent<Animator>();if(animator!=null){animator.Rebind();animator.Update(0);animator.enabled=false;}
            MonoBehaviour[] scripts=clone.GetComponentsInChildren<MonoBehaviour>(true);for(int i=scripts.Length-1;i>=0;i--)if(scripts[i]!=null)UnityEngine.Object.DestroyImmediate(scripts[i]);
            Rigidbody root=clone.GetComponent<Rigidbody>();if(root!=null)root.isKinematic=true;Collider rootCollider=clone.GetComponent<Collider>();if(rootCollider!=null)rootCollider.enabled=false;
            clone.transform.position+=top+Vector3.up*2-hips.position;
            Rigidbody[] bones=hips.GetComponentsInChildren<Rigidbody>(true);
            for(int i=0;i<bones.Length;i++){bones[i].isKinematic=false;bones[i].detectCollisions=true;bones[i].collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;bones[i].linearVelocity=Vector3.down*15;}
            Physics.SyncTransforms();bool contact=false;
            for(int step=0;step<250;step++)
            {
                float before=hips.linearVelocity.y;physics.Simulate(.02f);
                if(hips.position.y>groundY+.75f&&before<-2&&hips.linearVelocity.y-before>1.5f)contact=true;
            }
            UnityEngine.Object.DestroyImmediate(clone);
            if(!contact)throw new Exception("Actual hare ragdoll did not contact canopy: "+label+" "+tree);
            report.AppendLine("  PASS "+label+" "+tree+" actual hare ragdoll drop, "+bones.Length+" bodies, 15m/s");
        }
    }
}
