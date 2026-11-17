using UnityEngine;
using UnityEngine.UI;

public class WebcamFeed : MonoBehaviour
{
    [SerializeField] private RawImage background; // CameraBackground
    [SerializeField] private RawImage personLayer; // PersonCutout
    [SerializeField] private int width = 1280;
    [SerializeField] private int height = 720;
    [SerializeField] private int fps = 30;
    [SerializeField] private int deviceIndex = 0;
    [SerializeField] private bool mirror = true; // tampil seperti cermin

    public WebCamTexture Texture { get; private set; }

    private void Awake()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("Webcam tidak ditemukan.");
            return;
        }

        deviceIndex = Mathf.Clamp(deviceIndex, 0, devices.Length - 1);

        Texture = new WebCamTexture(
            devices[deviceIndex].name,
            width,
            height,
            fps
        );

        Texture.Play();

        background.texture = Texture;
        personLayer.texture = Texture;

        // Hanya lapisan kamera yang dibalik; Forgy dan teks tidak.
        Rect uv = new Rect(mirror ? 1f : 0f, 0f, mirror ? -1f : 1f, 1f);

        background.uvRect = uv;
        personLayer.uvRect = uv;
    }

    private void OnDestroy()
    {
        if (Texture != null)
            Texture.Stop();
    }
}