using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.Samples;
using UnityEngine;
public static class AnchorUuidStore
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "screenshot_anchors.json");

    public static void Save(ScreenshotAnchorData data)
    {
        var list = LoadAll();
        list.RemoveAll(d => d.uuid == data.uuid);
        list.Add(data);
        string json = JsonUtility.ToJson(new Wrapper { anchors = list });
        File.WriteAllText(FilePath, json);
    }

    public static void Remove(Guid uuid)
    {
        var list = LoadAll();
        int removedCount = list.RemoveAll(d => d.uuid == uuid.ToString());
        
        if (removedCount > 0)
        {
            string json = JsonUtility.ToJson(new Wrapper { anchors = list });
            File.WriteAllText(FilePath, json);
            Debug.Log($"[AnchorUuidStore] Removed anchor with UUID: {uuid}");
        }
        else
        {
            Debug.LogWarning($"[AnchorUuidStore] UUID not found: {uuid}");
        }
    }

    public static List<ScreenshotAnchorData> LoadAll()
    {
        if (!File.Exists(FilePath)) return new List<ScreenshotAnchorData>();
        string json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<Wrapper>(json).anchors;
    }

    public static void EraseAll()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log("[AnchorUuidStore] All anchors erased and file deleted.");
        }
        else
        {
            Debug.LogWarning("[AnchorUuidStore] No anchor file found to erase.");
        }
    }


    [System.Serializable]
    private class Wrapper
    {
        public List<ScreenshotAnchorData> anchors;
    }
}
