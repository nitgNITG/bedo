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
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector3 a = surface.transform.TransformPoint(vertices[triangles[0]]);
            Vector3 b = surface.transform.TransformPoint(vertices[triangles[1]]);
            Vector3 c = surface.transform.TransformPoint(vertices[triangles[2]]);
            Vector3 forward = mesh.normals.Length == vertices.Length
                ? -surface.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(mesh.normals[triangles[0]]).normalized
                : Vector3.Cross(b - a, c - a).normalized;
            var reference = Camera.main;
            if (reference != null && Vector3.Dot(forward, surface.bounds.center - reference.transform.position) < 0f)
                forward = -forward;
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;
            if (up.sqrMagnitude < .1f) up = Vector3.ProjectOnPlane(surface.transform.up, forward).normalized;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            // Imported meshes can retain unused vertices; fit the rendered triangles only.
            foreach (int index in triangles)
            {
                Vector3 point = surface.transform.TransformPoint(vertices[index]) - a;
                Vector2 projected = new Vector2(Vector3.Dot(point, right), Vector3.Dot(point, up));
                min = Vector2.Min(min, projected);
                max = Vector2.Max(max, projected);
            }
            Vector2 size = max - min;
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
