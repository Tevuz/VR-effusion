using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace _Project.Physics {

    public class CustomCollider : MonoBehaviour {

        private const int MAX_HIT_COUNT = 8;

        [SerializeField] internal SkinnedMeshRenderer _skin;
        [SerializeField] internal ComputeShader _shader;

        public List<Material> _materials = new();

        private int cacheFrame = -1;
        private GraphicsBuffer _vertices;
        private GraphicsBuffer _indices;

        private ComputeBuffer _distance;

        private (int i, int dx) _kernelRaycast;

        private void Start() {
            _shader.SetInt("MAX_HIT_COUNT", MAX_HIT_COUNT);

            _distance = new ComputeBuffer(1, 4, ComputeBufferType.Constant);

            _kernelRaycast.i = _shader.FindKernel("MainRaycast");
            _shader.GetKernelThreadGroupSizes(_kernelRaycast.i, out uint tx, out _, out _);
            _kernelRaycast.dx = (int)tx;
            _indices = _skin.sharedMesh.GetIndexBuffer();
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            _indices = _skin.sharedMesh.GetIndexBuffer();
        }
#endif

        private void OnDestroy() {
            _vertices?.Release();
            _indices?.Release();
        }

        public Hit? Raytrace(Vector3 pos, Vector3 dir) {
            if (cacheFrame != Time.fixedTime) {
                cacheFrame = Time.frameCount;
                _vertices?.Release();
                _vertices = _skin.GetVertexBuffer();
            }

            _shader.SetFloat("_distance", 1e30f);

            _shader.SetFloats("_rayPos", pos.x, pos.y, pos.z);
            _shader.SetFloats("_rayDir", dir.x, dir.y, dir.z);

            _shader.SetBuffer(_kernelRaycast.i, "_vertices", _vertices);
            _shader.SetBuffer(_kernelRaycast.i, "_indices", _indices);
            _shader.SetInt("_stride", _vertices.stride);

            foreach (int id in _materials.Where(m => m.enabled).Select(m => m.materialID)) {
                var mesh = _skin.sharedMesh.GetSubMesh(id);
                int x = (mesh.indexCount / 3 + _kernelRaycast.dx - 1) / _kernelRaycast.dx;
                _shader.SetInt("_offset", mesh.indexStart);
                _shader.SetInt("_length", mesh.indexCount);
                _shader.Dispatch(_kernelRaycast.i, x, 1, 1);
            }

            return null;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CustomCollider))]
    public class CustomColliderEditor : Editor {

        private SerializedProperty _skin;
        private SerializedProperty _shader;

        private bool _materialGroup;

        private void OnEnable() {
            _skin = serializedObject.FindProperty("_skin");
            _shader = serializedObject.FindProperty("_shader");
        }

        public override void OnInspectorGUI() {
            CustomCollider collider = (CustomCollider)target;

            serializedObject.Update();

            EditorGUILayout.PropertyField(_skin);
            EditorGUILayout.PropertyField(_shader);

            _materialGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_materialGroup, "Materials");
            if (_materialGroup) {
                UnityEngine.Material[] materials = collider._skin.sharedMaterials;
                var list = collider._materials;

                while (materials.Length > list.Count) {
                    list.Add(new Material());
                }

                while (materials.Length < list.Count) {
                    list.RemoveAt(list.Count - 1);
                }

                for (int i = 0; i < list.Count; i++) {
                    var label = $"    [{i}]:  {materials[i].name.Split("(Instance)")[0]}";
                    list[i].materialID = i;
                    list[i].enabled = EditorGUILayout.Toggle(label, list[i].enabled);
                }
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    public class Material {
        public int materialID;
        public bool enabled;
    }

    public struct Hit {
        public Vector3 position;
        public Vector3 normal;
        public float distance;
        public int materialID;
    }
}