using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interface
{
    public interface IRefreshUI
    {
        GameObject GetGameObject();
        void RefreshUI();
    }
}