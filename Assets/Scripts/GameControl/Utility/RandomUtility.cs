using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameControl.Interface;
using Random = UnityEngine.Random;

public static class RandomUtility
{
    public static T GetWeightedRandom<T>(List<T> list) where T : IRandomable
    {
        if (list == null || list.Count == 0)
            return default;

        float totalWeight = 0;
        foreach (var item in list) totalWeight += Mathf.Max(0, item.Chance);

        if (totalWeight == 0)
            return default;

        var randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0;

        foreach (var item in list)
        {
            currentWeight += Mathf.Max(0, item.Chance);
            if (randomValue < currentWeight)
                return item;
        }

        return list[^1];
    }

    public static T GetWeightedRandomById<T>(
        List<T> list,
        Dictionary<string, float> idWeights,
        Func<T, string> idSelector) where T : IRandomable
    {
        if (list == null || list.Count == 0) return default;
        if (idWeights == null || idWeights.Count == 0) return GetWeightedRandom(list);

        var candidates = BuildCandidates(list, idWeights, idSelector);
        if (candidates.Length == 0) return GetWeightedRandom(list);
        return PickByWeight(candidates);
    }

    static (T item, float weight)[] BuildCandidates<T>(List<T> list, Dictionary<string, float> idWeights, Func<T, string> idSelector)
    {
        var tmp = new List<(T, float)>(list.Count);
        foreach (var it in list)
        {
            var id = idSelector(it);
            if (id != null && idWeights.TryGetValue(id, out var w) && w > 0f)
                tmp.Add((it, w));
        }
        return tmp.ToArray();
    }

    static T PickByWeight<T>((T item, float weight)[] candidates)
    {
        float total = 0f;
        foreach (var c in candidates) total += c.weight;
        var r = UnityEngine.Random.Range(0f, total);
        float acc = 0f;
        foreach (var c in candidates)
        {
            acc += c.weight;
            if (r <= acc) return c.item;
        }
        return candidates[candidates.Length - 1].item;
    }
}
