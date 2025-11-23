using UnityEngine;
using Unity.InferenceEngine;
using System.Linq;

// Se 'Unity.Inference' não for reconhecido, tente 'Unity.AI.Inference' 
// e verifique o nome do Assembly no Inspector do arquivo .asmdef.

public class AirSnipSegmentation : MonoBehaviour
{
    [Header("Configuração do Modelo")]
    [Tooltip("Arraste o arquivo .onnx do yolov8s-seg aqui")]
    public ModelAsset modelAsset; // Se der erro aqui, verifique se virou 'InferenceModel'

    // Backend GPU é essencial para performance no Quest 3
    BackendType backend = BackendType.GPUCompute;

    private Worker workerMask;
    private Worker workerBox;
    
    // Cache para não alocar memória todo frame
    private RenderTexture resultMaskRenderTexture;

    // Dimensões padrão do YOLOv8
    private const int ImageSize = 640;
    private const int MaskCoefStartIndex = 84;
    private const int NumMaskCoeffs = 32;

    void Start()
    {
        // Prepara a RenderTexture de saída
        resultMaskRenderTexture = new RenderTexture(ImageSize, ImageSize, 0, RenderTextureFormat.RFloat);
        resultMaskRenderTexture.enableRandomWrite = true; 
        resultMaskRenderTexture.Create();

        LoadSegmentationModel();
    }

    void LoadSegmentationModel()
    {
        try
        {
            if (modelAsset == null)
            {
                Debug.LogError("ModelAsset não foi atribuído! Arraste o arquivo yolov8s-seg.onnx no Inspector.");
                return;
            }

            Debug.Log("Carregando modelo YOLO...");
            var model = ModelLoader.Load(modelAsset);

            // --- Construção do Grafo (Pós-Processamento na GPU) ---
            var graph = new FunctionalGraph();
            var inputs = graph.AddInputs(model);

            // Executa o modelo base
            FunctionalTensor[] outputs = Functional.Forward(model, inputs);
            Debug.Log($"Outputs do modelo: {outputs.Length}");
            
            var rawData = outputs[0];    // Boxes + Classes + Coeffs
            var prototypes = outputs[1]; // Protótipos

            // 1. Encontrar o Objeto Principal (Maior Score)
            // Pega scores (indices 4 até 84)
            var classScores = rawData[0, 4..MaskCoefStartIndex, ..]; 
            
            // ReduceMax para achar o melhor score de cada caixa
            var maxScoresPerBox = Functional.ReduceMax(classScores, 0);
            
            // ArgMax para achar o índice da melhor caixa
            var bestBoxIndex = Functional.ArgMax(maxScoresPerBox, 0, keepdim: true);

            // 2. Extrair Coeficientes
            var allMaskCoeffs = rawData[0, MaskCoefStartIndex..(MaskCoefStartIndex + NumMaskCoeffs), ..];
            
            // Seleciona os coeficientes da melhor caixa e transpõe
            var bestCoeffs = Functional.IndexSelect(allMaskCoeffs, 1, bestBoxIndex).Transpose(0, 1);

            // 3. Gerar a Máscara (MatMul)
            // Define o shape explicitamente (Inference Engine exige TensorShape ou int[])
            var protosFlat = prototypes.Reshape(new int[] { NumMaskCoeffs, 160 * 160 });
            
            var maskFlat = Functional.MatMul(bestCoeffs, protosFlat);
            
            var maskRaw = maskFlat.Reshape(new int[] { 1, 1, 160, 160 });
            var maskSigmoid = Functional.Sigmoid(maskRaw);

            // 4. Upsample (Interpolate)
            var finalMask = Functional.Interpolate(maskSigmoid, null, new float[] { 4.0f, 4.0f }, "linear");

            // 5. Extrair Bounding Box da melhor detecção
            // YOLOv8 retorna boxes em [x, y, w, h] nos primeiros 4 índices
            var bestBoxCoords = Functional.IndexSelect(rawData[0, 0..4, ..], 1, bestBoxIndex);

            Debug.Log("Compilando workers...");
            // Compila o modelo com os dois outputs
            var compiledModel = graph.Compile(finalMask, bestBoxCoords);
            workerMask = new Worker(compiledModel, backend);
            workerBox = workerMask; // Mesmo worker, outputs diferentes
            
            Debug.Log("Workers inicializados com sucesso!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao carregar modelo de segmentação: {e.Message}\n{e.StackTrace}");
        }
    }

    public Texture2D GetSegmentationMask(Texture2D inputImage)
    {
        if (workerMask == null || workerBox == null)
        {
            Debug.LogWarning("Workers não inicializados, retornando imagem original");
            return inputImage;
        }

        // 1. Converte Input para Tensor
        using var inputTensor = TextureConverter.ToTensor(inputImage, width: ImageSize, height: ImageSize, channels: 3);

        // 2. Executa o worker (retorna ambos outputs)
        workerMask.Schedule(inputTensor);

        // 3. Recupera a máscara (output 0)
        var maskOutput = workerMask.PeekOutput(0) as Tensor<float>;
        TextureConverter.RenderToTexture(maskOutput, resultMaskRenderTexture);
        
        // 4. Recupera o bounding box (output 1) - tensor rank 2
        using var boxOutput = (workerMask.PeekOutput(1) as Tensor<float>).ReadbackAndClone();
        
        // YOLOv8 retorna box em formato [x_center, y_center, width, height] em pixels (não normalizado)
        float xCenter = boxOutput[0, 0];
        float yCenter = boxOutput[0, 1];
        float boxWidth = boxOutput[0, 2];
        float boxHeight = boxOutput[0, 3];
        
        Debug.Log($"Bounding box (pixels): center=({xCenter:F1}, {yCenter:F1}), size=({boxWidth:F1}x{boxHeight:F1})");
        
        // Converte para coordenadas [x1, y1, x2, y2] em pixels do ImageSize (640x640)
        float x1_640 = xCenter - boxWidth / 2f;
        float y1_640 = yCenter - boxHeight / 2f;
        float x2_640 = xCenter + boxWidth / 2f;
        float y2_640 = yCenter + boxHeight / 2f;
        
        // Escala para as dimensões da imagem original
        float scaleX = (float)inputImage.width / ImageSize;
        float scaleY = (float)inputImage.height / ImageSize;
        
        int x1 = Mathf.Max(0, Mathf.RoundToInt(x1_640 * scaleX));
        int y1_top = Mathf.Max(0, Mathf.RoundToInt(y1_640 * scaleY));
        int x2 = Mathf.Min(inputImage.width, Mathf.RoundToInt(x2_640 * scaleX));
        int y2_top = Mathf.Min(inputImage.height, Mathf.RoundToInt(y2_640 * scaleY));
        
        // Inverter Y porque Unity usa coordenadas de baixo para cima em GetPixels
        int y1 = inputImage.height - y2_top;
        int y2 = inputImage.height - y1_top;
        
        // 5. Aplica a máscara no canal alpha da imagem original (tamanho completo)
        Texture2D fullTexture = new Texture2D(inputImage.width, inputImage.height, TextureFormat.RGBA32, mipChain: false);
        Color[] pixels = inputImage.GetPixels();
        
        // Lê a máscara e aplica no canal alpha
        RenderTexture.active = resultMaskRenderTexture;
        Texture2D maskTexture = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBAFloat, mipChain: false);
        maskTexture.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0);
        maskTexture.Apply();
        RenderTexture.active = null;
        
        Color[] maskPixels = maskTexture.GetPixels();
        
        // Aplica a máscara no canal alpha
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % inputImage.width;
            int y = i / inputImage.width;
            int maskX = Mathf.Clamp((int)((float)x / inputImage.width * ImageSize), 0, ImageSize - 1);
            int maskY = Mathf.Clamp((int)((float)y / inputImage.height * ImageSize), 0, ImageSize - 1);
            int maskIndex = maskY * ImageSize + maskX;
            
            pixels[i].a = maskPixels[maskIndex].r;
        }
        
        fullTexture.SetPixels(pixels);
        fullTexture.Apply();
        Object.Destroy(maskTexture);
        
        // 6. Faz o crop usando o bounding box (igual ao Python: crop = img[y1:y2, x1:x2])
        int cropX = x1;
        int cropY = y1;
        int cropWidth = x2 - x1;
        int cropHeight = y2 - y1;
        
        Debug.Log($"Crop: ({cropX}, {cropY}) size: {cropWidth}x{cropHeight}");
        
        if (cropWidth <= 0 || cropHeight <= 0)
        {
            Debug.LogWarning("Bounding box inválida, retornando imagem completa");
            return fullTexture;
        }
        
        // Cria textura cropada
        Texture2D croppedTexture = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, mipChain: false);
        Color[] croppedPixels = fullTexture.GetPixels(cropX, cropY, cropWidth, cropHeight);
        croppedTexture.SetPixels(croppedPixels);
        croppedTexture.Apply();
        
        Object.Destroy(fullTexture);
        
        return croppedTexture;
    }

    void OnDestroy()
    {
        workerMask?.Dispose();
        workerBox?.Dispose();
        if (resultMaskRenderTexture != null) resultMaskRenderTexture.Release();
    }
}