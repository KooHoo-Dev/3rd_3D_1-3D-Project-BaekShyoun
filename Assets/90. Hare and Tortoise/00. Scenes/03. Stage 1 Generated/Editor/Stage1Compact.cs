using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
namespace Stage1Generated {
[InitializeOnLoad] public static class Stage1Compact {
const string R="Assets/90. Hare and Tortoise/00. Scenes/03. Stage 1 Generated/Optimization 01";
static Stage1Compact(){EditorApplication.delayCall+=Run;}
static T[] Remap<T>(T[] input,List<int> used){return used.Select(i=>input[i]).ToArray();}
static void Run(){if(!File.Exists(R+"/COMPLETE.txt")||File.Exists(R+"/COMPACT.txt"))return;try{
long before=0,after=0,triangles=0,bytes=0;int meshes=0;
foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{R+"/Meshes"})){
var m=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));if(!m.isReadable)continue;before+=m.vertexCount;var pos=m.vertices;var normal=m.normals;var tangent=m.tangents;var color=m.colors;var uv=new List<Vector4>[8];var dim=new int[8];for(int c=0;c<8;c++){uv[c]=new List<Vector4>();m.GetUVs(c,uv[c]);dim[c]=m.GetVertexAttributeDimension((VertexAttribute)((int)VertexAttribute.TexCoord0+c));}
var map=new int[m.vertexCount];for(int i=0;i<map.Length;i++)map[i]=-1;var used=new List<int>();var sub=new int[m.subMeshCount][];for(int s=0;s<sub.Length;s++){sub[s]=m.GetTriangles(s);triangles+=sub[s].Length/3;for(int j=0;j<sub[s].Length;j++){int old=sub[s][j];if(map[old]<0){map[old]=used.Count;used.Add(old);}sub[s][j]=map[old];}}
m.Clear();m.indexFormat=used.Count<=65535?IndexFormat.UInt16:IndexFormat.UInt32;m.vertices=Remap(pos,used);if(normal.Length==pos.Length)m.normals=Remap(normal,used);if(tangent.Length==pos.Length)m.tangents=Remap(tangent,used);if(color.Length==pos.Length)m.colors=Remap(color,used);
for(int c=0;c<8;c++){if(uv[c].Count!=pos.Length)continue;if(dim[c]==2)m.SetUVs(c,used.Select(i=>(Vector2)uv[c][i]).ToList());else if(dim[c]==3)m.SetUVs(c,used.Select(i=>(Vector3)uv[c][i]).ToList());else m.SetUVs(c,used.Select(i=>uv[c][i]).ToList());}
m.subMeshCount=sub.Length;for(int s=0;s<sub.Length;s++)m.SetTriangles(sub[s],s);m.RecalculateBounds();after+=m.vertexCount;for(int b=0;b<m.vertexBufferCount;b++)bytes+=(long)m.GetVertexBufferStride(b)*m.vertexCount;bytes+=sub.Sum(a=>(long)a.Length)*(m.indexFormat==IndexFormat.UInt16?2:4);
// Rendering-only combined assets do not need CPU-readable copies in player builds.
m.UploadMeshData(true);EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);meshes++;
}
File.WriteAllText(R+"/COMPACT.txt","Combined meshes: "+meshes+"\nVertices: "+before+" -> "+after+"\nTriangles preserved: "+triangles+"\nCombined GPU vertex/index payload bytes: "+bytes+"\nCPU read/write disabled for combined meshes. Original source meshes untouched.\n");AssetDatabase.Refresh();
}catch(Exception e){File.WriteAllText(R+"/COMPACT.error",e.ToString());Debug.LogException(e);}}
}}
