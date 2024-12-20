﻿using System;
using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;
using Yueby.Utils;

namespace YuebyAvatarTools.PhysBoneTransfer.Editor
{
    public class PhysboneTransfer : EditorWindow
    {
        private Vector2 _pos;
        private Transform _origin;
        private static bool _isTransferMAMergeArmature = true;
        private static bool _isTransferMAMergeAnimator = true;
        private static bool _isTransferMABoneProxy = true;
        private static bool _isTransferMaterials = true;

        [MenuItem("Tools/YuebyTools/VRChat/Avatar/PhysBone Transfer", false, 21)]
        public static void OpenWindow()
        {

            var window = CreateWindow<PhysboneTransfer>();

            window.titleContent = new GUIContent("Avatar材质动骨转移");
            window.minSize = new Vector2(500, 450);

            window.Show();
        }

        private void OnGUI()
        {
            var selections = Selection.gameObjects;
            var isSelectedObject = selections.Length > 0;

            EditorUI.DrawEditorTitle("Avatar材质动骨转移");
            EditorUI.VerticalEGLTitled("配置", () =>
            {
                EditorUI.HorizontalEGL(() =>
                {
                    _origin = (Transform)EditorUI.ObjectFieldVertical(_origin, "原Armature", typeof(Transform));
                    EditorUI.Line(LineType.Vertical);
                    // _current = (Transform)EditorUI.ObjectFieldVertical(_current, "现Armature", typeof(Transform));
                }, GUILayout.MaxHeight(40));
            });

            EditorUI.VerticalEGLTitled("设置", () =>
            {
                _isTransferMAMergeArmature = EditorUI.Toggle(_isTransferMAMergeArmature, "转移ModularAvatarMergeArmature组件");
                _isTransferMAMergeAnimator = EditorUI.Toggle(_isTransferMAMergeAnimator, "转移ModularAvatarMergeAnimator组件");
                _isTransferMABoneProxy = EditorUI.Toggle(_isTransferMABoneProxy, "转移ModularAvatarBoneProxy组件");
                _isTransferMaterials = EditorUI.Toggle(_isTransferMaterials, "转移材质");

                if (GUILayout.Button("转移"))
                {
                    foreach (var selection in selections)
                    {
                        Recursive(_origin, selection.transform);

                        if (_isTransferMaterials)
                            TransferMaterials(selection.transform);
                        if (_isTransferMAMergeArmature)
                            CopyComponentByType<ModularAvatarMergeAnimator>(selection, _origin);
                    }

                }
            });
            EditorUI.VerticalEGLTitled("选中列表", () =>
            {
                if (isSelectedObject)
                    _pos = EditorUI.ScrollViewEGL(() =>
                    {
                        foreach (var selection in selections)
                            EditorGUILayout.ObjectField(selection, typeof(GameObject), true);
                    }, _pos, GUILayout.Height(200));
                else
                    EditorGUILayout.HelpBox("请在场景中选中需要删除的对象", MessageType.Error);
            });
        }

        private void TransferMaterials(Transform current)
        {
            var originRenderers = _origin.GetComponentsInChildren<Renderer>();

            if (originRenderers.Length <= 0) return;
            foreach (var originRenderer in originRenderers)
            {
                var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originRenderer.transform).Replace($"{_origin.name}", $"{current.name}");
                var targetGo = GameObject.Find(targetPath);
                if (!targetGo) continue;
                var targetRenderer = targetGo.GetComponent<Renderer>();

                var materials = originRenderer.sharedMaterials;

                targetRenderer.sharedMaterials = materials;
                Undo.RegisterFullObjectHierarchyUndo(targetGo, "TransferMaterial");
                // Debug.Log("...Transfer Materials");
            }
        }

        private void Recursive(Transform parent, Transform current)
        {
            foreach (Transform child in parent)
            {
                TransferPhysBone(child, current);
                TransferConstraint(child, current);
                TransferComponents(child, current);
                Recursive(child, current);
                // Debug.Log(child);
            }
        }

        private void CopyComponentByType<T>(GameObject targetGo, Transform child) where T : Component
        {
            var components = child.GetComponents<T>();
            foreach (var component in components)
            {
                if (component == null)
                    continue;

                ComponentUtility.CopyComponent(component);
                var targetComponent = targetGo.GetComponent<T>();
                if (targetComponent != null)
                {
                    ComponentUtility.PasteComponentValues(targetComponent);
                }
                else
                {
                    ComponentUtility.PasteComponentAsNew(targetGo);
                }
            }
        }

        private void TransferComponents(Transform child, Transform current)
        {
            var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(child).Replace($"{_origin.name}", $"{current.name}");
            var targetGo = GameObject.Find(targetPath);
            if (targetGo == null) return;

            if (_isTransferMABoneProxy)
                CopyComponentByType<ModularAvatarBoneProxy>(targetGo, child);

            if (_isTransferMAMergeArmature)
                CopyComponentByType<ModularAvatarMergeArmature>(targetGo, child);

            if (_isTransferMAMergeAnimator)
                CopyComponentByType<ModularAvatarMergeAnimator>(targetGo, child);
        }

        private void TransferConstraint(Transform child, Transform current)
        {
            var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(child).Replace($"{_origin.name}", $"{current.name}");
            var targetGo = GameObject.Find(targetPath);
            if (targetGo == null) return;

            var parentConstraints = child.GetComponents<ParentConstraint>();
            var rotationConstraints = child.GetComponents<RotationConstraint>();
            var positionConstraints = child.GetComponents<PositionConstraint>();
            CopyConstraint(parentConstraints, (c, t) =>
            {
                // t.rotationAtRest = c.rotationAtRest;
                // t.translationAtRest = c.translationAtRest;

                // t.rotationOffsets = c.rotationOffsets;
                // t.translationOffsets = c.translationOffsets;
                // t.weight = c.weight;

            });

            CopyConstraint(rotationConstraints, (c, t) =>
            {
                // t.rotationAtRest = c.rotationAtRest;
                // t.rotationOffset = c.rotationOffset;
                // t.weight = c.weight;

            });

            CopyConstraint(positionConstraints, (c, t) =>
            {
                // t.translationAtRest = c.translationAtRest;
                // t.translationOffset = c.translationOffset;
                // t.weight = c.weight;

            });

            return;

            void CopyConstraint<T>(T[] constraints, System.Action<T, T> action) where T : Component
            {
                foreach (var constraint in constraints)
                {
                    if (constraint == null)
                        continue;

                    var constraintCurrent = constraint as IConstraint;

                    var lastLocked = constraintCurrent.locked;
                    var lastActive = constraintCurrent.constraintActive;

                    constraintCurrent.locked = false;
                    constraintCurrent.constraintActive = false;

                    ComponentUtility.CopyComponent(constraint);
                    var targetConstraint = targetGo.GetComponent<T>();
                    if (targetConstraint != null)
                    {
                        ComponentUtility.PasteComponentValues(targetConstraint);
                    }
                    else
                    {
                        ComponentUtility.PasteComponentAsNew(targetGo);
                        targetConstraint = targetGo.GetComponent<T>();
                    }

                    constraintCurrent.locked = lastLocked;
                    constraintCurrent.constraintActive = lastActive;
                    action?.Invoke(constraint, targetConstraint);
                    ApplySource(constraint as IConstraint, targetConstraint as IConstraint);
                }
            }

            void ApplySource(IConstraint currentConstraint, IConstraint targetConstarint)
            {
                if (currentConstraint.sourceCount <= 0) return;

                //clear target source
                for (var i = 0; i < currentConstraint.sourceCount; i++)
                {
                    var source = currentConstraint.GetSource(i);
                    var targetSourceObject = GameObject.Find(VRC.Core.ExtensionMethods.GetHierarchyPath(source.sourceTransform).Replace($"{_origin.name}", $"{current.name}"));
                    if (targetSourceObject == null) continue;

                    // Debug.Log(targetSourceObject.name);

                    // target.RemoveSource(i);
                    targetConstarint.SetSource(i, new ConstraintSource()
                    {
                        sourceTransform = targetSourceObject.transform,
                        weight = source.weight
                    });
                }

                targetConstarint.constraintActive = currentConstraint.constraintActive;
                // target.locked = current.locked;
            }
        }

        private void TransferPhysBone(Transform child, Transform current)
        {
            var physBone = child.GetComponent<VRCPhysBone>();
            // var constraints = child.GetComponents<ParentConstraint>();

            if (physBone == null) return;

            if (physBone.rootTransform == null)
                physBone.rootTransform = physBone.transform;

            var targetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(physBone.rootTransform).Replace($"{_origin.name}", $"{current.name}");
            var targetGo = GameObject.Find(targetPath);

            if (targetGo != null)
            {
                ComponentUtility.CopyComponent(physBone);

                // var transform = child.transform;

                if (targetGo.GetComponent<VRCPhysBone>() != null)
                    ComponentUtility.PasteComponentValues(targetGo.GetComponent<VRCPhysBone>());
                else
                    ComponentUtility.PasteComponentAsNew(targetGo);

                var targetPhysBone = targetGo.GetComponent<VRCPhysBone>();

                if (physBone.rootTransform != null)
                {
                    var rootTransGo = GameObject.Find(VRC.Core.ExtensionMethods.GetHierarchyPath(physBone.rootTransform).Replace($"{_origin.name}", $"{current.name}"));
                    targetPhysBone.rootTransform = rootTransGo.transform;
                }
                else
                    targetPhysBone.rootTransform = targetGo.transform;

                // targetPhysBone.ignoreTransforms = GetIgnoreTransforms(targetPhysBone.ignoreTransforms);
                targetPhysBone.colliders = GetColliders(targetPhysBone.colliders, current);

                Undo.RegisterFullObjectHierarchyUndo(targetGo, "CopyComponent");
            }
        }

        // private List<Transform> GetIgnoreTransforms(List<Transform> transforms)
        // {
        //     var list = new List<Transform>();
        //
        //     foreach (var trans in transforms)
        //     {
        //         var targetPath = trans.GetHierarchyPath().Replace($"{_origin.name}", $"{_current.name}");
        //         var targetGo = GameObject.Find(targetPath);
        //     }
        //
        //     return list;
        // }

        private List<VRCPhysBoneColliderBase> GetColliders(List<VRCPhysBoneColliderBase> colliders, Transform current)
        {
            // 如果collider的rootTransform为空，则将rootTransform设置为自己，放到另一边avatar，collider对应parent对象下
            // 如果collider的rootTransform不为空,则保留rootTransform, 将rootTransform不变，放入rootTransform对应对象下
            List<VRCPhysBoneColliderBase> list = new List<VRCPhysBoneColliderBase>();
            foreach (var originCollider in colliders)
            {
                // 获得collider的RootTransform的路径
                if (originCollider == null) continue;
                if (originCollider.rootTransform == null)
                    originCollider.rootTransform = originCollider.transform;

                // 

                var colliderRootTransformPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originCollider.rootTransform).Replace($"{_origin.name}", $"{current.name}");
                var colRootTransGo = GameObject.Find(colliderRootTransformPath);

                // 如果RootTransform底下没有collider，则创建collider对象，并复制collider的属性到新创建的collider对象上
                // 如果RootTransform底下有collider，则直接复制collider的属性到底下的collider对象上
                if (colRootTransGo != null)
                {
                    // create a new gameObject as target gameObject child for the collider
                    var optionColTransform = colRootTransGo.transform;
                    if (colRootTransGo.transform.childCount > 0)
                        optionColTransform = colRootTransGo.transform.Find(colRootTransGo.name + "_PBC_Transfer");

                    // var pbcBase = colRootTransGo.GetComponent<VRCPhysBoneColliderBase>();

                    if (optionColTransform != null)
                    {
                        optionColTransform.transform.position = originCollider.transform.position;
                        optionColTransform.transform.rotation = originCollider.transform.rotation;

                        var colBase = optionColTransform.GetComponent<VRCPhysBoneColliderBase>();
                        ComponentUtility.CopyComponent(originCollider);
                        ComponentUtility.PasteComponentValues(colBase);

                        colBase.rootTransform = colRootTransGo.transform;
                        colRootTransGo = optionColTransform.gameObject;
                    }
                    else
                    {
                        var colGo = new GameObject(colRootTransGo.name + "_PBC_Transfer")
                        {
                            transform =
                            {
                                parent = colRootTransGo.transform,
                                position = originCollider.transform.position,
                                rotation = originCollider.transform.rotation,
                                localScale = originCollider.transform.localScale
                            }
                        };

                        ComponentUtility.CopyComponent(originCollider);
                        ComponentUtility.PasteComponentAsNew(colGo);
                        var colBase = colGo.GetComponent<VRCPhysBoneColliderBase>();

                        colBase.rootTransform = colGo.transform;
                        colRootTransGo = colGo;
                    }

                    Undo.RegisterFullObjectHierarchyUndo(colRootTransGo, "CopyComponent");
                }
                else if (originCollider.transform.childCount == 0)
                {
                    // 如果目标collider对象不存在，且没有子对象，则创建到col.transform.parent的对应位置
                    var parentPath = VRC.Core.ExtensionMethods.GetHierarchyPath(originCollider.transform.parent).Replace($"{_origin.name}", $"{current.name}");
                    var parent = GameObject.Find(parentPath);

                    // 如果parent不为空，检测parent地下的是否有该对象
                    if (parent != null)
                    {
                        var colGo = parent.transform.Find(originCollider.name);
                        if (colGo != null)
                        {
                            colRootTransGo = colGo.gameObject;
                            ComponentUtility.CopyComponent(originCollider);
                            ComponentUtility.PasteComponentValues(colRootTransGo.GetComponent<VRCPhysBoneColliderBase>());
                            var colBase = colRootTransGo.GetComponent<VRCPhysBoneColliderBase>();
                            colBase.rootTransform = colBase.transform;
                        }
                        else
                        {
                            colRootTransGo = new GameObject(originCollider.name)
                            {
                                transform =
                                {
                                    parent = parent.transform,
                                    position = originCollider.transform.position,
                                    rotation = originCollider.transform.rotation,
                                    localScale = originCollider.transform.localScale
                                }
                            };
                            Undo.RegisterCreatedObjectUndo(colRootTransGo, "CreateNewCollider");
                            ComponentUtility.CopyComponent(originCollider);
                            ComponentUtility.PasteComponentAsNew(colRootTransGo);
                            var colBase = colRootTransGo.GetComponent<VRCPhysBoneColliderBase>();
                            colBase.rootTransform = colBase.transform;
                        }
                    }
                }

                if (colRootTransGo != null)
                    list.Add(colRootTransGo.GetComponent<VRCPhysBoneColliderBase>());
            }

            return list;
        }
    }
}