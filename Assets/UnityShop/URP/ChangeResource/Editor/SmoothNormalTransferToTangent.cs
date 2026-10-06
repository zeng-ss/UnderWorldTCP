using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace TA
{
    ///将smoothNormalMesh的法线，转移到mesh的切线中，
    ///其中smoothNormalMesh的法线是经过处理过的平滑后的法线

    public class SmoothNormalTransferToTangent: MonoBehaviour
    {
        public static Mesh CombineMeshNormalToTangent(Mesh mesh, Mesh smoothNormalMesh)
        {
            // Mesh smoothNormalMesh = SmoothNormalObj.GetComponent<MeshFilter>().mesh;
            Dictionary<Vector3, Vector3> dic = new Dictionary<Vector3, Vector3>();

            for (int i = 0; i < smoothNormalMesh.normals.Length; i++)
            {
                Vector3 pos = smoothNormalMesh.vertices[i];
                Vector3 nor = smoothNormalMesh.normals[i];
                if (!dic.ContainsKey(pos))
                {
                    dic.Add(pos,nor);
                }
            }

            Mesh combineMesh = new Mesh();

            Vector3[] normals = new Vector3[mesh.normals.Length];
            Vector4[] tangents = new Vector4[mesh.tangents.Length];
            
            for (int i = 0; i < mesh.tangents.Length; i++)
            {
                // normals[i] = Vector3.right;
                Vector3 pos = mesh.vertices[i];

                if (dic.ContainsKey(pos))
                {
                    tangents[i] = dic[pos];
                    tangents[i].w = 0;
                }
            }
            
            // Debug.Log("keys:"+dic.Keys.Count);
            
            combineMesh.vertices = mesh.vertices;
            combineMesh.normals = mesh.normals;
            combineMesh.tangents = tangents;
            combineMesh.uv = mesh.uv;
            combineMesh.triangles = mesh.triangles;

            return combineMesh;
        }

        public static void SaveMesh(Mesh mesh,string dir, string filename)
        {
            AssetDatabase.CreateAsset(mesh, $"{dir}/{filename}_{mesh.name}_smooth.asset");
        }

        [MenuItem("TA/Tools/UpdateTreeFacing")]
        public static void UpdateTreeFacing()
        {
            if(Selection.gameObjects.Length==0)
            {
                Debug.LogError("No Mesh");
                return;
            }

            GameObject MeshGo =Selection.gameObjects[0];
            var MeshGoInstance = GameObject.Instantiate(MeshGo);

            var path = AssetDatabase.GetAssetPath(Selection.gameObjects[0]);
            var dir = Path.GetDirectoryName(path);
            var filename = Path.GetFileNameWithoutExtension(path);

            var meshfilters = MeshGoInstance.GetComponentsInChildren<MeshFilter>();
            if(meshfilters[0].sharedMesh.subMeshCount!=2)
            {
                Debug.Log("subMeshCount!=2");
                return;
            }
            var mesh = UpdateTreeMeshFacing(meshfilters[0].sharedMesh);
            // SaveMesh(mesh, dir, MeshGo.name + "_SmoothMesh");
            GameObject.DestroyImmediate( MeshGoInstance);
        }

        public static Mesh UpdateTreeMeshFacing(Mesh mesh)
        {
            Mesh combineMesh = new Mesh();
            // Vector4[] tangents = new Vector4[mesh.tangents.Length];
            Vector4[] tangents = mesh.tangents.Clone() as Vector4[];
            // Color[] colors = new Color[mesh.tangents.Length];

            var subMesh0 = mesh.GetSubMesh(0);
            var subMesh1 = mesh.GetSubMesh(1);

            Debug.Log("subMesh0.indexStart:"+subMesh0.indexStart);
            Debug.Log("subMesh0.indexCount:"+subMesh0.indexCount);
            Debug.Log("subMesh1.indexStart:"+subMesh1.indexStart);
            Debug.Log("subMesh1.indexCount:"+subMesh1.indexCount);
            Debug.Log("subMesh0.vertexCount:"+subMesh0.vertexCount);
            Debug.Log("subMesh1.vertexCount:"+subMesh1.vertexCount);
            Debug.Log("mesh.tangents.Length:"+mesh.tangents.Length);
            Debug.Log("mesh.colors.Length:"+mesh.colors.Length);
            Debug.Log("mesh.vertices:"+mesh.vertices.Length);
            Debug.Log("mesh.triangles:"+mesh.triangles.Length);
            return null;
            /*
            //submesh0: facing 
            for (int i = subMesh0.indexStart; i < subMesh0.indexCount; i++)
            {
                Vector4 tangent = mesh.tangents[i];
                tangent.w = (tangent.w==-1f?-0.5f:0.5f);
                tangents[i] = tangent;
            }
            //submesh1: static 
            for (int i = subMesh1.indexStart; i < subMesh1.indexCount; i++)
            {
                tangents[i] = mesh.tangents[i];
            }
            */

            /*for (int i = 0; i < mesh.triangles.Length; i++)
            {
                int index = mesh.triangles[i];
                //submesh0: facing 
                if(index>= subMesh0.indexStart && index<subMesh0.indexCount)
                {
                    tangents[index].w = (tangents[index].w==-1f?-0.5f:0.5f);  
                }
            }

            Debug.Log("tangents:"+tangents[0]);

            combineMesh.vertices = mesh.vertices;
            combineMesh.normals = mesh.normals;
            combineMesh.tangents = tangents;
            combineMesh.uv = mesh.uv;
            combineMesh.uv2 = mesh.uv2;
            combineMesh.triangles = mesh.triangles;
            combineMesh.colors = mesh.colors;

            return combineMesh;*/
        }

        [MenuItem("TA/Tools/UpdateTreePrefab")]
        public static void UpdateTreeMeshPrefab()
        {
             if (Selection.gameObjects.Length != 0)
             {
                string guid;
                long fileid;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Selection.gameObjects[0],out guid,out fileid);
                Debug.Log("guid:"+guid);
                Debug.Log("fileid:"+fileid);
             }
        }

        [MenuItem("TA/Tools/合并平滑法线 导出Mesh(仅平滑法线的Mesh需命名以_SmoothNomral结尾)")]
        public static void NormalTransfer()
        {
            if (Selection.gameObjects.Length != 2)
            {
                Debug.LogError("需要选择两个模型");
                return;
            }
            
            bool first = Selection.gameObjects[0].name.EndsWith("_SmoothNomral");
            bool second = Selection.gameObjects[1].name.EndsWith("_SmoothNomral");

            int bit1 = first ? 1 : 0;
            int bit2 = second ? 1 : 0;
            
            if ((bit1+bit2)!=1)
            {
                Debug.LogError("需要选择两个模型,仅平滑法线的Mesh需命名以_SmoothNomral结尾");
                return;
            }

            GameObject MeshGo =Selection.gameObjects[0];
            GameObject MeshSmoothNormalGo =Selection.gameObjects[1];

            if (first)
            {
                MeshGo =Selection.gameObjects[1];
                MeshSmoothNormalGo =Selection.gameObjects[0];
            }
            
            Debug.Log("MeshGo:"+MeshGo.name+" || " +"MeshSmoothNormalGo:"+MeshSmoothNormalGo.name);
            
            var MeshGoInstance = GameObject.Instantiate(MeshGo);
            var MeshSmoothNormalGoInstance = GameObject.Instantiate(MeshSmoothNormalGo);
        
            var path = AssetDatabase.GetAssetPath(Selection.gameObjects[0]);
            var dir = Path.GetDirectoryName(path);
            var filename = Path.GetFileNameWithoutExtension(path);
            
            {
                var meshfilters = MeshGoInstance.GetComponentsInChildren<MeshFilter>();
                var meshfilters2 = MeshSmoothNormalGoInstance.GetComponentsInChildren<MeshFilter>();

                if (meshfilters==null || meshfilters2==null|| meshfilters.Length != meshfilters2.Length)
                {
                    Debug.LogError("物体不一致");
                    return;
                }

                for (int i = 0; i < meshfilters.Length; i++)
                {
                    if (meshfilters[i].sharedMesh != null && meshfilters2[i].sharedMesh != null)
                    {
                        var mesh = CombineMeshNormalToTangent(meshfilters[i].sharedMesh, meshfilters2[i].sharedMesh);
                        SaveMesh(mesh, dir, MeshGo.name + "_SmoothMesh");
                    }
                }
                
            }
            
            {
                var meshfilters = MeshGoInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
                var meshfilters2 = MeshSmoothNormalGoInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

                if (meshfilters==null || meshfilters2==null|| meshfilters.Length != meshfilters2.Length)
                {
                    Debug.LogError("物体不一致");
                    return;
                }

                for (int i = 0; i < meshfilters.Length; i++)
                {
                    if (meshfilters[i].sharedMesh != null && meshfilters2[i].sharedMesh != null)
                    {
                        var mesh = CombineMeshNormalToTangent(meshfilters[i].sharedMesh, meshfilters2[i].sharedMesh);
                        SaveMesh(mesh, dir, MeshGo.name + "_SmoothMesh");
                    }
                }

            }
            
            GameObject.DestroyImmediate( MeshGoInstance);
            GameObject.DestroyImmediate( MeshSmoothNormalGoInstance);

        }

    }
}