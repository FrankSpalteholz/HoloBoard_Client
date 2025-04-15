using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARCameraToMediaPipe : MonoBehaviour
{
    // Delegate für das ImageProcessed-Event
    public delegate void ImageProcessedDelegate(byte[] imageData, int width, int height);
    
    // Event, das ausgelöst wird, wenn ein neues Bild verarbeitet wurde
    public event ImageProcessedDelegate OnImageProcessed;
    
    [SerializeField]
    ARCameraManager cameraManager;
    
    [SerializeField]
    [Tooltip("Ein GameObject mit einer Renderer-Komponente, auf dem das Kamerabild zum Debuggen angezeigt wird")]
    GameObject debugPlane;
    
    [SerializeField]
    [Tooltip("Aktiviert oder deaktiviert die Debug-Anzeige")]
    bool enableDebugView = true;
    
    [SerializeField]
    [Tooltip("Ausgabebreite für das MediaPipe-Format")]
    int outputWidth = 1280;
    
    [SerializeField]
    [Tooltip("Ausgabehöhe für das MediaPipe-Format")]
    int outputHeight = 720;
    
    [SerializeField]
    [Tooltip("Vertikal spiegeln")]
    bool mirrorVertically = true;
    
    [SerializeField]
    [Tooltip("Frames überspringen, um die Performance zu verbessern (1 = jeden Frame verarbeiten, 2 = jeden zweiten Frame, usw.)")]
    int frameSkip = 1;

    // Cache für wiederverwendbare Ressourcen
    private Texture2D _cameraTexture;
    private Texture2D _processedTexture;
    private Renderer _debugRenderer;
    
    // Wiederverwendbare RenderTextures und Materials
    private RenderTexture _scaledRT;
    private RenderTexture _finalRT;
    private Material _processingMaterial;
    
    // Puffer für die finale Ausgabe
    private byte[] _outputBuffer;
    
    // Frame-Zähler für Frame-Skipping
    private int _frameCounter = 0;

    [Header("Camera Information")]
    [SerializeField]
    private string cameraResolution = "Not Available";
    [SerializeField]
    private string cameraAspectRatio = "Not Available";
    [SerializeField]
    private float processingTimeMs = 0;

    // Shader für Bildverarbeitung
    private static readonly string ProcessingShaderName = "Hidden/ARCameraProcessing";
    
    // Für Performance-Messung
    private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

    void Start()
    {
        if (cameraManager == null)
            cameraManager = FindObjectOfType<ARCameraManager>();
            
        if (debugPlane != null)
        {
            _debugRenderer = debugPlane.GetComponent<Renderer>();
            if (_debugRenderer == null)
            {
                Debug.LogWarning("Die angegebene Debug-Plane hat keine Renderer-Komponente!");
            }
        }
        
        // Shader laden und Material erstellen
        if (_processingMaterial == null)
        {
            Shader processingShader = Shader.Find(ProcessingShaderName);
            if (processingShader == null)
            {
                //Debug.LogError($"Shader '{ProcessingShaderName}' nicht gefunden! Erstelle einfaches Material.");
                _processingMaterial = new Material(Shader.Find("Unlit/Texture"));
            }
            else
            {
                _processingMaterial = new Material(processingShader);
            }
        }
        
        // Ausgabepuffer einmal anlegen
        _outputBuffer = new byte[outputWidth * outputHeight * 4]; // 4 Bytes pro Pixel (RGBA)
    }

    void OnEnable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived -= OnCameraFrameReceived;
    }
    
    void OnDestroy()
    {
        CleanupResources();
    }
    
    // Methode zum Aufräumen aller Ressourcen
    private void CleanupResources()
    {
        if (_cameraTexture != null)
        {
            Destroy(_cameraTexture);
            _cameraTexture = null;
        }
            
        if (_processedTexture != null)
        {
            Destroy(_processedTexture);
            _processedTexture = null;
        }
        
        if (_processingMaterial != null)
        {
            Destroy(_processingMaterial);
            _processingMaterial = null;
        }
        
        if (_scaledRT != null)
        {
            _scaledRT.Release();
            Destroy(_scaledRT);
            _scaledRT = null;
        }
        
        if (_finalRT != null)
        {
            _finalRT.Release();
            Destroy(_finalRT);
            _finalRT = null;
        }
    }

    // Methode zum Protokollieren der Kamera-Informationen
    private void LogCameraInfo(XRCpuImage image)
    {
        // Auflösung der Kamera
        int width = image.width;
        int height = image.height;
        
        // Seitenverhältnis berechnen
        float aspectRatio = (float)width / height;
        
        // Informationen loggen
        Debug.Log($"AR Camera Info: Resolution: {width}x{height}, Aspect Ratio: {aspectRatio:F2}");
        
        // Werte für den Inspector aktualisieren
        cameraResolution = $"{width}x{height}";
        cameraAspectRatio = $"{aspectRatio:F2}";
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        // Frame-Skipping implementieren
        _frameCounter++;
        if (_frameCounter < frameSkip)
            return;
        
        _frameCounter = 0;
        
        // Prüfen, ob wir ein CPU-Bild haben
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        using (image)
        {
            // Kamera-Informationen protokollieren
            if (Time.frameCount % 60 == 0) // Jede 60 Frames
            {
                LogCameraInfo(image);
            }
            
            // Konvertierungsparameter einstellen
            var conversionParams = new XRCpuImage.ConversionParams
            {
                // Ganzes Bild verwenden
                inputRect = new RectInt(0, 0, image.width, image.height),
                
                // Ausgabedimensionen = Eingabedimensionen 
                outputDimensions = new Vector2Int(image.width, image.height),
                
                // Wähle ein Format, das MediaPipe verarbeiten kann (RGB oder RGBA)
                outputFormat = TextureFormat.RGBA32,
                
                // Keine Transformation
                transformation = XRCpuImage.Transformation.None
            };

            int dataSize = image.GetConvertedDataSize(conversionParams);
            
            // Wir erstellen ein NativeArray für die Datenkonvertierung
            var rawData = new NativeArray<byte>(dataSize, Allocator.Temp);
            
            try
            {
                // Zeitmessung starten
                _stopwatch.Reset();
                _stopwatch.Start();
                
                // Bild konvertieren
                image.Convert(conversionParams, rawData);
                
                // Auf die Zielgröße (1280x720) skalieren für MediaPipe
                byte[] processedData = ProcessTextureDataOptimized(rawData, image.width, image.height, 
                                                         outputWidth, outputHeight);
                
                // Debug-Anzeige aktualisieren
                if (enableDebugView && _debugRenderer != null)
                {
                    // Hier verwenden wir die bereits prozessierten Daten für die Debug-Anzeige
                    UpdateDebugTexture(processedData, outputWidth, outputHeight);
                }
                
                // Zeitmessung stoppen und speichern
                _stopwatch.Stop();
                processingTimeMs = _stopwatch.ElapsedMilliseconds;
                
                // Event auslösen und verarbeitete Daten an MediaPipe übergeben
                OnImageProcessed?.Invoke(processedData, outputWidth, outputHeight);
            }
            finally
            {
                // NativeArray freigeben
                if (rawData.IsCreated)
                    rawData.Dispose();
            }
        }
    }

    private void UpdateDebugTexture(byte[] processedData, int width, int height)
    {
        // Texture erstellen oder wiederverwenden
        if (_processedTexture == null || _processedTexture.width != width || _processedTexture.height != height)
        {
            if (_processedTexture != null)
                Destroy(_processedTexture);
                
            _processedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        // Pixeldaten direkt in die Texture laden
        _processedTexture.LoadRawTextureData(processedData);
        _processedTexture.Apply();

        // Texture dem Renderer zuweisen
        _debugRenderer.material.mainTexture = _processedTexture;
    }
    
    // Optimierte Version von ProcessTextureData
    private byte[] ProcessTextureDataOptimized(NativeArray<byte> sourceData, int sourceWidth, int sourceHeight, 
                                              int targetWidth, int targetHeight)
    {
        // 1. Quell-Texture vorbereiten (wiederverwenden wenn möglich)
        if (_cameraTexture == null || _cameraTexture.width != sourceWidth || _cameraTexture.height != sourceHeight)
        {
            if (_cameraTexture != null)
                Destroy(_cameraTexture);
                
            _cameraTexture = new Texture2D(sourceWidth, sourceHeight, TextureFormat.RGBA32, false);
        }
        
        // Daten in die Quell-Texture laden
        _cameraTexture.LoadRawTextureData(sourceData);
        _cameraTexture.Apply();
        
        // 2. Berechnungen für Cropping
        float widthScale = (float)sourceWidth / targetWidth;
        float scaledHeight = sourceHeight / widthScale;
        int cropFromTopBottom = Mathf.RoundToInt((scaledHeight - targetHeight) / 2f);
        
        // 3. RenderTextures vorbereiten (wiederverwenden wenn möglich)
        if (_scaledRT == null || _scaledRT.width != targetWidth || _scaledRT.height != Mathf.RoundToInt(scaledHeight))
        {
            if (_scaledRT != null)
            {
                _scaledRT.Release();
                Destroy(_scaledRT);
            }
            
            _scaledRT = new RenderTexture(targetWidth, Mathf.RoundToInt(scaledHeight), 0, RenderTextureFormat.ARGB32);
            _scaledRT.Create();
        }
        
        if (_finalRT == null || _finalRT.width != targetWidth || _finalRT.height != targetHeight)
        {
            if (_finalRT != null)
            {
                _finalRT.Release();
                Destroy(_finalRT);
            }
            
            _finalRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            _finalRT.Create();
        }
        
        // 4. Quell-Texture in RenderTexture zur Skalierung
        // Hier verwenden wir Graphics.Blit mit Scale-Parameter
        Graphics.Blit(_cameraTexture, _scaledRT);
        
        // 5. Crop das Bild (nur den gewünschten Bereich kopieren)
        RenderTexture.active = _scaledRT;
        
        // Verwende die existierende Texture oder erzeuge eine neue
        Texture2D croppedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        
        // Kopiere nur den zentralen Bereich
        croppedTexture.ReadPixels(
            new Rect(0, cropFromTopBottom, targetWidth, targetHeight), 
            0, 0);
        croppedTexture.Apply();
        
        // RenderTexture-Zustand wiederherstellen
        RenderTexture.active = null;
        
        // 6. Vertikale Spiegelung anwenden, wenn aktiviert
        byte[] finalData;
        
        if (mirrorVertically)
        {
            // Anstatt ein Color[]-Array zu nutzen, verwenden wir direkt einen Byte-Buffer
            // und spiegeln die Zeilen beim Kopieren
            finalData = new byte[targetWidth * targetHeight * 4]; // RGBA
            byte[] tempData = croppedTexture.GetRawTextureData();
            
            int bytesPerRow = targetWidth * 4; // 4 Bytes pro Pixel (RGBA)
            
            // Zeilen kopieren und umkehren
            for (int y = 0; y < targetHeight; y++)
            {
                int srcRow = y * bytesPerRow;
                int dstRow = (targetHeight - 1 - y) * bytesPerRow;
                
                // Reihe für Reihe kopieren
                Buffer.BlockCopy(tempData, srcRow, finalData, dstRow, bytesPerRow);
            }
        }
        else
        {
            // Direkt die Rohdaten verwenden, ohne Spiegelung
            finalData = croppedTexture.GetRawTextureData();
        }
        
        // Temporäre Texture freigeben
        Destroy(croppedTexture);
        
        return finalData;
    }
}