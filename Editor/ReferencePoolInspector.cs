using System.Collections.Generic;
using Dories.Reference.Runtime;
using UnityEditor;
using UnityEngine;

namespace Dories.Reference.Editor
{
    [CustomEditor(typeof(ReferencePoolMonitor))]
    internal class ReferencePoolInspector : UnityEditor.Editor
    {
        private readonly List<ReferenceCollection> _collections = new();
        private Vector2 _scrollPosition;
        private bool _monitorFoldout = true;

        /// <summary>
        /// 检查该编辑器当前状态是否需要不断重新绘制
        /// </summary>
        /// <returns></returns>
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawMonitor();
        }

        private void DrawMonitor()
        {
            _monitorFoldout = EditorGUILayout.Foldout(_monitorFoldout, "References", true);
            if (!_monitorFoldout)
                return;
            
            EditorGUI.indentLevel++;
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后显示各引用池状态。", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            var pool = ((ReferencePoolMonitor)target).ReferencePool;
            pool.GetCollections(_collections);
            EditorGUILayout.LabelField("引用池数量", _collections.Count.ToString());

            if (_collections.Count == 0)
            {
                EditorGUILayout.LabelField("(无引用池)", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Type", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Unused", EditorStyles.miniLabel, GUILayout.Width(48f));
            EditorGUILayout.LabelField("Using", EditorStyles.miniLabel, GUILayout.Width(44f));
            EditorGUILayout.LabelField("Acquire", EditorStyles.miniLabel, GUILayout.Width(52f));
            EditorGUILayout.LabelField("Release", EditorStyles.miniLabel, GUILayout.Width(52f));
            EditorGUILayout.LabelField("Add", EditorStyles.miniLabel, GUILayout.Width(40f));
            EditorGUILayout.LabelField("Remove", EditorStyles.miniLabel, GUILayout.Width(52f));
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(280f));
            foreach (var c in _collections)
            {
                EditorGUILayout.BeginHorizontal();
                // Using 为负说明重复 Release，标红
                var color = GUI.color;
                if (c.UsingReferenceCount < 0)
                    GUI.color = Color.red;
                GUILayout.Label(new GUIContent(c.ReferenceType.Name, c.ReferenceType.FullName));
                GUILayout.Label(c.UnusedReferenceCount.ToString(), GUILayout.Width(48f));
                GUILayout.Label(c.UsingReferenceCount.ToString(), GUILayout.Width(44f));
                GUILayout.Label(c.AcquireReferenceCount.ToString(), GUILayout.Width(52f));
                GUILayout.Label(c.ReleaseReferenceCount.ToString(), GUILayout.Width(52f));
                GUILayout.Label(c.AddReferenceCount.ToString(), GUILayout.Width(40f));
                GUILayout.Label(c.RemoveReferenceCount.ToString(), GUILayout.Width(52f));
                GUI.color = color;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Clear All"))
                pool.ClearAll();

            EditorGUI.indentLevel--;
        }
    }
}
