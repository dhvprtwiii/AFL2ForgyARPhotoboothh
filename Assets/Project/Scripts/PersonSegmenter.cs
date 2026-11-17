using Unity.InferenceEngine;   // jika paket Anda Sentis: using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;

public class PersonSegmenter : MonoBehaviour
{
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private WebcamFeed webcam;
    [SerializeField] private RawImage personLayer;
    [Tooltip("Seret shader Forgy/MaskSmooth ke sini.")]
    [SerializeField] private Shader smoothShader;
    [SerializeField] private int inputWidth = 256;
    [SerializeField] private int inputHeight = 256;
    [Range(0.05f, 1f)]
    [SerializeField] private float temporalBlend = 0.45f;
    [SerializeField] private BackendType backend = BackendType.GPUCompute;

    private Worker worker;
    private Tensor<float> input;
    private RenderTexture rawRT, smoothA, smoothB;
    private Material materialInstance, smoothMat;
    private bool flip;

    private void Start()
    {
        Model model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, backend);
        input = new Tensor<float>(new TensorShape(1, 3, inputHeight, inputWidth));

        rawRT = new RenderTexture(inputWidth, inputHeight, 0, RenderTextureFormat.ARGB32);
        smoothA = new RenderTexture(inputWidth, inputHeight, 0, RenderTextureFormat.ARGB32);
        smoothB = new RenderTexture(inputWidth, inputHeight, 0, RenderTextureFormat.ARGB32);
        rawRT.Create();
        smoothA.Create();
        smoothB.Create();
        Graphics.Blit(Texture2D.blackTexture, smoothA);
        Graphics.Blit(Texture2D.blackTexture, smoothB);

        Shader sh = smoothShader != null ? smoothShader : Shader.Find("Forgy/MaskSmooth");
        smoothMat = new Material(sh);

        materialInstance = new Material(personLayer.material);
        materialInstance.SetTexture("_Mask", smoothA);
        personLayer.material = materialInstance;
    }

    private void Update()
    {
        if (webcam.Texture == null || !webcam.Texture.didUpdateThisFrame) return;

        TextureConverter.ToTensor(webcam.Texture, input, new TextureTransform());
        worker.Schedule(input);

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        // Jika output model berbentuk 1 x H x W x 1, aktifkan baris ini:
        // output.Reshape(new TensorShape(1, 1, inputHeight, inputWidth));
        TextureConverter.RenderToTexture(output, rawRT);

        // Haluskan mask: blur 3x3 + campur dengan frame sebelumnya.
        RenderTexture prev = flip ? smoothB : smoothA;
        RenderTexture next = flip ? smoothA : smoothB;
        smoothMat.SetTexture("_Prev", prev);
        smoothMat.SetFloat("_Blend", temporalBlend);
        Graphics.Blit(rawRT, next, smoothMat);
        materialInstance.SetTexture("_Mask", next);
        flip = !flip;
    }

    private void OnDestroy()
    {
        if (input != null) input.Dispose();
        if (worker != null) worker.Dispose();
        if (rawRT != null) rawRT.Release();
        if (smoothA != null) smoothA.Release();
        if (smoothB != null) smoothB.Release();
        if (materialInstance != null) Destroy(materialInstance);
        if (smoothMat != null) Destroy(smoothMat);
    }
}