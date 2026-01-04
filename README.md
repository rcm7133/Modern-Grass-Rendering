# Modern-Grass-Rendering

<img width="1892" height="1064" alt="Screenshot 2026-01-03 161435" src="https://github.com/user-attachments/assets/14e0eab0-a652-4ed3-b965-ce431db84703" />

Realtime rendering of over 120,000 GPU instanced grass blade meshes with distance based LOD and GPU Frustum Culling.

# How Does it Work?

A tried and true method for creating realtime grass for many decades has been billboarded grass. This method involves intersecting multiple planes with a transparent grass texture to give the illusion of dense grass with minimal performance cost. The planes have backface culling disabled to allow for the appearance of higher density. Here is an example I've made:

<img width="1015" height="1036" alt="Screenshot 2026-01-03 152929" src="https://github.com/user-attachments/assets/4bbed102-1f13-4654-b1bf-4b4b60434f8b" />

This approach is performant, but has many issues. When viewed from above the illusion is broken and the planes become very obvious. Because each plane has only four vertices there is very little data to work with when it comes to animating the sway of grass in the vertex shader. What can be done to improve the look of our grass?

## GPU Instanced Mesh Grass Blades

We need a means of generating grass world space positions as a Vector3 on the generated terrain.

<img width="1215" height="937" alt="grass1" src="https://github.com/user-attachments/assets/4e4365b9-90bd-4089-a561-03000d8a43b1" />

The terrain determines the height of each vertex by sampling its height map at a given UV coordinate. If we feed that same height map into a compute shader we can very quickly generate a buffer of grass positons in a grid pattern on the GPU. The density of the grass will determine how many grass blades will be made.

<img width="1066" height="950" alt="grass2" src="https://github.com/user-attachments/assets/ac7af640-b098-479a-aeff-e63e3a4f225c" />

The image above shows the compute shader generated grass positions on the ground mesh. Now we have the positions to put our grass on in a grid pattern. Now we a grass mesh to place on each positon. In this case we will use a custom mesh that has 11 verts and 9 tris:

<img width="607" height="716" alt="highLod" src="https://github.com/user-attachments/assets/18456701-8841-486a-98e3-f8c12b361318" />

Now we need to place this mesh on each of the postions we generated. One approach would be to instantiate a game object with the grass mesh on each of the postions. This would be absolutely disasterous. The CPU is not meant to handle this many objects, especially if we want to animate them later. What we will do instead is use GPU Instancing so we feed the mesh to the GPU once per frame and have the GPU draw each blade. To do this we will use Unity's DrawMeshInstancedIndirect function. We will also apply a material to the grass so we can apply color and later animate the blades using the vertex shader. This gives us:

<img width="831" height="787" alt="grass3" src="https://github.com/user-attachments/assets/29fadbc2-61f8-4cc0-a2e5-ae6212f0d505" />

While this doesn't look appealing yet, it provides the basic concept for us to refine. Increasing the density gives us:

<img width="847" height="963" alt="grass4" src="https://github.com/user-attachments/assets/3b48c6e8-dd66-477f-8308-7f7fad39e26c" />

Naturally, increasing the density makes the grass looks far better with the drawback of performance loss as the GPU needs to do more work. 

## Visual Improvements

There are many things that can be done to improve the look of the grass. Right now the grass positions are far too uniform. Applying a noise texture to the XZ coordinates of the generated grass postions will break up the uniformity. In this case we will use Perlin Noise:

<img width="659" height="662" alt="perlinNoise" src="https://github.com/user-attachments/assets/5193543a-d7f0-41b0-9987-70b95a7eb203" />

We can also apply the same noise to the height of the grass to give them random lengths. Applying the noise to the grass positions and heigt gives us:

<img width="1011" height="853" alt="grass5" src="https://github.com/user-attachments/assets/32593693-bfcf-4573-80f8-ef1414bc3d46" />
<img width="890" height="889" alt="grass7" src="https://github.com/user-attachments/assets/affe08b4-3787-439d-ac91-1d9035487c69" />

Another problem with the grass is that they only face one direction:

<img width="842" height="891" alt="Screenshot 2025-12-17 225900" src="https://github.com/user-attachments/assets/d1505696-71b5-4f74-b4b7-6b20bfc98de4" />

We need to apply billboarding to the grass in the vertex shader so the grass will always face the camera. To do this we need to calculate the direction from the grass to the camera and take the cross product with the world up vector (0, 1, 0). This will give us a vector that points to 90 degrees right of the camera facing. Then we take the cross product of the right vector and the world up vector tro give us the direction the grass needs to face. We apply the transformation to make the grass always look at the camera. 

<img width="995" height="1132" alt="Screenshot 2025-12-29 103851" src="https://github.com/user-attachments/assets/4006199f-ef8b-4883-bfb6-b160937a187c" />

We can now animate the grass to make it appear to sway in the wind. To do this we need to create a compute shader that will write a wind sway pattern to a render texture. We will then sample this render texture in the grass vertex shader to displace each vertex of the grass. To create the texture we will use an oscillator function such as Sine or Cosine function. In my case I used amplitude * sin(id.xy * frequency + time * frequency * speed). This allows the sway texture to be finely controlled. Then we apply the sway amount based off how far it is from the base of the grass. The higher the vertex is on the grass mesh the more the sway will be applied. Here is a visual of the sway texture being sampled in the vertex shader on the grass:

![Desktop 2025 12 21 - 14 38 14 01 (online-video-cutter com)](https://github.com/user-attachments/assets/0059ce40-2883-4bff-8f30-d66956ee37ca)

Now we need better colors. We can control the color of the grass in the grass fragment shader. We will have different colors for different sections of the grass. The very bottom will be the Ambient Occlusion section which will mimic the look of shadows near the base of the grass. Then there will be two middle sections and a tip color. This is what they look like on the grass:

<img width="842" height="898" alt="grass8" src="https://github.com/user-attachments/assets/44309d51-6afb-438d-8435-0991cd442bd6" />

If we blend bewtween these colors in the fragment shader we get:

<img width="782" height="705" alt="grass9" src="https://github.com/user-attachments/assets/889e895e-da00-4154-966b-4bea9a306eb1" />

This approach allows us to set the grass to any combination of colors we desire.

## GPU Memory

Currently the grass struct that our GPU buffer is composed of a Float3 for the positon, a float for the height if the grass, and a float2 for the worldUV. This means that we are allocating 24 bytes per grass blade into our compute buffer on the GPU. If we have around 120,000 grass blades we are allocating around 2.9 MB of GPU memory for the grass. This is very litte memory for how dense the grass is. However we would like to improve the performance by reducing the amound of work the GPU has to do per frame. This will come with the trade off of needing to allocate more GPU memory.

## Optimizations

There are many things we can do to improve the performance of our grass. We can apply Level Of Detail (LOD) to our grass. We are wasting a lot of GPU compute time by rendering relatively high detail grass blades and vertex animations for grass that is far away. At this distance the high details of the grass is hardly noticeable. What if at a certain distance from the camera we swapped the High Level of Detail (HLOD) model for a Low Level of Detail (LLOD) model to reduce the number of tris and verts we are rendering? To do this we need to filter each grass positon into one of two new buffers based off the distance the LOD cutoff begins. We will make a new compute shader to sort each grass position into a HLOD and LLOD buffer. We will then draw the postions in the LLOD buffer with a LLOD mesh and the positions in the HLOD buffer with the old HLOD mesh. 

Here are the two meshes, the HLOD is the same as the mesh we were using before. The LLOD is simpler with 7 verts and 5 tris.
HLOD:
<img width="607" height="716" alt="Screenshot 2025-12-20 161507" src="https://github.com/user-attachments/assets/51098eb1-e216-4a87-9b74-b0e6fdfad439" />
LLOD:
<img width="418" height="698" alt="Screenshot 2025-12-20 161616" src="https://github.com/user-attachments/assets/b90234fa-2b6a-48e3-bf01-811f85d4a8c4" />

Here is the grass before the LOD is applied:

![New Folder (3) 2025 12 21 - 14 48 50 02 (online-video-cutter com) (2)](https://github.com/user-attachments/assets/bd269d09-db02-4229-bfd9-388dc2c7e37c)

And here is the grass with LOD applied. The LLOD grass is colored red and the HLOD grass is green.

![New Folder (3) 2025 12 29 - 00 05 36 01 (online-video-cutter com)](https://github.com/user-attachments/assets/203f74e8-bddc-4f09-9e10-4bedab05e855)

By doing this we have created two new buffers that need to be allocated. This has trippled the number of bytes needed per grass from 24 to 72. There is likely a much better way to do this, but even so with a 120,000 grass blades as shown the memory usage is only around 8.64 MB which is very small on modern GPUs. What we get from higher memory usage is around a **40% increase in performance**. We have dramatically reduced the number of tris and verts we have to process with almost zero change in graphical fidelity.

Another problem is we are sending grass positions to the GPU that are not in view of the camera. This wastes GPU resources on grass that the player can't even see. The camera uses a shape called a frustum to determine the final output of the image displayed on the screen. Any object in this shape is sent to the GPU to be drawn and is ignored (or culled) if it is not within the camera frustum.

<img width="600" height="281" alt="VisualCameraFrustum" src="https://github.com/user-attachments/assets/99b04981-fb16-4078-b82b-1243ea80706a" />
Credit: https://learnopengl.com/Guest-Articles/2021/Scene/Frustum-Culling

Because our grass is realistically just a buffer of positions we will have to make this culling effect for ourselves. Before dispaching the LOD compute shader, we will feed it the six planes that make up the camera frustum shape using the GeometryUtility.CalculateFrustumPlanes function. We will represent these planes as a float4 in the compute shader. Then in the compute shader we will check the grass postition against each plane using dist = dot(frustumPlane.xyz, grass.position) + frustumPlane.w. This will get the distance from the plane to the grass position. Then we need to declare a check sphere radius that will determine how large to approximate the size of the grass. We will check this distance to see if it's less than -1 * checkRadius. If that condition is true for any of the planes then the grass is out of view and work on the grass is abandoned. If it is visible then resume LOD calculations.

Before applying Frustum Culling:

![ezgif-887fd4832f883ec1](https://github.com/user-attachments/assets/a75fcd17-cde3-4e45-bd9f-05546190ebb8)

After applying Frustum Culling:

![ezgif-4251f396822ec036](https://github.com/user-attachments/assets/aa00548b-9481-4b52-9296-02e294f86fba)

This reduces the number of meshes the GPU is being told to draw, and it this case results in ~10% boost in performance.

# Credits
https://www.youtube.com/watch?v=Ibe1JBF5i5Y&
https://www.youtube.com/watch?v=Y0Ko0kvwfgA
https://www.youtube.com/watch?v=jw00MbIJcrk
https://learnopengl.com/Guest-Articles/2021/Scene/Frustum-Culling



