
// Copyright notice - Licence: MIT. https://opensource.org/licenses/MIT
// Provided by Alex Meesters. www.alexmeesters.nl. Used within low-scope.com products.

/*
--- HOW TO USE? ---
On the component that you want have validation for:
Within the OnValidate method add: ValidateHierarchy.Add(this)
Within the OnDestroy method add: ValidateHierarchy.Remove(this)

Please note that the OnValidate method must be public. Else it won't work.
Cheers!
*/

# if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace Lowscope.Tools
{
    // This solution was required because there is no way to properly check if entire gameobject hierarchies are dirty.
    // This will do callbacks for subscribed objects when a property of an object with the same root GameObject has been modified.
    [InitializeOnLoad]
    public class ValidateHierarchy
    {
        static ValidateHierarchy()
        {
            Undo.postprocessModifications += OnPostProcessModifications;
            EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Selection.selectionChanged += LastSelection;
        }

        private static Transform lastRoot;

        private static void LastSelection()
        {
            if (Application.isPlaying)
                return;

            Transform getTransform = Selection.activeTransform;

            if (getTransform != null)
            {
                lastRoot = getTransform.transform.root;
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingPlayMode)
            {
                validateableMonobehaviours.Clear();
            }
        }

        // Transform is the root object
        private static Dictionary<Transform, HashSet<MonoBehaviour>> validateableMonobehaviours = new Dictionary<Transform, HashSet<MonoBehaviour>>();

        public static void Remove(MonoBehaviour monoBehaviour)
        {
            if (Application.isPlaying)
                return;

            if (validateableMonobehaviours.ContainsKey(monoBehaviour.transform.root))
            {
                if (validateableMonobehaviours.Count == 1)
                {
                    validateableMonobehaviours.Clear();
                }
                else
                {
                    validateableMonobehaviours[monoBehaviour.transform.root].Remove(monoBehaviour);
                }
            }
        }

        public static void Add(MonoBehaviour monoBehaviour)
        {
            // Check for prefabs, we don't want to add those.

            bool isPrefab = false;
#if UNITY_2018_3_OR_NEWER
            isPrefab = PrefabUtility.IsPartOfPrefabAsset(monoBehaviour.gameObject);
#else
            isPrefab = monoBehaviour.gameObject.scene.name == null;
#endif
            if (isPrefab)
                return;


            if (!validateableMonobehaviours.ContainsKey(monoBehaviour.transform.root))
            {
                validateableMonobehaviours.Add(monoBehaviour.transform.root, new HashSet<MonoBehaviour>() { monoBehaviour });
            }
            else
            {
                if (!validateableMonobehaviours[monoBehaviour.transform.root].Contains(monoBehaviour))
                {
                    validateableMonobehaviours[monoBehaviour.transform.root].Add(monoBehaviour);
                }
            }
        }

        // In case anything in the transform hierachy changes we want to check what object has been modified
        private static void OnHierarchyChanged()
        {
            if (Application.isPlaying)
                return;

            if (Selection.activeTransform != null)
            {
                CallValidation(Selection.activeTransform.root);
            }
            else
            {
                if (lastRoot != null && validateableMonobehaviours.ContainsKey(lastRoot))
                {
                    CallValidation(lastRoot);
                }
            }
        }

        // What happens here is that when an object gets removed, we check if the root is a Save Component
        // If that is the case, we send a OnValidate notification to ensure all references are in order.
        private static UndoPropertyModification[] OnPostProcessModifications(UndoPropertyModification[] modifications)
        {
            if (Application.isPlaying)
                return modifications;

            for (int i = 0; i < modifications.Length; i++)
            {
                if (modifications[i].currentValue.propertyPath == "m_RootOrder")
                {
                    Component valueTarget = modifications[i].currentValue.target as Component;

                    if (valueTarget != null && valueTarget.gameObject != null)
                    {
                        CallValidation(valueTarget.transform.root);
                    }

                    if (valueTarget.transform.root == valueTarget.transform)
                    {
                        CallValidation(lastRoot);
                    }
                }
            }

            return modifications;
        }

        // This will check the hierarchy from below to top if there is a subscribed gameobject
        private static void CallValidation(Transform target)
        {
            if (target == null)
                return;

            if (validateableMonobehaviours.ContainsKey(target))
            {
                foreach (MonoBehaviour item in validateableMonobehaviours[target])
                {
                    if (item != null)
                    {
                        MethodInfo tMethod = item.GetType().GetMethod("OnValidate");
                        if (tMethod != null)
                        {
                            tMethod.Invoke(item, null);
                            break;
                        }
                    }
                }
            }
        }
    }

}

#endif
