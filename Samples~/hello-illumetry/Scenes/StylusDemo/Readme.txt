How it works:
Move the stylus pointer to the object and hold down the button on the stylus. 
Now you have grabbed the object and you can move it.

Please note how we implement the interaction between the stylus and the objects on the scene. 
When an object is grabbed by user, we split it into a rigid body and a visual part (basically, a MeshRenderer). 
The rigid body gets connected to the stylus with a fixed joint and is left for the Unity's physics engine to update. 
The visual part, however, gets reparented by the stylus game object. 
This way the grabbed object gets redrawn as soon as new tracking data available, hiding the otherwise noticeable lag between rendering and physics.

R - Cubes will be return to start pose. 