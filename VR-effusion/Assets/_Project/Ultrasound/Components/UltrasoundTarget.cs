using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ultrasound {
    public class UltrasoundTarget : MonoBehaviour {

        [SerializeField] internal List<TargetMaterial> _materials = new();

        internal Mesh GetMesh {
            get {
                if (TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
                    return skinnedMeshRenderer.sharedMesh;
                if (TryGetComponent(out MeshFilter meshFilter))
                    return meshFilter.sharedMesh;
                throw new MissingComponentException(
                    $"There is no 'MeshFilter' or 'SkinnedMesRenderer' attached to '{name}' game object.");
            }
        }

        internal Material[] GetMaterials {
            get {
                if (TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
                    return skinnedMeshRenderer.sharedMaterials;
                if (TryGetComponent(out MeshRenderer meshRenderer))
                    return meshRenderer.sharedMaterials;
                throw new MissingComponentException(
                    $"There is no 'MeshRenderer' or 'SkinnedMesRenderer' attached to '{name}' game object.");
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UltrasoundTarget))]
    public class UltrasoundTargetEditor : Editor {
        private bool _materialHeader;
        private bool _sort, _details;

        private void OnEnable() {
        }

        public override void OnInspectorGUI() {
            UltrasoundTarget target = (UltrasoundTarget)this.target;

            serializedObject.Update();

            Mesh mesh = target.GetMesh;
            _materialHeader = EditorGUILayout.BeginFoldoutHeaderGroup(_materialHeader, "Materials");
            if (_materialHeader) {
                Material[] materials = target.GetMaterials;

                if (materials.Length != target._materials.Count) {
                    target._materials = materials.Select(m => new TargetMaterial() {
                        materialName = m.name.Split("(Instance)")[0],
                        enabled = true,
                        foldout = false,
                        layer = 0,
                        density = 1.0f,
                        attenuation = 1.0f
                    }).ToList();
                }

                _sort = EditorGUILayout.Toggle("Sort List", _sort);
                EditorGUILayout.LabelField("");

                var list = target._materials.AsEnumerable();
                if (_sort)
                    list = target._materials.OrderBy(m => !m.enabled).ThenByDescending(m => m.materialName);

                EditorGUI.indentLevel++;

                foreach (TargetMaterial material in list) {
                    EditorGUILayout.BeginHorizontal();
                    material.enabled = EditorGUILayout.Toggle($"{material.materialName}", material.enabled);
                    if (material.enabled) {
                        material.foldout = EditorGUILayout.Toggle("Show Properties", material.foldout);
                        _details &= material.foldout;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;
                    if (material.foldout) {
                        material.layer = Math.Clamp(EditorGUILayout.IntField("layer", material.layer), 0, 31);
                        material.density = EditorGUILayout.FloatField("Density", material.density);
                        material.attenuation = Mathf.Clamp(EditorGUILayout.FloatField("Attenuation", material.attenuation), 0.0f, 1.0f);
                    }

                    EditorGUI.indentLevel--;

                }

                EditorGUI.indentLevel--;
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    [Serializable]
    internal class TargetMaterial {
        public string materialName;
        public bool enabled;
        internal bool foldout;
        public int layer;
        public float density;
        public float attenuation;
    }
}

