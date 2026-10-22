using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace TutorialFramework
{
    /// <summary>
    /// Logical parent for one Free Mode connection.  Any visual spline or
    /// LineRenderer belongs below this object; clicking this object removes
    /// only this exact connection.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabWire : MonoBehaviour
    {
        [SerializeField]
        private Collider clickCollider;

        private FreeLabManager owner;
        private FreeLabEndpoint endpointA;
        private FreeLabEndpoint endpointB;
        private GameObject plugA;
        private GameObject plugB;
        private bool removalRequested;
        private Mesh cableClickMesh;

        [Header("Alternate-module cable route")]
        [SerializeField] private bool fitToAllocatedPlugs;
        [SerializeField] private FreeLabEndpoint routeEndpointA;
        [SerializeField] private Vector3 outletLocalA, outletLocalB;
        [SerializeField] private Vector3 outletDirectionA = Vector3.back, outletDirectionB = Vector3.back;
        [SerializeField] private float routeSag, routeDepth;

        // Only the newly authored alternate-module cables opt in. Their shape
        // follows the actual first/stacked plug allocated by the shared lab.
        public void AuthorAlternateModuleRoute(FreeLabEndpoint firstEndpoint, GameObject firstPlug,
            GameObject secondPlug, float sag, float depth)
        {
            fitToAllocatedPlugs = true;
            routeEndpointA = firstEndpoint;
            routeSag = sag;
            routeDepth = depth;
            ReadOutlet(firstPlug, out outletLocalA, out outletDirectionA);
            ReadOutlet(secondPlug, out outletLocalB, out outletDirectionB);
            FitRoute(firstPlug.transform, secondPlug.transform);
        }

        private static void ReadOutlet(GameObject plug, out Vector3 position, out Vector3 direction)
        {
            var filter = plug.GetComponentInChildren<MeshFilter>(true);
            var bounds = filter.sharedMesh.bounds;
            // Plugs.fbx's cable mouth is on its negative-Z end. Start just
            // inside the mouth so there is no visible gap at the joint.
            var mouth = new Vector3(bounds.center.x, 0, bounds.min.z + .025f);
            position = plug.transform.InverseTransformPoint(filter.transform.TransformPoint(mouth));
            direction = plug.transform.InverseTransformDirection(filter.transform.TransformDirection(Vector3.back)).normalized;
        }

        private void FitRoute(Transform firstPlug, Transform secondPlug)
        {
            var container = GetComponentInChildren<SplineContainer>(true);
            if (!container || !firstPlug || !secondPlug) return;
            Vector3 a = firstPlug.TransformPoint(outletLocalA), b = secondPlug.TransformPoint(outletLocalB);
            Vector3 da = firstPlug.TransformDirection(outletDirectionA).normalized;
            Vector3 db = secondPlug.TransformDirection(outletDirectionB).normalized;
            Vector3 front = (da + db).normalized;
            if (front.sqrMagnitude < .01f) front = da;
            Vector3 chord = b - a;
            Vector3 middle = (a + b) * .5f + Vector3.down * routeSag + front * routeDepth;
            const float exitLength = .45f;
            Vector3[] points = { a, a + da * exitLength, middle, b + db * exitLength, b };
            Vector3[] incoming = { Vector3.zero, -da * (exitLength / 3), -chord * .18f, db * .65f, db * (exitLength / 3) };
            Vector3[] outgoing = { da * (exitLength / 3), da * .65f, chord * .18f, -db * (exitLength / 3), Vector3.zero };
            var spline = new Spline();
            for (int i = 0; i < points.Length; i++)
                spline.Add(new BezierKnot((float3)container.transform.InverseTransformPoint(points[i]),
                    (float3)container.transform.InverseTransformVector(incoming[i]),
                    (float3)container.transform.InverseTransformVector(outgoing[i]), quaternion.identity), TangentMode.Broken);
            container.Splines = new[] { spline };
            foreach (var extrusion in GetComponentsInChildren<SplineExtrude>(true)) extrusion.Rebuild();
        }

        public FreeLabEndpoint EndpointA => endpointA;
        public FreeLabEndpoint EndpointB => endpointB;

        private void Awake()
        {
            if (clickCollider == null)
                clickCollider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            if (BuildSplineClickMesh()) return;
            StartCoroutine(RefreshSplineColliders());
        }

        private IEnumerator RefreshSplineColliders()
        {
            // SplineExtrude can rebuild the same mesh instance on activation.
            // Re-cook after that rebuild so physics follows the visible cable.
            yield return null;
            foreach (var collider in GetComponentsInChildren<MeshCollider>(true))
            {
                var filter = collider.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh) continue;
                collider.sharedMesh = null;
                collider.sharedMesh = filter.sharedMesh;
            }
        }

        private bool BuildSplineClickMesh()
        {
            // A simple tube avoids fragile high-detail render-mesh collision and
            // gives thin cables a small click margin without changing their visuals.
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int sides = 6;
            foreach (var container in GetComponentsInChildren<SplineContainer>())
            {
                var extrude = container.GetComponent<SplineExtrude>();
                if (!extrude) continue;
                var scale = container.transform.lossyScale;
                float radius = Mathf.Max(.025f, extrude.Radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) * 1.35f);
                foreach (var spline in container.Splines)
                {
                    if (spline.Count < 2) continue;
                    int segments = Mathf.Clamp(Mathf.CeilToInt(SplineUtility.CalculateLength(spline, container.transform.localToWorldMatrix) / .5f), 16, 512);
                    int first = vertices.Count;
                    for (int i = 0; i <= segments; i++)
                    {
                        float t = (float)i / segments;
                        Vector3 center = container.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(spline, t));
                        Vector3 tangent = container.transform.TransformVector((Vector3)SplineUtility.EvaluateTangent(spline, t)).normalized;
                        if (tangent.sqrMagnitude < .01f) tangent = Vector3.forward;
                        Vector3 side = Vector3.Cross(tangent, Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .95f ? Vector3.right : Vector3.up).normalized;
                        Vector3 up = Vector3.Cross(tangent, side).normalized;
                        for (int j = 0; j < sides; j++)
                        {
                            float angle = j * Mathf.PI * 2 / sides;
                            vertices.Add(transform.InverseTransformPoint(center + radius * (side * Mathf.Cos(angle) + up * Mathf.Sin(angle))));
                        }
                        if (i == 0) continue;
                        for (int j = 0; j < sides; j++)
                        {
                            int a = first + (i - 1) * sides + j, b = first + (i - 1) * sides + (j + 1) % sides;
                            int c = first + i * sides + j, d = first + i * sides + (j + 1) % sides;
                            triangles.Add(a); triangles.Add(c); triangles.Add(b);
                            triangles.Add(b); triangles.Add(c); triangles.Add(d);
                            triangles.Add(b); triangles.Add(c); triangles.Add(a);
                            triangles.Add(d); triangles.Add(c); triangles.Add(b);
                        }
                    }
                }
            }
            if (vertices.Count == 0) return false;
            var mesh = new Mesh { name = name + " click surface", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            var surface = GetComponent<MeshCollider>();
            if (!surface) surface = gameObject.AddComponent<MeshCollider>();
            surface.sharedMesh = mesh;
            surface.enabled = true; surface.isTrigger = false;
            clickCollider = surface;
            foreach (var collider in GetComponentsInChildren<MeshCollider>()) if (collider != surface) collider.enabled = false;
            if (cableClickMesh) Destroy(cableClickMesh);
            cableClickMesh = mesh;
            return true;
        }

        private void OnDestroy()
        {
            if (cableClickMesh) Destroy(cableClickMesh);
        }

        public void Configure(
            FreeLabManager manager,
            FreeLabEndpoint first,
            FreeLabEndpoint second,
            GameObject firstPlug,
            GameObject secondPlug)
        {
            removalRequested = false;
            owner = manager;
            endpointA = first;
            endpointB = second;
            plugA = firstPlug;
            plugB = secondPlug;
            if (fitToAllocatedPlugs)
                FitRoute(first == routeEndpointA ? firstPlug.transform : secondPlug.transform,
                    first == routeEndpointA ? secondPlug.transform : firstPlug.transform);
        }

        private void OnMouseDown()
        {
            RemoveFromFreeLab();
        }

        private void OnMouseUpAsButton()
        {
            RemoveFromFreeLab();
        }

        public bool RemoveFromFreeLab()
        {
            if (InteractionManager.Instance == null ||
                !InteractionManager.Instance.Free)
            {
                return false;
            }

            if (removalRequested)
                return false;

            bool removed = owner != null && owner.RemoveConnection(this);

            if (removed)
                removalRequested = true;

            return removed;
        }

        private void OnDisable()
        {
            endpointA?.ReleaseConnectionPlug(plugA);
            endpointB?.ReleaseConnectionPlug(plugB);
        }
    }
}
