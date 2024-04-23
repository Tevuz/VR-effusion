using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ultrasound
{
    public class UltrasoundController : MonoBehaviour {

        [SerializeField] internal float _width;
        [SerializeField] internal float _depth;
        [SerializeField, Range(0, 60)] internal float _ARC;

        [SerializeField] internal Matrix4x4 _projection = Matrix4x4.identity;
        [SerializeField] internal Matrix4x4 _view = Matrix4x4.identity;

        internal Mesh _mesh;
        internal GraphicsBuffer _vertices;
        internal GraphicsBuffer _indices;

        private void OnValidate() {
            float w = _width * 0.5f;
            _projection = Matrix4x4.Ortho(-w, w, -w, w, 0.0f, -_depth);


            InitMesh();
            Render();
        }

        // Update is called once per frame
        void Update() {
            _view = transform.worldToLocalMatrix;
        }

        private void InitMesh() {
            _vertices?.Dispose();
            _indices?.Dispose();
            //_mesh = transform.GetChild(0).gameObject.GetComponent<MeshFilter>().mesh;
            MeshFilter filter = GetComponent<MeshFilter>();
            _mesh = filter.mesh = new Mesh();
            _mesh.name = "Frustum";

            _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            var a_position = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

            _mesh.SetVertexBufferParams(4, a_position);
            _mesh.SetIndexBufferParams(8, IndexFormat.UInt32);

            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, 8, MeshTopology.Lines), MeshUpdateFlags.DontRecalculateBounds);

            _vertices = _mesh.GetVertexBuffer(0);
            _indices = _mesh.GetIndexBuffer();

            _indices.SetData(new[]{0, 1, 1, 2, 2, 3, 3, 0});
        }

        private void Render() {
            var inverse = _projection.inverse;
            _vertices.SetData(new[] {
                (inverse.MultiplyPoint(new Vector3(-1, 0, -1))),
                (inverse.MultiplyPoint(new Vector3(-1, 0, +1))),
                (inverse.MultiplyPoint(new Vector3(+1, 0, +1))),
                (inverse.MultiplyPoint(new Vector3(+1, 0, -1))),
            });
        }
    }
}
