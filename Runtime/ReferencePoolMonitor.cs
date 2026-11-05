#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Dories.Reference.Runtime
{
    /// <summary>
    /// 编辑器下由 ReferencePool 构造时自动创建，供 Inspector 实时查看引用池状态。
    /// </summary>
    internal class ReferencePoolMonitor : MonoBehaviour
    {
        internal ReferencePool ReferencePool { get; private set; }
        
        internal static void Create(ReferencePool referencePool)
        {
            // DontDestroyOnLoad 只能在 Play Mode 调用；编辑模式（如 EditMode 测试）下不建节点
            if (!Application.isPlaying)
                return;

            var go = new GameObject("[ReferencePool Monitor]");
            DontDestroyOnLoad(go);
            go.AddComponent<ReferencePoolMonitor>().ReferencePool = referencePool;
        }
    }
}
#endif
