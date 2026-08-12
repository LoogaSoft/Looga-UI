using System.Collections.Generic;
using LoogaSoft.UI.Extensions;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.UI.Extensions.Editor
{
    [InitializeOnLoad]
    internal static class LoogaUIShadowCleanupUtility
    {
        static bool _cleanupQueued;

        static LoogaUIShadowCleanupUtility()
        {
            LoogaUIShadowEditorBridge.RegisterCreatedObjectUndo = RegisterCreatedObjectUndo;
            Undo.undoRedoPerformed += QueueCleanup;
            EditorApplication.hierarchyChanged += QueueCleanup;
            AssemblyReloadEvents.beforeAssemblyReload += ClearEditorBridge;
            EditorApplication.delayCall += QueueCleanup;
        }

        static void RegisterCreatedObjectUndo(GameObject shadowObject)
        {
            Undo.RegisterCreatedObjectUndo(shadowObject, "Create Looga UI Shadow Renderer");
        }

        static void ClearEditorBridge()
        {
            LoogaUIShadowEditorBridge.RegisterCreatedObjectUndo = null;
        }

        [MenuItem("LoogaSoft/Looga UI/Cleanup Generated Shadows")]
        static void CleanupFromMenu()
        {
            int removed = CleanupGeneratedRenderers(rebuildAll: true);
            Debug.Log($"Looga UI removed {removed} generated shadow renderer(s) and rebuilt active shadows.");
        }

        static void QueueCleanup()
        {
            if (_cleanupQueued)
            {
                return;
            }

            _cleanupQueued = true;
            EditorApplication.delayCall += RunQueuedCleanup;
        }

        static void RunQueuedCleanup()
        {
            _cleanupQueued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            CleanupGeneratedRenderers(rebuildAll: false);
        }

        static int CleanupGeneratedRenderers(bool rebuildAll)
        {
            LoogaUIShadow[] shadows = Resources.FindObjectsOfTypeAll<LoogaUIShadow>();
            HashSet<int> validOwnerIds = new();
            HashSet<int> claimedOwnerIds = new();
            foreach (LoogaUIShadow shadow in shadows)
            {
                if (IsEditableSceneObject(shadow))
                {
                    validOwnerIds.Add(shadow.GetInstanceID());
                }
            }

            int removed = 0;
            LoogaUIShadowRendererOwner[] ownedRenderers = Resources.FindObjectsOfTypeAll<LoogaUIShadowRendererOwner>();
            foreach (LoogaUIShadowRendererOwner renderer in ownedRenderers)
            {
                if (!IsEditableSceneObject(renderer))
                {
                    continue;
                }

                LoogaUIShadow owner = renderer.Owner;
                if (!rebuildAll &&
                    owner != null &&
                    validOwnerIds.Contains(owner.GetInstanceID()) &&
                    claimedOwnerIds.Add(owner.GetInstanceID()))
                {
                    continue;
                }

                Object.DestroyImmediate(renderer.gameObject);
                removed++;
            }

            GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject sceneObject in sceneObjects)
            {
                if (!IsEditableSceneObject(sceneObject) ||
                    sceneObject.name != LoogaUIShadow.ShadowObjectName ||
                    (sceneObject.hideFlags & HideFlags.HideAndDontSave) == 0 ||
                    sceneObject.GetComponent<LoogaUIShadowRendererOwner>() != null)
                {
                    continue;
                }

                Object.DestroyImmediate(sceneObject);
                removed++;
            }

            if (rebuildAll)
            {
                foreach (LoogaUIShadow shadow in shadows)
                {
                    if (!IsEditableSceneObject(shadow))
                    {
                        continue;
                    }

                    shadow.MarkDirty();
                }

                SceneView.RepaintAll();
            }

            return removed;
        }

        static bool IsEditableSceneObject(Component component)
        {
            return component != null && IsEditableSceneObject(component.gameObject);
        }

        static bool IsEditableSceneObject(GameObject gameObject)
        {
            return gameObject != null &&
                   gameObject.scene.IsValid() &&
                   gameObject.scene.isLoaded &&
                   !EditorUtility.IsPersistent(gameObject);
        }
    }
}
