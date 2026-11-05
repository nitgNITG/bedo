#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if !UNITY_WEBGL || UNITY_EDITOR
using SFB;
#endif

namespace Bedo.Core.UI
{
    public class MapCameraScreenshotButton_RT : MonoBehaviour
    {
        [Header("Capture")]
        [SerializeField] private Camera captureCamera;
        [Tooltip("Optional monitor surface: captures only this screen, without the room around it.")]
        [SerializeField] private Renderer captureSurface;
        private bool capturing;
        [SerializeField] private int outputWidth = 2048;
        [SerializeField] private int outputHeight = 2048;
        [SerializeField] private string defaultFileName = "Map_Screenshot";
        [SerializeField] private bool transparentBackground = false;

        private Button _button;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void Download(string base64, string fileName);
#endif

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick() => SaveScreenshot();

        public void SaveScreenshot()
        {
            if (capturing) return;
            if (captureCamera == null && captureSurface == null)
            {
                Debug.LogError("MapCameraScreenshotButton_RT: captureCamera is not assigned.");
                return;
            }

            StartCoroutine(CaptureAndSave());
        }

        private IEnumerator CaptureAndSave()
        {
            capturing = true;
            byte[] png;
            bool wasEnabled = captureCamera != null && captureCamera.enabled;
            try
            {
                if (captureSurface != null)
                {
                    yield return new WaitForEndOfFrame();
                    png = CaptureSurfaceToPngBytes(captureSurface, outputWidth);
                }
                else
                {
                    captureCamera.enabled = true;
                    yield return new WaitForEndOfFrame();
                    png = CaptureToPngBytes();
                }
            }
            finally
            {
                if (captureSurface == null && captureCamera != null) captureCamera.enabled = wasEnabled;
                capturing = false;
            }

            if (png == null || png.Length == 0)
                yield break;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: trigger browser download (no save dialog)
            Download(System.Convert.ToBase64String(png), defaultFileName + ".png");
#else
            // Standalone: show save dialog and write file
            string path = StandaloneFileBrowser.SaveFilePanel("Save Map Screenshot", "", defaultFileName, "png");
            if (string.IsNullOrEmpty(path))
                yield break;

            File.WriteAllBytes(path, png);
            Debug.Log($"Map screenshot saved to: {path}");
#endif
        }

        public static byte[] CaptureSurfaceToPngBytes(Renderer surface, int width)
        {
            var filter = surface != null ? surface.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null)
                throw new System.InvalidOperationException("The monitor surface needs a mesh.");
            Mesh mesh = filter.sharedMesh;
            // Submesh bounds remain available after Unity discards CPU vertex/index
            // buffers in a player build. Never read triangles, vertices or normals here.
            if (mesh.subMeshCount == 0)
                throw new System.InvalidOperationException("The monitor surface has no rendered submesh.");
            Bounds bounds = mesh.GetSubMesh(0).bounds;
            for (int submesh = 1; submesh < mesh.subMeshCount; submesh++)
                bounds.Encapsulate(mesh.GetSubMesh(submesh).bounds);
            Vector3 extent = bounds.size;
            Vector3 normal = extent.x <= extent.y && extent.x <= extent.z ? Vector3.right :
                extent.y <= extent.z ? Vector3.up : Vector3.forward;
            Vector3 a = surface.transform.TransformPoint(bounds.center);
            Vector3 forward = surface.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(normal).normalized;
            var reference = Camera.main;
            if (reference != null && Vector3.Dot(forward, surface.bounds.center - reference.transform.position) < 0f)
                forward = -forward;
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;
            if (up.sqrMagnitude < .1f) up = Vector3.ProjectOnPlane(surface.transform.up, forward).normalized;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            // Fit the rendered submeshes, excluding unused imported vertices.
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                Vector3 point = surface.transform.TransformPoint(local) - a;
                Vector2 projected = new Vector2(Vector3.Dot(point, right), Vector3.Dot(point, up));
                min = Vector2.Min(min, projected);
                max = Vector2.Max(max, projected);
            }
            Vector2 size = max - min;
            // Authored corners give imported monitors an exact crop even when
            // their imported submesh bounds retain unused vertices.
            var frame = surface.transform.Find("Screenshot Frame");
            if (frame != null)
            {
                var bottomLeft = frame.Find("Bottom Left");
                var bottomRight = frame.Find("Bottom Right");
                var topLeft = frame.Find("Top Left");
                if (bottomLeft == null || bottomRight == null || topLeft == null)
                    throw new System.InvalidOperationException("The monitor screenshot frame is incomplete.");
                a = bottomLeft.position;
                right = (bottomRight.position - a).normalized;
                up = (topLeft.position - a).normalized;
                forward = Vector3.Cross(right, up).normalized;
                size = new Vector2(Vector3.Distance(a, bottomRight.position), Vector3.Distance(a, topLeft.position));
                min = Vector2.zero;
                max = size;
            }
            if (size.x <= .001f || size.y <= .001f) throw new System.InvalidOperationException("The monitor surface has no capture area.");
            width = Mathf.Max(16, width);
            int height = Mathf.Max(16, Mathf.RoundToInt(width * size.y / size.x));
            var owner = new GameObject("Temporary Monitor Capture") { hideFlags = HideFlags.HideAndDontSave };
            var camera = owner.AddComponent<Camera>();
            if (reference != null) camera.CopyFrom(reference);
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = size.y * .5f;
            camera.aspect = size.x / size.y;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 100f;
            camera.transform.SetPositionAndRotation(a + right * ((min.x + max.x) * .5f) + up * ((min.y + max.y) * .5f) - forward * 20f, Quaternion.LookRotation(forward, up));
            var target = new RenderTexture(width, height, 24);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = target;
                camera.ResetProjectionMatrix();
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                return image.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                if (image != null) Destroy(image);
                target.Release();
                Destroy(target);
                Destroy(owner);
            }
        }

        private byte[] CaptureToPngBytes()
        {
            var rt = new RenderTexture(outputWidth, outputHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture prevTarget = captureCamera.targetTexture;

            CameraClearFlags prevFlags = captureCamera.clearFlags;
            Color prevColor = captureCamera.backgroundColor;
            if (transparentBackground)
            {
                captureCamera.clearFlags = CameraClearFlags.SolidColor;
                captureCamera.backgroundColor = new Color(0, 0, 0, 0);
            }

            captureCamera.targetTexture = rt;
            captureCamera.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, outputWidth, outputHeight), 0, 0);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();

            // restore
            captureCamera.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            if (transparentBackground)
            {
                captureCamera.clearFlags = prevFlags;
                captureCamera.backgroundColor = prevColor;
            }

            Destroy(tex);
            Destroy(rt);

            return png;
        }
    }
}
