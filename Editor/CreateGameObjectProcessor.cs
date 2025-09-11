
using System;
using System.Linq;
using System.Reflection;
using FreakshowStudio.CreateGameObjectAttribute.Runtime;
using FreakshowStudio.MenuInternals.Editor;
using UnityEditor;
using UnityEditor.EventSystems;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace FreakshowStudio.CreateGameObjectAttribute.Editor
{
    [InitializeOnLoad]
    internal static class CreateGameObjectProcessor
    {
        static CreateGameObjectProcessor()
        {
            var types = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(t => t != null);
                    }
                })
                .Where(t =>
                {
                    if (!t.IsClass)
                    {
                        return false;
                    }

                    var hasAttribute =
                        t.GetCustomAttribute<CreateGameObjectMenuAttribute>()
                            != null;

                    return hasAttribute;
                })
                .Select(t =>
                    (
                        t,
                        t.GetCustomAttribute<CreateGameObjectMenuAttribute>()
                    )
                );

            foreach (var (type, attribute) in types)
            {
                var exec = new Action(() =>
                {
                    if (!typeof(Component).IsAssignableFrom(type))
                    {
                        Debug.LogError(
                            $"{type} is not a component and can't " +
                            "be added to a GameObject");

                        return;
                    }

                    var go = new GameObject(attribute.GameObjectName);
                    go.AddComponent(type);

                    foreach (var additionalType
                             in attribute.AdditionalComponents)
                    {
                        go.AddComponent(additionalType);
                    }

                    if (attribute.RequiresCanvas) SetUiElementParent(go);
                    else SetGameObjectParent(go);
                });

                var validate = new Func<bool>(() => true);

                MenuInternal.AddMenuItem(
                    "GameObject/" + attribute.MenuName,
                    attribute.Priority,
                    exec,
                    validate);
            }
        }

        private static void SetGameObjectParent(
            GameObject go)
        {
            var parent = Selection.activeGameObject;

            if (parent == null)
            {
                StageUtility.PlaceGameObjectInCurrentStage(go);
                return;
            }

            var isCurrentStage =
                StageUtility.GetStageHandle(parent) ==
                    StageUtility.GetCurrentStageHandle();

            if (isCurrentStage)
            {
                GameObjectUtility.SetParentAndAlign(go, parent);
            }
            else
            {
                StageUtility.PlaceGameObjectInCurrentStage(go);
            }
        }

        private static void SetUiElementParent(
            GameObject element)
        {
            // Implementation based on the internal
            // UnityEditor.UI.MenuOptions class
            var parent = Selection.activeGameObject;
            bool explicitParentChoice = true;

            if (parent == null)
            {
                parent = GetOrCreateCanvasGameObject();
                explicitParentChoice = false;

                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                if (prefabStage != null &&
                    !prefabStage.IsPartOfPrefabContents(parent))
                {
                    parent = prefabStage.prefabContentsRoot;
                }
            }

            if (parent.GetComponentsInParent<Canvas>(true).Length == 0)
            {
                var canvas = CreateNewCanvas();

                Undo.SetTransformParent(
                    canvas.transform,
                    parent.transform,
                    string.Empty);

                parent = canvas;
            }

            GameObjectUtility.EnsureUniqueNameForSibling(element);

            SetParentAndAlign(element, parent);

            if (!explicitParentChoice)
            {
                SetPositionVisibleInSceneView(
                    parent.GetComponent<RectTransform>(),
                    element.GetComponent<RectTransform>());
            }

            Undo.RegisterFullObjectHierarchyUndo(
                parent == null ? element : parent,
                string.Empty);

            Undo.SetCurrentGroupName("Create " + element.name);

            Selection.activeGameObject = element;
        }

        private static void SetPositionVisibleInSceneView(
            RectTransform canvasTransform,
            RectTransform itemTransform)
        {
            var sceneView = SceneView.lastActiveSceneView;

            if (sceneView == null ||
                sceneView.camera == null)
            {
                return;
            }

            var camera = sceneView.camera;

            var position = AnchoredPosition(
                canvasTransform,
                itemTransform,
                camera);

            itemTransform.anchoredPosition = position;
            itemTransform.localRotation = Quaternion.identity;
            itemTransform.localScale = Vector3.one;
        }

        private static Vector3 AnchoredPosition(
            RectTransform canvasTransform,
            RectTransform itemTransform,
            Camera camera)
        {

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasTransform,
                    new Vector2(
                        camera.pixelWidth / 2f,
                        camera.pixelHeight / 2f),
                    camera,
                    out var localPlanePosition))
            {
                return Vector3.zero;
            }

            var canvasDeltaX = canvasTransform.sizeDelta.x;
            var canvasDeltaY = canvasTransform.sizeDelta.y;

            var itemDeltaX = itemTransform.sizeDelta.x;
            var itemDeltaY = itemTransform.sizeDelta.y;

            var itemAnchorMinX = itemTransform.anchorMin.x;
            var itemAnchorMinY = itemTransform.anchorMin.y;

            var itemPivotX = itemTransform.pivot.x;
            var itemPivotY = itemTransform.pivot.y;

            var canvasPivotX = canvasTransform.pivot.x;
            var canvasPivotY = canvasTransform.pivot.y;

            var localX = localPlanePosition.x + canvasDeltaX * canvasPivotX;
            var localY = localPlanePosition.y + canvasDeltaY * canvasPivotY;

            var planeX = Mathf.Clamp(localX, 0, canvasDeltaX);
            var planeY = Mathf.Clamp(localY, 0, canvasDeltaY);

            var x = Mathf.Clamp(
                planeX - canvasDeltaX * itemAnchorMinX,
                canvasDeltaX * (0 - canvasPivotX) + itemDeltaX * itemPivotX,
                canvasDeltaX * (1 - canvasPivotX) - itemDeltaX * itemPivotX);

            var y = Mathf.Clamp(
                planeY - canvasDeltaY * itemAnchorMinY,
                canvasDeltaY * (0 - canvasPivotY) + itemDeltaY * itemPivotY,
                canvasDeltaY * (1 - canvasPivotY) - itemDeltaY * itemPivotY);

            return new Vector3(x, y, 0);
        }

        private static GameObject GetOrCreateCanvasGameObject()
        {
            var selectedGo = Selection.activeGameObject;

            var canvas = (selectedGo != null)
                ? selectedGo.GetComponentInParent<Canvas>()
                : null;

            if (IsValidCanvas(canvas))
            {
                return canvas!.gameObject;
            }

            var canvasArray = StageUtility
                .GetCurrentStageHandle()
                .FindComponentsOfType<Canvas>();

            foreach (var c in canvasArray)
            {
                if (IsValidCanvas(c))
                {
                    return c.gameObject;
                }
            }

            return CreateNewCanvas();
        }

        static bool IsValidCanvas(Canvas? canvas)
        {
            if (canvas == null ||
                !canvas.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (EditorUtility.IsPersistent(canvas) ||
                (canvas.hideFlags & HideFlags.HideInHierarchy) != 0)
            {
                return false;
            }

            return
                StageUtility.GetStageHandle(canvas.gameObject) ==
                    StageUtility.GetCurrentStageHandle();
        }

        private static GameObject CreateNewCanvas()
        {
            var root = ObjectFactory.CreateGameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            root.layer = LayerMask.NameToLayer("UI");
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            StageUtility.PlaceGameObjectInCurrentStage(root);
            var customScene = false;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage != null)
            {
                Undo.SetTransformParent(
                    root.transform,
                    prefabStage.prefabContentsRoot.transform,
                    "");

                customScene = true;
            }

            Undo.SetCurrentGroupName("Create " + root.name);

            if (!customScene)
            {
                CreateEventSystem(false, null);
            }

            return root;
        }

        private static void CreateEventSystem(
            bool select,
            GameObject? parent)
        {
            var stage = parent == null
                ? StageUtility.GetCurrentStageHandle()
                : StageUtility.GetStageHandle(parent);

            var esys = stage.FindComponentOfType<EventSystem>();

            if (esys == null)
            {
                var eventSystem = ObjectFactory
                    .CreateGameObject("EventSystem");

                if (parent == null)
                {
                    StageUtility.PlaceGameObjectInCurrentStage(eventSystem);
                }
                else
                {
                    SetParentAndAlign(eventSystem, parent);
                }

                esys = ObjectFactory.AddComponent<EventSystem>(eventSystem);
                InputModuleComponentFactory.AddInputModule(eventSystem);

                Undo.RegisterCreatedObjectUndo(
                    eventSystem,
                    "Create " + eventSystem.name);
            }

            if (select && esys != null)
            {
                Selection.activeGameObject = esys.gameObject;
            }
        }

        private static void SetParentAndAlign(
            GameObject child,
            GameObject parent)
        {
            if (parent == null) return;

            Undo.SetTransformParent(child.transform, parent.transform, "");

            var rectTransform = child.transform as RectTransform;

            if (rectTransform)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                var localPosition = rectTransform.localPosition;
                localPosition.z = 0;
                rectTransform.localPosition = localPosition;
            }
            else
            {
                child.transform.localPosition = Vector3.zero;
            }

            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            SetLayerRecursively(child, parent.layer);
        }

        private static void SetLayerRecursively(
            GameObject go,
            int layer)
        {
            go.layer = layer;
            var t = go.transform;

            for (int i = 0; i < t.childCount; i++)
            {
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
            }
        }

    }
}
