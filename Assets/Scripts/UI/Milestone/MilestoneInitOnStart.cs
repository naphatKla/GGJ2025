using System.Collections;
using System.Collections.Generic;
using Interface;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Milestone
{
    [RequireComponent(typeof(LoopScrollRect))]
    [DisallowMultipleComponent]
    public class MilestoneInitOnStart : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource, IRefreshUI
    {
        [Space,Title("Setting")]
        public GameObject item;
        public int totalCount = -1;
        
        public GameObject GetObject(int index)
        {
            throw new System.NotImplementedException();
        }

        public void ReturnObject(Transform trans)
        {
            throw new System.NotImplementedException();
        }

        public void ProvideData(Transform transform, int idx)
        {
            throw new System.NotImplementedException();
        }

        public GameObject GetGameObject()
        {
            throw new System.NotImplementedException();
        }

        public void RefreshUI()
        {
            throw new System.NotImplementedException();
        }
    }
}


