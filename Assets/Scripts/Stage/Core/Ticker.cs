using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITickable { void Tick(); }

public class Ticker : MonoBehaviour
{
    private List<ITickable> tickables = new();
    private float tickInterval = 0.1f;
    private float timer;

    public void Register(ITickable tickable) => tickables.Add(tickable);
    public void Unregister(ITickable tickable) => tickables.Remove(tickable);

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            foreach (var tickable in tickables)
                tickable.Tick();
            timer = 0;
        }
    }
}