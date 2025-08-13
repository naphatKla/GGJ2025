using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameControl.EventMap
{
    public interface IHitboxProvider
    {
        HitboxType HitboxType { get; }
    }

    public interface IBoxHitbox : IHitboxProvider
    {
        Vector3 Size { get; set; }
        Vector3 Offset { get; set;}
    }

    public interface ISphereHitbox : IHitboxProvider
    {
        float Radius { get; set;}
        Vector3 Offset { get; set;}
    }

    public interface ICapsuleHitbox : IHitboxProvider
    {
        float Radius { get; set;}
        float Height { get; set;}
        Vector3 Offset { get; set;}
    }

}
