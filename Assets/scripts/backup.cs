// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Meta.XR.Samples;
// using UnityEngine;
// public static class AnchorUuidStore
// {
//     public const string NumUuidsPlayerPref = "numUuids";

//     public static int Count => PlayerPrefs.GetInt(NumUuidsPlayerPref, 0);

//     public static HashSet<Guid> Uuids
//     {
//         get => Enumerable
//             .Range(0, Count)
//             .Select(GetUuidKey)
//             .Select(PlayerPrefs.GetString)
//             .Select(str => Guid.TryParse(str, out var uuid) ? uuid : Guid.Empty)
//             .Where(uuid => uuid != Guid.Empty)
//             .ToHashSet();

//         set
//         {
//             // Delete everything beyond the new count
//             foreach (var key in Enumerable.Range(0, Count).Select(GetUuidKey))
//             {
//                 PlayerPrefs.DeleteKey(key);
//             }

//             // Set the new count
//             PlayerPrefs.SetInt(NumUuidsPlayerPref, value.Count);

//             // Update all the uuids
//             var index = 0;
//             foreach (var uuid in value)
//             {
//                 PlayerPrefs.SetString(GetUuidKey(index++), uuid.ToString());
//             }
//         }
//     }

//     public static void Add(Guid uuid)
//     {
//         var uuids = Uuids;
//         if (uuids.Add(uuid))
//         {
//             Uuids = uuids;
//         }
//     }

//     public static void Remove(Guid uuid)
//     {
//         var uuids = Uuids;
//         if (uuids.Remove(uuid))
//         {
//             Uuids = uuids;
//         }
//     }

//     static string GetUuidKey(int index) => $"uuid{index}";
// }
