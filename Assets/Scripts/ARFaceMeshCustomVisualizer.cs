using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Generates a scaled and offset mesh for an <see cref="ARFace"/>.
    /// </summary>
    [RequireComponent(typeof(ARFace))]
    public sealed class ARFaceMeshCustomVisualizer : MonoBehaviour
    {
        [Header("Custom Face Mesh Settings")]
        [Tooltip("Uniforme Skalierung des Face Meshes (z. B. 0.88 = 88%)")]
        [SerializeField]
        private float faceScale = 0.88f;

        [Tooltip("Z-Offset in lokaler Face-Richtung")]
        [SerializeField]
        private float zOffset = 0.01f;

        public Mesh mesh { get; private set; }

        void SetVisible(bool visible)
        {
            m_MeshRenderer = GetComponent<MeshRenderer>();
            if (m_MeshRenderer == null) return;

            if (visible && !m_MeshRenderer.enabled)
            {
                SetMeshTopology();
            }

            m_MeshRenderer.enabled = visible;
        }

        void SetMeshTopology()
        {
            if (mesh == null) return;

            using (new ScopedProfiler("SetMeshTopology"))
            {
                using (new ScopedProfiler("ClearMesh"))
                    mesh.Clear();

                if (m_Face.vertices.Length > 0 && m_Face.indices.Length > 0)
                {
                    var scaledVertices = new List<Vector3>(m_Face.vertices.Length);
                    foreach (var vertex in m_Face.vertices)
                    {
                        var v = vertex * faceScale;
                        v.z += zOffset;
                        scaledVertices.Add(v);
                    }

                    using (new ScopedProfiler("SetVertices"))
                        mesh.SetVertices(scaledVertices);

                    using (new ScopedProfiler("SetIndices"))
                        mesh.SetIndices(m_Face.indices, MeshTopology.Triangles, 0, false);

                    using (new ScopedProfiler("RecalculateBounds"))
                        mesh.RecalculateBounds();

                    if (m_Face.normals.Length == m_Face.vertices.Length)
                    {
                        using (new ScopedProfiler("SetNormals"))
                            mesh.SetNormals(m_Face.normals);
                    }
                    else
                    {
                        using (new ScopedProfiler("RecalculateNormals"))
                            mesh.RecalculateNormals();
                    }
                }

                if (m_Face.uvs.Length > 0)
                {
                    using (new ScopedProfiler("SetUVs"))
                        mesh.SetUVs(0, m_Face.uvs);
                }

                var meshFilter = GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = mesh;
                }

                var meshCollider = GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = mesh;
                }

                m_TopologyUpdatedThisFrame = true;
            }
        }

        void UpdateVisibility()
        {
            var visible = enabled &&
                (m_Face.trackingState != TrackingState.None) &&
                (ARSession.state > ARSessionState.Ready);

            SetVisible(visible);
        }

        void OnUpdated(ARFaceUpdatedEventArgs eventArgs)
        {
            UpdateVisibility();
            if (!m_TopologyUpdatedThisFrame)
            {
                SetMeshTopology();
            }
            m_TopologyUpdatedThisFrame = false;
        }

        void OnSessionStateChanged(ARSessionStateChangedEventArgs eventArgs)
        {
            UpdateVisibility();
        }

        void Awake()
        {
            mesh = new Mesh();
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_Face = GetComponent<ARFace>();
        }

        void OnEnable()
        {
            m_Face.updated += OnUpdated;
            ARSession.stateChanged += OnSessionStateChanged;
            UpdateVisibility();
        }

        void OnDisable()
        {
            m_Face.updated -= OnUpdated;
            ARSession.stateChanged -= OnSessionStateChanged;
        }

        ARFace m_Face;
        MeshRenderer m_MeshRenderer;
        bool m_TopologyUpdatedThisFrame;
    }
}
