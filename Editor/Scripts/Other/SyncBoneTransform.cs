﻿using UnityEditor;
using UnityEngine;
using Yueby.Utils;

namespace Yueby.AvatarTools.Other
{
    public class SyncBoneTransform : EditorWindow
    {
        private Transform _origin, _target;
        private bool _isUsingWorldSpace;

        private void OnGUI()
        {
            EditorUI.DrawEditorTitle("同步骨骼Transform");

            EditorUI.VerticalEGLTitled("配置", () =>
            {
                _origin = (Transform)EditorUI.ObjectField("源", 50, _origin, typeof(Transform), true);
                _target = (Transform)EditorUI.ObjectField("当前", 50, _target, typeof(Transform), true);
                _isUsingWorldSpace = EditorUI.Radio(_isUsingWorldSpace, "使用世界坐标");
            });

            EditorUI.VerticalEGLTitled("操作", () =>
            {
                if (GUILayout.Button("同步"))
                {
                    if (_origin != null && _target != null)
                        SyncBone(_origin, _target);
                }
            });
        }

        [MenuItem("Tools/YuebyTools/VRChat/Avatar/Sync Bone Transform", false, 41)]
        private static void Open()
        {
            var window = GetWindow<SyncBoneTransform>();
            window.titleContent = new GUIContent("同步骨骼Transform");
            window.minSize = new Vector2(400, 600);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void SyncBone(Transform origin, Transform target)
        {
            if (_isUsingWorldSpace)
            {
                origin.transform.position = Vector3.zero;
                target.transform.position = Vector3.zero;
            }

            foreach (var originTrans in origin.GetComponentsInChildren<Transform>(true))
                foreach (var targetTrans in target.GetComponentsInChildren<Transform>())
                {
                    if (!targetTrans.name.Equals(originTrans.name)) continue;

                    if (!_isUsingWorldSpace)
                    {
                        targetTrans.localPosition = originTrans.localPosition;
                        targetTrans.localRotation = originTrans.localRotation;
                        targetTrans.localScale = originTrans.localScale;
                    }
                    else
                    {
                        targetTrans.position = originTrans.position;
                        targetTrans.rotation = originTrans.rotation;
                        targetTrans.localScale = originTrans.localScale;
                    }

                    break;
                }
        }
    }
}