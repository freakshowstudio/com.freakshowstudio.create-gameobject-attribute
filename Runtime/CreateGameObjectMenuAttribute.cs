
using System;


namespace FreakshowStudio.CreateGameObjectAttribute.Runtime
{
    /// <summary>
    /// An attribute that registers a MonoBehaviour for registration in the
    /// GameObject creation menu.
    /// </summary>
    /// <remarks>
    /// This attribute is used to define the metadata needed to create
    /// a GameObject via a custom menu entry in the Unity Editor's
    /// "GameObject" menu. When applied to a MonoBehaviour, it enables
    /// Unity to display a new menu item for creating the specified
    /// GameObject with its associated configuration.
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class,
        Inherited = false
    )]
    public class CreateGameObjectMenuAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the GameObject to be created when using the custom
        /// menu entry in the Unity Editor's "GameObject" menu.
        /// </summary>
        public string GameObjectName { get; private set; }

        /// <summary>
        /// Gets the name of the menu entry to be displayed in the
        /// Unity Editor's "GameObject" menu for creating a
        /// custom GameObject.
        /// </summary>
        public string MenuName { get; private set; }

        /// <summary>
        /// Gets the priority of the menu item in Unity Editor's
        /// "GameObject" menu.
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// Indicates whether the GameObject being created requires a
        /// Canvas as a parent.
        /// </summary>
        /// <remarks>
        /// If set to true, the GameObject created through the custom menu
        /// will be automatically parented to a Canvas. This is useful
        /// for UI-related GameObjects. If false, the GameObject will be
        /// parented to whatever is currently selected.
        /// </remarks>
        public bool RequiresCanvas { get; private set; }

        /// <summary>
        /// Gets the list of additional component types to be automatically
        /// added to the GameObject when it is created via the custom
        /// menu entry in the Unity Editor's "GameObject" menu.
        /// </summary>
        /// <remarks>
        /// Each type in this list must inherit from Unity's Component class.
        /// These components are added to the newly created GameObject
        /// during the creation process.
        /// </remarks>
        public Type[] AdditionalComponents { get; private set; }

        /// <summary>
        /// Attribute used to simplify the creation of GameObjects
        /// through a menu within the Unity Editor. Enables the addition
        /// of contextual commands to create GameObjects with predefined
        /// properties, components, and settings directly from the editor
        /// menu.
        /// </summary>
        /// <param name="gameObjectName">
        /// The name of the GameObject to be created.
        /// </param>
        /// <param name="menuName">
        /// The name (path) of the menu item. "GameObject" is automatically
        /// prepended to the path. Use forward slash (/) for nesting.
        /// </param>
        /// <param name="priority">
        /// The priority of the menu item. Lower values place the item
        /// higher in the menu.
        /// </param>
        /// <param name="requiresCanvas">
        /// Whether the GameObject requires a Canvas component. If this is
        /// true (for uGUI elements), the GameObject will always be created
        /// under a canvas (if one doesn't exist, one will be created).
        /// </param>
        /// <param name="additionalComponents">
        /// Additional components to add to the GameObject.
        /// </param>
        public CreateGameObjectMenuAttribute(
            string gameObjectName,
            string menuName,
            int priority = 0,
            bool requiresCanvas = false,
            params Type[] additionalComponents)
        {
            GameObjectName = gameObjectName;
            MenuName = menuName;
            Priority = priority;
            RequiresCanvas = requiresCanvas;
            AdditionalComponents = additionalComponents;
        }
    }
}
