using System.Collections.Generic;
using Dories.Reference.Runtime;
using UnityEngine;

namespace Dories.Reference.Sample
{
    /// <summary>
    /// 引用池示例：每帧自动借还 HitEvent，按钮手动借还 DamageInfo。
    /// 运行后选中 Hierarchy 里的 [ReferencePool Monitor] 查看实时状态。
    /// </summary>
    public class ReferencePoolSample : MonoBehaviour
    {
        private class HitEvent : IReference
        {
            public Vector3 Position;

            public void Clear()
            {
                Position = default;
            }
        }

        private class DamageInfo : IReference
        {
            public int Value;

            public void Clear()
            {
                Value = 0;
            }
        }

        [SerializeField] private bool _enableStrictCheck = true;
        [SerializeField] private int _hitsPerFrame = 5;

        private readonly List<DamageInfo> _held = new();
        private ReferencePool _pool;
        private bool _autoHits = true;

        private void Awake()
        {
            _pool = new ReferencePool { EnableStrictCheck = _enableStrictCheck };
        }

        private void Update()
        {
            if (!_autoHits)
                return;

            // 当帧借、当帧还：稳定后 Add 不再增长，说明对象在被复用
            for (int i = 0; i < _hitsPerFrame; i++)
            {
                var hit = _pool.Acquire<HitEvent>();
                hit.Position = Random.insideUnitSphere;
                _pool.Release(hit);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 260, 400), GUI.skin.box);
            GUILayout.Label($"持有 DamageInfo: {_held.Count}");

            _autoHits = GUILayout.Toggle(_autoHits, "每帧自动借还 HitEvent");

            if (GUILayout.Button("Acquire 10 DamageInfo"))
            {
                for (int i = 0; i < 10; i++)
                {
                    var info = _pool.Acquire<DamageInfo>();
                    info.Value = Random.Range(1, 100);
                    _held.Add(info);
                }
            }

            if (GUILayout.Button("Release All DamageInfo"))
            {
                foreach (var info in _held)
                    _pool.Release(info);
                _held.Clear();
            }

            if (GUILayout.Button("Warm Up 20 DamageInfo"))
                _pool.Add<DamageInfo>(20);

            if (GUILayout.Button("Remove Unused DamageInfo"))
                _pool.RemoveAll<DamageInfo>();

            GUILayout.EndArea();
        }
    }
}
