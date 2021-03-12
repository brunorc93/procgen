# ProcGen

Terrain Procedural Generation

Created in Unity3D using C#, unity's Texture2D, 3D Quads and Terrain features.

This project was created as separated modules able to run independently and those modules have been ported to C#.net. They can be seen in the following repos:

1. [Island Shape](https://github.com/brunorc93/islandShapeGen.net)  
1. [Biome Growth](https://github.com/brunorc93/BiomeGrowth.net)  
> more links will be added as soon as the modules are ported onto C#.net for ease of use.  

The project currently can only be visualized within Unity. It has 3 Scenes:
1. one for Island Shape generation that returns a Texture2D with the shape of the island  
1. one for visualizing Noise results (e.g.: fractal noise, simple Perlin noise, clamped noise, ridged noise)
1. and one that runs the entire project loading a pre produced shape and showing the generated 3D terrain in a second camera as well as a UI showing the names for each biome.
> full vizualization through this repo's README file will be added along the way.
