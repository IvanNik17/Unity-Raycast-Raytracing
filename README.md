# Unity-Raycast-Raytracing
A prototype unity application for testing if raycasts can be used to calculate a raytracing path from a light source to a 3D reconstructed object. The light source in the Unity environment is positioned and rotated the same way as the real world equivalent. This method can be used to see if parts of the object have not received not enough light, which would result in a Structure from Motion reconstruction with noise and geometrical defects.

The real structure from motion capturing was made in the lab, that's modeled as part of the prototype. In principal the prototype can be used with other custom spaces - the only thing that needs to change is the position and number of light source raycaster objects and setup the amout of bounces in the environment. Each of the objects that in the environment that will receive raycasts needs to have a collider.

To run the prototype Unity 2019.3.7f1 or higher is required. 

# Controls

1. Pressing the Z button visualizes a sparse raytracer in the selected environment. This is only for visualization purposes and the amount of shown raycast bounces can be changed to make it less cluttered
2. Pressing the A button runs a full raytracer, which counts the hits on the surface on the main 3D mesh, as well the amount of bounces before the surface is hit. The running time of this part of the application highly depends on the set number of bounces. For three environmental bounces for 100 rays it takes around 35 sec to calculate.
3. Pressing the S button save the ray hits for each vertex and face on the selected object to a .txt file for later analysis


![Gameplay Gif](GameImages/RayCastRaytracer.gif)
