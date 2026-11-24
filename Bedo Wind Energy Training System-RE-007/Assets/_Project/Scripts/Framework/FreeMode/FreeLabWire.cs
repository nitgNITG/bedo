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
        private Spline authoredSlotRoute;

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
            // Keep the cable flush to the plug mouth. A long forward exit
            // followed by a broad turn appears as a sideways hook in oblique views.
            const float exitLength = .12f;
            Vector3[] points = { a, a + da * exitLength, middle, b + db * exitLength, b };
            Vector3[] incoming = { Vector3.zero, -da * (exitLength / 3), -chord * .18f, db * .02f, db * (exitLength / 3) };
            Vector3[] outgoing = { da * (exitLength / 3), da * .02f, chord * .18f, -db * (exitLength / 3), Vector3.zero };
            var spline = new Spline();
            for (int i = 0; i < points.Length; i++)
                spline.Add(new BezierKnot((float3)container.transform.InverseTransformPoint(points[i]),
                    (float3)container.transform.InverseTransformVector(incoming[i]),
                    (float3)container.transform.InverseTransformVector(outgoing[i]), quaternion.identity), TangentMode.Broken);
            container.Splines = new[] { spline };
            foreach (var extrusion in GetComponentsInChildren<SplineExtrude>(true)) extrusion.Rebuild();
        }

        private void AlignAllocatedPlugEntries(GameObject first, GameObject second)
        {
            if (endpointA.AdditionalConnectionPlugCount == 0 && endpointB.AdditionalConnectionPlugCount == 0) return;
            var container = GetComponentInChildren<SplineContainer>(true);
            if (!container || container.Spline.Count < 4 || !first || !second) return;
            if (authoredSlotRoute == null) authoredSlotRoute = new Spline(container.Spline);
            var route = new Spline(authoredSlotRoute);
            ReadOutlet(first, out var firstMouth, out var unusedFirst);
            ReadOutlet(second, out var secondMouth, out var unusedSecond);
            Vector3 a = first.transform.TransformPoint(firstMouth), b = second.transform.TransformPoint(secondMouth);
            Vector3 start = container.transform.TransformPoint((Vector3)route[0].Position);
            Vector3 end = container.transform.TransformPoint((Vector3)route[route.Count - 1].Position);
            if ((start - a).sqrMagnitude + (end - b).sqrMagnitude > (start - b).sqrMagnitude + (end - a).sqrMagnitude)
            { var swap = a; a = b; b = swap; }
            Vector3 da = container.transform.InverseTransformVector(a - start);
            Vector3 db = container.transform.InverseTransformVector(b - end);
            for (int i = 0; i < route.Count; i++)
            {
                if (i > 1 && i < route.Count - 2) continue;
                var knot = route[i]; knot.Position += (float3)(i < 2 ? da : db);
                route.SetTangentMode(i, TangentMode.Broken);
                route.SetKnot(i, knot);
            }
            var splines = new List<Spline>(container.Splines); splines[0] = route;
            container.Splines = splines;
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
                    using var worldSpline = new NativeSpline(spline, container.transform.localToWorldMatrix, Unity.Collections.Allocator.Temp);
                    float length = worldSpline.GetLength();
                    if (length <= Mathf.Epsilon) continue;

                    // Leave a dead zone at each plug while retaining a clickable
                    // middle on even the shortest cables.
                    const float plugClearance = 1f;
                    float trim = Mathf.Min(plugClearance, length * .25f) / length;
                    float clickableLength = length * (1f - 2f * trim);
                    int segments = Mathf.Clamp(Mathf.CeilToInt(clickableLength / .5f), 16, 512);
                    int first = vertices.Count;
                    for (int i = 0; i <= segments; i++)
                    {
                        float t = Mathf.Lerp(trim, 1f - trim, (float)i / segments);
                        Vector3 center = (Vector3)SplineUtility.EvaluatePosition(worldSpline, t);
                        Vector3 tangent = ((Vector3)SplineUtility.EvaluateTangent(worldSpline, t)).normalized;
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
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                if (collider != surface) collider.enabled = false;
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
            // Start each connection from its authored route, never an old detour.
            var routeContainer = GetComponentInChildren<SplineContainer>(true);
            if (routeContainer != null)
            {
                if (authoredSlotRoute == null) authoredSlotRoute = new Spline(routeContainer.Spline);
                else routeContainer.Spline = new Spline(authoredSlotRoute);
            }
            removalRequested = false;
            owner = manager;
            endpointA = first;
            endpointB = second;
            plugA = firstPlug;
            plugB = secondPlug;
            if (fitToAllocatedPlugs)
                FitRoute(first == routeEndpointA ? firstPlug.transform : secondPlug.transform,
                    first == routeEndpointA ? secondPlug.transform : firstPlug.transform);
            else
                AlignAllocatedPlugEntries(firstPlug, secondPlug);
            SeparateConnectedCables(manager, routeContainer);
        }

        // Free Mode can combine lamps/rheostats in any order. Only a cable
        // actually touching an already connected cable needs a local detour.
        // Guided routes, plug mouths and their short straight entries stay fixed.
        private struct CablePoint
        {
            public Vector3 position;
            public float parameter;
            public CablePoint(Vector3 p, float t) { position = p; parameter = t; }
        }

        private sealed class CablePath
        {
            public List<Vector3[]> curves;
            public List<CablePoint> points;
            public float radius;
            public Bounds bounds;
            public readonly Dictionary<Vector3Int,List<int>> cells = new Dictionary<Vector3Int,List<int>>();
        }

        private static Vector3Int CableCell(Vector3 point) => new Vector3Int(Mathf.FloorToInt(point.x),Mathf.FloorToInt(point.y),Mathf.FloorToInt(point.z));

        private static Vector3 Bezier(Vector3[] c, float t)
        {
            float u = 1 - t;
            return u*u*u*c[0] + 3*u*u*t*c[1] + 3*u*t*t*c[2] + t*t*t*c[3];
        }

        private static List<CablePoint> SampleCable(List<Vector3[]> curves)
        {
            var points = new List<CablePoint>();
            for (int i = 0; i < curves.Count; i++)
            {
                var c = curves[i];
                float length = Vector3.Distance(c[0],c[1])+Vector3.Distance(c[1],c[2])+Vector3.Distance(c[2],c[3]);
                int count = Mathf.Max(12, Mathf.CeilToInt(length / .12f));
                for (int j = i == 0 ? 0 : 1; j <= count; j++)
                { float t = (float)j/count; points.Add(new CablePoint(Bezier(c,t),i+t)); }
            }
            return points;
        }

        private static CablePath ReadCable(SplineContainer container)
        {
            var spline = container.Spline;
            var curves = new List<Vector3[]>();
            for (int i=0;i<spline.Count-1;i++)
            {
                var a=spline[i];var b=spline[i+1];
                Vector3 p=container.transform.TransformPoint((Vector3)a.Position), q=container.transform.TransformPoint((Vector3)b.Position);
                curves.Add(new[] {p, p+container.transform.TransformVector((Vector3)math.rotate(a.Rotation,a.TangentOut)),
                    q+container.transform.TransformVector((Vector3)math.rotate(b.Rotation,b.TangentIn)),q});
            }
            var extrusion=container.GetComponent<SplineExtrude>();var scale=container.transform.lossyScale;
            float radius=extrusion?extrusion.Radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)):.04f;
            var path=new CablePath {curves=curves, points=SampleCable(curves),radius=radius};
            path.bounds=new Bounds(path.points[0].position,Vector3.zero);
            foreach(var point in path.points)path.bounds.Encapsulate(point.position);
            path.bounds.Expand(radius*2);
            // A small spatial grid keeps clearance work local to the
            // crossing, rather than testing every segment of every long lead.
            for(int i=0;i<path.points.Count-1;i++)
            {
                var lo=CableCell(Vector3.Min(path.points[i].position,path.points[i+1].position));
                var hi=CableCell(Vector3.Max(path.points[i].position,path.points[i+1].position));
                for(int x=lo.x;x<=hi.x;x++)for(int y=lo.y;y<=hi.y;y++)for(int z=lo.z;z<=hi.z;z++)
                {
                    var key=new Vector3Int(x,y,z);
                    if(!path.cells.TryGetValue(key,out var indices)){indices=new List<int>();path.cells[key]=indices;}
                    indices.Add(i);
                }
            }
            return path;
        }

        // Closest points on two finite segments, including parallel/short ones.
        private static float SegmentDistance(Vector3 p,Vector3 q,Vector3 a,Vector3 b,out float t)
        {
            Vector3 d=q-p,e=b-a,r=p-a;float dd=Vector3.Dot(d,d),ee=Vector3.Dot(e,e),f=Vector3.Dot(e,r);float u;
            if(dd<1e-10f) {t=0;u=ee>1e-10f?Mathf.Clamp01(f/ee):0;}
            else {float c=Vector3.Dot(d,r);if(ee<1e-10f){u=0;t=Mathf.Clamp01(-c/dd);}
                else {float de=Vector3.Dot(d,e),den=dd*ee-de*de;t=den>1e-10f?Mathf.Clamp01((de*f-c*ee)/den):0;
                    u=(de*t+f)/ee;if(u<0){u=0;t=Mathf.Clamp01(-c/dd);}else if(u>1){u=1;t=Mathf.Clamp01((de-c)/dd);}}}
            return Vector3.Distance(p+d*t,a+e*u);
        }

        private static float CableContact(List<CablePoint> points,CablePath other,float radius,out float parameter,out float penetration)
        {
            float best=radius+other.radius+.04f;parameter=-1;penetration=0;
            for(int i=0;i<points.Count-1;i++)
            {
                var a=points[i];var b=points[i+1];float segmentBest=radius+other.radius+.04f;var box=new Bounds((a.position+b.position)*.5f,Vector3.zero);
                box.Encapsulate(a.position);box.Encapsulate(b.position);box.Expand((radius+.04f)*2);
                if(!box.Intersects(other.bounds))continue;
                var search=box;search.Expand(other.radius*2);var lo=CableCell(search.min);var hi=CableCell(search.max);
                for(int x=lo.x;x<=hi.x;x++)for(int y=lo.y;y<=hi.y;y++)for(int z=lo.z;z<=hi.z;z++)
                {
                    if(!other.cells.TryGetValue(new Vector3Int(x,y,z),out var indices))continue;
                    foreach(int j in indices)
                    {
                    var c=other.points[j].position;var d=other.points[j+1].position;
                    if(Mathf.Min(c.x,d.x)>box.max.x+other.radius||Mathf.Max(c.x,d.x)<box.min.x-other.radius||
                       Mathf.Min(c.y,d.y)>box.max.y+other.radius||Mathf.Max(c.y,d.y)<box.min.y-other.radius||
                       Mathf.Min(c.z,d.z)>box.max.z+other.radius||Mathf.Max(c.z,d.z)<box.min.z-other.radius)continue;
                    float distance=SegmentDistance(a.position,b.position,c,d,out float t);
                    if(distance<best){best=distance;parameter=Mathf.Lerp(a.parameter,b.parameter,t);}
                    segmentBest=Mathf.Min(segmentBest,distance);
                    }
                }
                penetration+=Mathf.Max(0,radius+other.radius+.035f-segmentBest)*Vector3.Distance(a.position,b.position);
            }
            return best;
        }

        private static float ContactCost(List<CablePoint> points,List<CablePath> others,float radius,out float parameter,bool physicalOnly=false)
        {
            float cost=0,worst=0;parameter=-1;
            foreach(var other in others)
            {
                float distance=CableContact(points,other,radius,out float at,out float penetration);
                float deficit=radius+other.radius+(physicalOnly?0:.035f)-distance;
                if(at>=0&&deficit>0){cost+=physicalOnly?1+deficit:penetration;if(deficit>worst){worst=deficit;parameter=at;}}
            }
            return cost;
        }

        private static void SplitCurve(Vector3[] c,float t,out Vector3[] left,out Vector3[] right)
        {
            var a=Vector3.Lerp(c[0],c[1],t);var b=Vector3.Lerp(c[1],c[2],t);var d=Vector3.Lerp(c[2],c[3],t);
            var e=Vector3.Lerp(a,b,t);var f=Vector3.Lerp(b,d,t);var g=Vector3.Lerp(e,f,t);
            left=new[]{c[0],a,e,g};right=new[]{g,f,d,c[3]};
        }

        private static float Interpolate(List<CablePoint> points,float[] lengths,float value,bool toLength)
        {
            for(int i=1;i<points.Count;i++)
            {float a=toLength?points[i-1].parameter:lengths[i-1],b=toLength?points[i].parameter:lengths[i];
             if(value<=b)return Mathf.Lerp(toLength?lengths[i-1]:points[i-1].parameter,toLength?lengths[i]:points[i].parameter,Mathf.InverseLerp(a,b,value));}
            return toLength?lengths[lengths.Length-1]:points[points.Count-1].parameter;
        }

        private static void BumpWeight(float s,float a,float center,float b,out float weight,out float derivative)
        {
            weight=derivative=0;if(s<=a||s>=b)return;
            float width=s<center?center-a:b-center,t=(s<center?s-a:s-center)/width;
            weight=.5f+(s<center?-.5f:.5f)*Mathf.Cos(Mathf.PI*t);
            derivative=(s<center?1:-1)*.5f*Mathf.PI*Mathf.Sin(Mathf.PI*t)/width;
        }

        private static List<Vector3[]> Detour(List<Vector3[]> curves,float parameter,Vector3 delta,float span)
        {
            var points=SampleCable(curves);var lengths=new float[points.Count];
            for(int i=1;i<points.Count;i++)lengths[i]=lengths[i-1]+Vector3.Distance(points[i-1].position,points[i].position);
            float center=Interpolate(points,lengths,parameter,true);
            float a=Mathf.Max(.12f,center-span);
            float b=Mathf.Min(lengths[lengths.Length-1]-.12f,center+span);
            if(b-a<.3f)return null;center=Mathf.Clamp(center,a+.05f,b-.05f);
            float[] cuts={Interpolate(points,lengths,a,false),Interpolate(points,lengths,center,false),Interpolate(points,lengths,b,false)};
            var result=new List<Vector3[]>();
            for(int k=0;k<curves.Count;k++)
            {
                var boundaries=new List<float>{0};foreach(float cut in cuts)if(cut>k+.000001f&&cut<k+1-.000001f)boundaries.Add(cut-k);boundaries.Add(1);
                for(int j=0;j<boundaries.Count-1;j++)
                {
                    float l=boundaries[j],r=boundaries[j+1];SplitCurve(curves[k],r,out var part,out var unused);
                    if(l>0){SplitCurve(part,l/r,out unused,out var last);part=last;}
                    BumpWeight(Interpolate(points,lengths,k+l,true),a,center,b,out float wa,out float da);
                    BumpWeight(Interpolate(points,lengths,k+r,true),a,center,b,out float wb,out float db);
                    float ha=Vector3.Distance(part[0],part[1]),hb=Vector3.Distance(part[2],part[3]);
                    part[0]+=delta*wa;part[1]+=delta*(wa+da*ha);part[2]+=delta*(wb-db*hb);part[3]+=delta*wb;result.Add(part);
                }
            }
            return result;
        }

        private void SeparateConnectedCables(FreeLabManager manager,SplineContainer container, FreeLabWire pending = null, bool moveObstruction = true)
        {
            if(!manager||!container||container.Spline.Count<4)return;
            var own=ReadCable(container);var others=new List<CablePath>();
            foreach(var wire in manager.Connections)
                if(wire != this)
                    foreach(var other in wire.GetComponentsInChildren<SplineContainer>(true))
                        if(other&&other.Spline.Count>1)others.Add(ReadCable(other));
            if(pending != null)
                foreach(var other in pending.GetComponentsInChildren<SplineContainer>(true))
                    if(other&&other.Spline.Count>1)others.Add(ReadCable(other));
            if(others.Count==0||ContactCost(own.points,others,own.radius,out var hit,true)==0)return;
            var curves=own.curves;var points=own.points;
            Vector3[] directions={Vector3.back,Vector3.forward,Vector3.left,Vector3.right,Vector3.up,Vector3.down};
            for(int iteration=0;iteration<24;iteration++)
            {
                float cost=ContactCost(points,others,own.radius,out hit);if(cost==0)break;
                List<Vector3[]> best=null;List<CablePoint> bestPoints=null;float bestCost=cost;
                foreach(float span in new[]{1.5f,2.5f,4f})
                {
                    foreach(float distance in new[]{.20f,.35f,.55f,.8f,1.2f})
                    {
                        foreach(var direction in directions)
                        {
                            var candidate=Detour(curves,hit,direction*distance,span);if(candidate==null)continue;
                            var sampled=SampleCable(candidate);float candidateCost=ContactCost(sampled,others,own.radius,out var unused);
                            if(candidateCost<bestCost-.0001f){best=candidate;bestPoints=sampled;bestCost=candidateCost;}
                            if(bestCost==0)break;
                        }
                        if(bestCost==0)break;
                    }
                    if(bestCost==0)break;
                }
                if(best==null)break;curves=best;points=bestPoints;
            }
            var spline=new Spline();
            for(int i=0;i<=curves.Count;i++)
            {
                Vector3 p=i<curves.Count?curves[i][0]:curves[i-1][3];
                Vector3 incoming=i>0?curves[i-1][2]-p:Vector3.zero,outgoing=i<curves.Count?curves[i][1]-p:Vector3.zero;
                spline.Add(new BezierKnot((float3)container.transform.InverseTransformPoint(p),(float3)container.transform.InverseTransformVector(incoming),
                    (float3)container.transform.InverseTransformVector(outgoing),quaternion.identity),TangentMode.Broken);
            }
            container.Spline=spline;
            foreach(var extrusion in GetComponentsInChildren<SplineExtrude>(true))extrusion.Rebuild();
            if(gameObject.activeInHierarchy) BuildSplineClickMesh();
            // An older cable may cross the new plug's fixed entry itself. That
            // entry cannot move: ease the obstructing cable's body around it.
            if(moveObstruction)
                foreach(var wire in manager.Connections)
                {
                    if(wire == this) continue;
                    var other=wire.GetComponentInChildren<SplineContainer>(true);
                    if(!other) continue;
                    var obstructing=ReadCable(other);
                    if(CableContact(points,obstructing,own.radius,out var at,out var penetration)<own.radius+obstructing.radius)
                        wire.SeparateConnectedCables(manager,other,this,false);
                }
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

