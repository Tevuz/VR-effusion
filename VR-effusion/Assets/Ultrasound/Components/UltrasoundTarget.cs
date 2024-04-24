using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ultrasound.Domain;
using UnityEngine;

namespace Ultrasound {
    public class UltrasoundTarget : MonoBehaviour {

        internal struct MeshTarget {
            internal GraphicsBuffer vertices;
            internal GraphicsBuffer indices;
            internal IEnumerable<SubTarget> subMeshes;
        }

        internal struct SubTarget {
            internal UltrasoundMaterial material;
            internal int start;
            internal int count;
        }

        internal MeshTarget? _target;

        private void Start() {
            try {
                var skin = gameObject.GetComponent<SkinnedMeshRenderer>();

                _target = new MeshTarget() {
                    vertices = skin.GetVertexBuffer(),
                    indices = skin.sharedMesh.GetIndexBuffer(),
                    subMeshes = Enumerable.Range(0, skin.sharedMesh.subMeshCount)
                        .Select(i => skin.sharedMesh.GetSubMesh(i)).Select(s => new SubTarget()
                            { start = s.indexStart, count = s.indexCount })
                };
            } catch (Exception _) {

            }

            try {
                var filter = gameObject.GetComponent<MeshFilter>();

                _target = new MeshTarget() {
                    vertices = filter.mesh.GetVertexBuffer(0),
                    indices = filter.mesh.GetIndexBuffer(),
                    subMeshes = new[] { new SubTarget { start = 0, count = (int)filter.mesh.GetIndexCount(0) } }
                };
            } catch (Exception _) {

            }
        }
    }
}

