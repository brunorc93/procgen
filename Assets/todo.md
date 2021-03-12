# GENERAL:  
> transfrom SetPixel into SetPixels wherever it is possible

> use color_[] instead of Texture2D whenever possible, making it easier to use threading

> USE THREADING; REWRITE ALL CODE SO THAT IT DOESNT DEPEND ON UNITY API

> things to look into:  

>> c# structs  

>> c# class within class  

>> maybe order one use funcitons in order of use  

>> multithreading:
>>> https://www.jacksondunstan.com/articles/3746  
>>> https://entitycrisis.blogspot.com/2012/08/really-really-easy-multithreading-in.html  
>>> https://unitycoder.com/blog/2016/09/17/easy-threading-with-threadpool/  
>>> https://answers.unity.com/questions/357033/unity3d-and-c-coroutines-vs-threading.html  

>> mesh boolean operations  

>> selecting which chunks to render  
<!-- 
>> LOD  

>> mesh slicing  

>> mesh simplification  

>> removing unused vertices   -->

>> change small ints into shorts  

> make methods load the required variables when possible and return the results instead of loading nothing and returning void  

> normalize common methods into Helper class; eg.: CallDebug, RenderFinal, Showcase
# Helper.cs:
> create:
>> GetDiagonalNeighbours(point)  
>> GetCrossNeighbours(point)  
>> GetWeightedNeighbours(point,b,c) -> return a dictionary maybe?  

# TerrainGenerator.cs:
> revisit removing bias   

>> CIRCLES BEEIG GENERATED IN WEIRD OCTAGONAL SHAPES!  

>> sinusoidal circles wont solve this issue as the grid is the main problem (as well as small texture size when generating it);  

>> bezier curves might be nice  

>> Generating Voronoi Centroids in an spiral pattern (specially a Bounded spiral) where the point generated is not exactly in spiral but near it with a small random fluctuation in its r and theta values. Also making the spiral elliptical according to iage dimensions might be usefull for non square textures.  

> fix FPS going way to low on some steps (as low as 0.4fps) -> steps 10+ -> subdivide steps using the if(auxValue) loop  NAH -> use a Coroutine!  
>> steps that need optimization: 14 (75%), 11 (11%), 1 (4%), 6 (2%), 5(1%); % of time cosumed
# BiomesGenerator.cs:
> maybe harbour adjacent to village? -> add harbour after biome generation by switching a random VillageXXX-CoastXXX biome into VillageXXX-HarbourXXX  

> take a look at brazillian biomes such as: arquipelago de abrolhos, lençois maranhenses, cangaço  

>>transfer Generate GUI into UIMan somehow!!! so that heightmapgen and others may use it  

> fix FPS going way to low on some steps
# HeightMapGenerator.cs:
>  

> Maybe try scaling less the island (8x instead of 16x, or just 4x)  
> Also try to get the outlines and then the outlines +1, outlines +2, until outline +n instead of getting the distance to the outlines for every single point (maybe n = 10 as a start);
> change the outlines back to outlines[colors.length][size,size];
> outlines = 0 -> outlines; outlines = 1: 1 block away from outline block ...
> same goes for shorelines
> check what happens at chunk borders (because of Helper.Neighbours method)
> if needed make a Helper.AllChunkNeighbours that considers i<0 and i>=size

>
>  

> fix FPS going way to low on some steps
# PathGenerator.cs:
> /* Algorithm  

> Djikstra Algorithm

> Anisotropic Grid + Weighted Shortest Path Algorithm
# TGDataCollector.cs:
>
# MeshGenerator.cs: 
> look into Delaunay triangulation, maybe even 3D Delaunay Triangulation  

> it is also known as Doleno Triangulation  

> maybe now Voronoi Centroids might be usefull...  

> generate mesh   

> separate mesh into chunks  
# TerrainShader.??:
> textures for each region/biome based on altitude, steepness, ???  
# TGSnapShooter.cs:
>
# BGSnapShooter.cs:
>