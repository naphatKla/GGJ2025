using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UI.Transition
{
    public abstract class TransitionBase : ScriptableObject
    {
        public abstract UniTask PlayAsync(GameObject target, bool isAppear,CancellationToken cancellationToken);
    }
}