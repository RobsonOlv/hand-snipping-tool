using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AnchorManager : MonoBehaviour
{
    public GameObject MenuList;
    private List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new();
    private const string AnchorIdsKey = "SavedAnchorIds";

    private void Start()
    {
        // AnchorUuidStore.EraseAll();
        LoadAnchorsByUuid();
    }

    public async void SaveAnchor(GameObject screenshotContainer, Texture2D texture = null)
    {
        var existingAnchor = screenshotContainer.GetComponent<OVRSpatialAnchor>();
        if (existingAnchor != null)
        {
            Debug.Log($"[AnchorManager] Erasing {existingAnchor.Uuid}.");
            await EraseAnchor(existingAnchor);
        }

        var anchor = screenshotContainer.AddComponent<OVRSpatialAnchor>();
        await anchor.WhenLocalizedAsync();

        Debug.Log($"[AnchorManager] anchor after Erase {anchor.Uuid}...");
        await UniTask.WaitUntil(() => anchor.Uuid != Guid.Empty);
        var result = await anchor.SaveAnchorAsync();
        Debug.Log($"[AnchorManager] Saving anchor with {anchor.Uuid}...");
        if (result.Success)
        {
            Debug.LogWarning($"[AnchorManager] Anchor with {anchor.Uuid} saved");
            if (texture != null)
            {
                string filename = $"screenshot_{anchor.Uuid}.png";
                string fullPath = Path.Combine(Application.persistentDataPath, filename);
                File.WriteAllBytes(fullPath, texture.EncodeToPNG());

                string audioPath = "";
                AudioHolder audioHolder = screenshotContainer.GetComponent<AudioHolder>();
                if (audioHolder != null && audioHolder.recordedClip != null)
                {
                    string audioFilename = $"audio_{anchor.Uuid}.wav";
                    audioPath = Path.Combine(Application.persistentDataPath, audioFilename);
                    if (SaveAudioClipToWav(audioHolder.recordedClip, audioPath))
                    {
                        Debug.Log($"[AnchorManager] Audio saved to {audioPath}");
                    }
                    else
                    {
                        Debug.LogError($"[AnchorManager] Failed to save audio to {audioPath}");
                        audioPath = ""; // Reset if failed
                    }
                }

                float worldWidth = 0;
                float worldHeight = 0;
                var dimensions = screenshotContainer.GetComponent<ScreenshotDimensions>();
                if (dimensions != null)
                {
                    worldWidth = dimensions.worldWidth;
                    worldHeight = dimensions.worldHeight;
                }

                var data = new ScreenshotAnchorData
                {
                    uuid = anchor.Uuid.ToString(),
                    texturePath = fullPath,
                    audioPath = audioPath,
                    textureHeight = texture.height,
                    textureWidth = texture.width,
                    worldWidth = worldWidth,
                    worldHeight = worldHeight,
                    localScale = anchor.transform.localScale
                };

                AnchorUuidStore.Save(data);
            }
        }
        else
        {
            Debug.LogWarning($"[AnchorManager] Anchor with {anchor.Uuid} failed to saved");
        }
        anchor.enabled = false;
    }

    private bool SaveAudioClipToWav(AudioClip clip, string filePath)
    {
        try
        {
            using (var fileStream = CreateEmpty(filePath))
            {
                ConvertAndWrite(fileStream, clip);
                WriteHeader(fileStream, clip);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnchorManager] Error saving WAV: {e.Message}");
            return false;
        }
    }

    private FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte[] emptyByte = new byte[1];

        for (int i = 0; i < 44; i++) //preparing the header
        {
            fileStream.WriteByte(emptyByte[0]);
        }

        return fileStream;
    }

    private void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

        Byte[] bytesData = new Byte[samples.Length * 2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        int rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);

        // fileStream.Close();
    }

    public async UniTask EraseAnchor(OVRSpatialAnchor anchor)
    {
        var uuid = anchor.Uuid;
        var result = await anchor.EraseAnchorAsync();
        if (result.Success)
        {
            Debug.LogWarning($"[AnchorManager] Erased anchor data {uuid}");
            AnchorUuidStore.Remove(uuid);
            Destroy(anchor);
            await UniTask.NextFrame();
        }
        else
        {
            Debug.LogError($"[AnchorManager] Failed to erased anchor data {uuid}");
        }
    }

    async void LoadAnchorsByUuid()
    {
        var allData = AnchorUuidStore.LoadAll(); // Carrega todos os ScreenshotAnchorData salvos
        var uuids = allData.Select(d => Guid.Parse(d.uuid)).ToList();
        Debug.Log($"[AnchorManager] Loading anchors with uuids: {string.Join(", ", uuids)}");

        _unboundAnchors.Clear();

        // Step 1: Load
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, _unboundAnchors);

        if (result.Success)
        {
            Debug.LogWarning($"[AnchorManager] Anchors loaded successfully.");

            // Note result.Value is the same as _unboundAnchors
            foreach (var unboundAnchor in result.Value)
            {
                var data = allData.FirstOrDefault(d => d.uuid == unboundAnchor.Uuid.ToString());
                if (data == null)
                {
                    Debug.LogWarning($"[AnchorManager] No data found for anchor {unboundAnchor.Uuid}. Skipping localization.");
                    continue;
                }
                // Step 2: Localize
                bool success = await unboundAnchor.LocalizeAsync();
                if (success)
                {
                    var interactionContainer = new GameObject($"Anchor_{unboundAnchor.Uuid}");
                    var spatialAnchor = interactionContainer.AddComponent<OVRSpatialAnchor>();
                    unboundAnchor.BindTo(spatialAnchor);

                    await spatialAnchor.WhenLocalizedAsync();
                    spatialAnchor.enabled = false;

                    interactionContainer.transform.localScale = data.localScale;

                    if (!File.Exists(data.texturePath))
                    {
                        Debug.LogWarning($"[AnchorManager] Texture not found: {data.texturePath}");
                        return;
                    }

                    byte[] textureBytes = File.ReadAllBytes(data.texturePath);
                    Texture2D texture = new Texture2D(data.textureWidth, data.textureHeight, TextureFormat.RGBA32, false);
                    texture.LoadImage(textureBytes);

                    AudioClip loadedAudio = null;
                    if (!string.IsNullOrEmpty(data.audioPath) && File.Exists(data.audioPath))
                    {
                        using (var uwr = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + data.audioPath, AudioType.WAV))
                        {
                            await uwr.SendWebRequest();
                            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                            {
                                loadedAudio = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(uwr);
                                loadedAudio.name = "LoadedRecording";
                                Debug.Log($"[AnchorManager] Audio loaded from {data.audioPath}");
                            }
                            else
                            {
                                Debug.LogError($"[AnchorManager] Failed to load audio: {uwr.error}");
                            }
                        }
                    }

                    var screenshotParams = new ScreenShotCreationParams
                    {
                        AnchorObject = interactionContainer,
                        Menu = MenuList,
                        Texture = texture,
                        RecordedAudio = loadedAudio,
                        WorldWidth = data.worldWidth,
                        WorldHeight = data.worldHeight
                    };

                    new ScreenShotComponent(screenshotParams);
                }
                else
                {
                    Debug.LogError($"[AnchorManager] Localization failed for anchor {unboundAnchor.Uuid}");
                }
            }
        }
        else
        {
            Debug.LogError($"[AnchorManager] Load failed with error {result.Status}.");
        }
    }
}
