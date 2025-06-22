using System.Collections;
using System.Collections.Generic;
using GameControl.Interface;
using UnityEngine;

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

        var randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var item in list)
        {
            currentWeight += Mathf.Max(0, item.Chance);
            if (randomValue < currentWeight)
                return item;
        }

        return list[^1];
    }
}
