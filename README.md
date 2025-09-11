# Create GameObject Attribute

An attribute (`[CreateGameObjectMenu]`) for automatically creating a 
menu item to create a game object with the component added.

Add the attribute to any class that implements `Component`
(typically `MonoBehaviour`) and it will be added to the GameObject menu.

Use the parameters to specify the name of the game object, and the menu
path ("GameObject" is automatically prepended). The priority is used to
order the menu item in the list, a lower number puts it higher in the
list. 

The "Require Canvas" parameter can be used to automatically parent the
game object to a canvas. This is useful for UI-related game objects.

You can also specify additional components to add to the game object.

The class that you put the attribute on must be a `Component`
(or a subclass of it, like `MonoBehaviour`), and so must all the
components you specify in the `AdditionalComponents` parameter.
