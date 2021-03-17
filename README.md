# ProcGen

Terrain Procedural Generation

Created in Unity3D using C#, unity's Texture2D, 3D Quads and Terrain features.

This project was created as separated modules able to run independently and those modules have been ported to C#.net. They can be seen in the following repos:

1. [Island Shape](https://github.com/brunorc93/islandShapeGen.net)  
1. [Biome Growth](https://github.com/brunorc93/BiomeGrowth.net)  
3. [Noise - next](https://github.com/brunorc93/noise)  
> more links will be added as soon as the modules are ported onto C#.net.  

The project currently can only be visualized within Unity. It has 3 Scenes:
1. Island Shape generation that returns a Texture2D with the shape of the island  
1. Visualizing Noise results (e.g.: fractal noise, simple Perlin noise, clamped noise, ridged noise)
1. Scene loading a pre produced shape and showing the generated 3D terrain in a second camera as well as a UI showing the names for each biome.
> the full project might take a while (5 to 10 minutes on my old ass laptop) to run and generate the terrains due to calculating distances from each point in the island to its outline and this step hasn't been optimized yet.

> full visualization through this repo's README file will be added along the way.
