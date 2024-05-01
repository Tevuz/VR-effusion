using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ultrasound
{
    public class UltrasoundController : MonoBehaviour {

        [SerializeField, Min(0)] internal float _width;
        [SerializeField, Min(0)] internal float _depth;
        [SerializeField, Range(0, 60)] internal float _ARC;

        internal Matrix4x4 _projection = Matrix4x4.identity;
        internal Matrix4x4 _view => transform.worldToLocalMatrix;

        internal Mesh _mesh;
        internal GraphicsBuffer _vertices;
        internal GraphicsBuffer _indices;

        private void Start() {
            _width = 0.09f;
            _depth = 0.15f;
            float w = _width * 0.5f;
            _projection = Matrix4x4.Ortho(-w, w, -w, w, 0.0f, -_depth);

            InitMesh();
            Render();
        }

        private void OnDestroy() {
            _vertices.Release();
            _indices.Release();
        }

#if UNITY_EDITOR
        private void OnValidate() {
            float w = _width * 0.5f;
            _projection = Matrix4x4.Ortho(-w, w, -w, w, 0.0f, -_depth);
            Render();
        }
#endif

        private void InitMesh() {
            _mesh = new Mesh(){ name = "Frustum" };
            GetComponent<MeshFilter>().mesh = _mesh;

            _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            var a_position = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

            _mesh.SetVertexBufferParams(4, a_position);
            _mesh.SetIndexBufferParams(8, IndexFormat.UInt32);

            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, 8, MeshTopology.Lines), MeshUpdateFlags.DontRecalculateBounds);

            _vertices = _mesh.GetVertexBuffer(0);
            _indices = _mesh.GetIndexBuffer();

            _indices.SetData(new[]{0, 1, 1, 2, 2, 3, 3, 0});

            Render();
        }

        private void Render() {
            var inverse = _projection.inverse;
            _vertices?.SetData(new[] {
                (inverse.MultiplyPoint(new Vector3(-1, 0, -1))),
                (inverse.MultiplyPoint(new Vector3(-1, 0, +1))),
                (inverse.MultiplyPoint(new Vector3(+1, 0, +1))),
                (inverse.MultiplyPoint(new Vector3(+1, 0, -1))),
            });
        }
    }
}
