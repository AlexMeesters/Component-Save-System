/*
 * 
Copyright notice - Licence: MIT. https://opensource.org/licenses/MIT
Provided by Alex Meesters. www.alexmeesters.nl. Used within low-scope.com products.
Source: https://github.com/AlexMeesters/UnityValidateHierarchy

--- HOW TO USE? ---
On the component that you want have validation for:
Within the OnValidate method add: ValidateHierarchy.Add(this)
Within the OnDestroy method add: ValidateHierarchy.Remove(this)

Cheers! All the defines are added to prevent any building errors.
*/

using UnityEngine;

# if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using System.Reflection;

namespace Lowscope.Tools
{
    // This solution was required because there is no way to properly check if entire gameobject hierarchies are dirty.
    // This will do callbacks for subscribed objects when a property of an object with the same root GameObject has been modified.
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class ValidateHierarchy
    {
#if UNITY_EDITOR
        static ValidateHierarchy()
        {
            Undo.postprocessModifications += OnPostProcessModifications;

#if UNITY_2018_1_OR_NEWER
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
#else
            EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
#endif
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

#endif
        public static void Remove(MonoBehaviour monoBehaviour)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            if (validateableMonobehaviours.ContainsKey(monoBehaviour.transform.root))
            {
                validateableMonobehaviours[monoBehaviour.transform.root].Remove(monoBehaviour);

                if (validateableMonobehaviours[monoBehaviour.transform.root].Count == 0)
                {
                    validateableMonobehaviours.Remove(monoBehaviour.transform.root);
                }
            }
#endif
        }

        public static void Add(MonoBehaviour monoBehaviour)
        {
#if UNITY_EDITOR
            // Check for prefabs, we don't want to add those.

            bool isPrefab = false;
#if UNITY_2018_3_OR_NEWER
            isPrefab = PrefabUtility.GetCorrespondingObjectFromSource(monoBehaviour.gameObject) == null && PrefabUtility.GetPrefabInstanceHandle(monoBehaviour.gameObject) != null;
#elif UNITY_2018_1_OR_NEWER
            isPrefab = PrefabUtility.GetCorrespondingObjectFromSource(monoBehaviour.gameObject) == null && PrefabUtility.GetPrefabObject(monoBehaviour.gameObject) != null;
#else
            isPrefab = PrefabUtility.GetPrefabParent(monoBehaviour.gameObject) == null && PrefabUtility.GetPrefabObject(monoBehaviour.gameObject) != null;
#endif
            if (isPrefab)
                return;

            if (!validateableMonobehaviours.ContainsKey(monoBehaviour.transform.root))
            {
                validateableMonobehaviours.Add(monoBehaviour.transform.root, new HashSet<MonoBehaviour>());
            }

            validateableMonobehaviours[monoBehaviour.transform.root].Add(monoBehaviour);
#endif
        }
#if UNITY_EDITOR
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
                        MethodInfo tMethod = item.GetType().GetMethod("OnValidate",
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                        if (tMethod != null)
                        {
                            tMethod.Invoke(item, null);
                        }
                    }
                }
            }
        }
#endif
    }
}