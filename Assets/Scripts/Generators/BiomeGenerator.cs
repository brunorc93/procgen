using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class BiomeGenerator : MonoBehaviour
{
    //---------- common vars --------public----
    public bool debugging = false;
    public bool useDebugUI = true;
    public bool useSeed = false;
    public int randomSeed = 42;
    public bool showcase = true;
    public bool renderBool = true;
    public bool load_rnd = true;
    public int load_N = 0;
    public bool _save = true;
    public bool _continue = true;
    //--------------------------gameObjects----
    [HideInInspector]
    public UIManager UIMan;
    //---------------------hidden-&-private----
    [HideInInspector]
    public bool dataCollect = false, 
    destroyOnEnd = true,
    finished = false;
    [HideInInspector]
    public int auxValue = 0,
    generation_phase = 0,
    roundNumber = 0,
    size = 0,
    updateCount = 0;
    [HideInInspector]
    public Texture2D shape_bmp;
    [HideInInspector]
    public Color[,] colors; // indexed as y,x
    //--- end of common vars ------------------

    // HERE - organize this bunch of variables
    // private bool loadedTG = false;
    private bool UIdone = false;

    private bool nodesChosen = false;

    // this could be a class/struct
    private List<string> inlandNodesStringList = new List<string>();
    private List<Vector2Int> inlandNodesV2List = new List<Vector2Int>();

    // this could be a class/struct
    private int[] zonesRValueArray = null;
    private int[] zonesAreaArray = null;
    private List<List<Vector2Int>> zonesV2List = new List<List<Vector2Int>>();

    private Vector2[] perlinPoint = null;
    private float[][] gradients = null;
    private bool perlinDone = true;

    // this could be a class/struct
    private List<string> coastalNodesStringList = new List<string>();
    private List<int> coastalNodesBlueValueList = new List<int>();
    private List<Vector2Int> coastalNodesV2List = new List<Vector2Int>();

    private Vector2Int startingCoastalWalkPoint = new Vector2Int(0,0);

    private int coastSize;

    // this could be a class/struct
    private List<Vector3Int> fullZonesRGBValues = new List<Vector3Int>();
    private List<string> fullZonesNames = new List<string>(); 
    private List<int> fullZonesArea = new List<int>();
    private List<List<Vector2Int>> fullZonesV2List = new List<List<Vector2Int>>();

    private bool[,] holes; // indexed as y,x ; true for surface, false for hole
    private float[,] heights; // indexed as y,x
    private float[,] distToBorder; // indexed as y,x
    private float[,] distToShore; // indexed as y,x
    private float[,] distToSide; // indexed as y,x

    private Texture2D holes_bmp;
    private Texture2D heights_bmp;
    private Texture2D distToBorder_bmp;
    private Texture2D distToShore_bmp;
    private Texture2D distToSide_bmp;

    private List<Vector2Int>[] outlines;
    private List<Vector2Int> shoreline;
    private List<Vector2Int>[] sidelines;
    
    private GameObject _terrain;
    private int maxHeight = 250;
    
    private float maxDistToShore; // here - rename this to globalMaxDistToShore
    // private float[] maxDistToShore // here - create this and use it
    private Vector2[] initialPoint;
    
    private float[] maxDistToBorder;
    private float[] averageHeight;
    private int[] pointCount;

// main ---------------------------------------

    void _Start()
    { // function called in the first update of the script
      if (useSeed) { Random.InitState(randomSeed); }
      Load(load_N,load_rnd);
    }

    void Update()
    {
      if (!finished)
      {
        updateCount++;
        switch (generation_phase)
        {
          case 0: // Creating TG
            if (auxValue == 0) { UIMan = GameObject.Find("/UI/Canvas").GetComponent<UIManager>(); }
            CallDebug("Starting Biome Generator");
            _Start();
            generation_phase++;
            break;
          case 1:  // Setting up renderer
            CallDebug("Setting renderer");
            SetupRenderer();
            generation_phase++;
            Showcase();
            break;
          case 2:  // Clear shape_bmp
            CallDebug("Clearing shape_bmp colors");
            shape_bmp = shape_bmp.ResetColors();
            generation_phase++;
            Showcase();
            break;
          case 3:  // Choosing Nodes
            CallDebug("Choosing Biome Nodes from list of biomes");
            if (!nodesChosen)
            {
              ChooseNodes();
              nodesChosen = true;
            }
            generation_phase++;
            break;
          case 4:  // Placing Coastal Nodes in island
            CallDebug("Placing Coastline Nodes");
            SetCoastToStartPlacing_part1();
            SetStartingCoastalWalkPoint();
            SetCoastToStartPlacing_part2();
            CleanCoastalLeftovers();
            PlaceCoastalNodes();
            generation_phase++;
            Showcase();
            break;
          case 5:  // Expanding Coastal Nodes
            CallDebug("Expanding Coastline Nodes");
            // nodes grow until alpha value turns 253/255 -> growth similar to celular automata
            if (!ExpandedCoastalNodes()) { auxValue++; }
            else
            {
              generation_phase++;
              auxValue = 0;
            }
            Showcase();
            break;
          case 6:  // Total Alpha Clean Up  
            CallDebug("Transforming color.a!=0f & 1f into color.a=1f");
            shape_bmp = shape_bmp.Non0AlphaTo1();
            generation_phase++;
            auxValue = 0;
            Showcase();
            break;
          case 7:  // Placing Inland Nodes in island
            CallDebug("Placing Inland Nodes");
            PlaceInlandNodes();
            generation_phase++;
            Showcase();
            break;
          case 8:  // Expanding Inland Nodes
            CallDebug("Expanding Inland Nodes");
            if (auxValue%350 == 0 && perlinDone)
            {
              perlinDone = false;
              auxValue++;
              StartCoroutine(CalculatePerlinNoise());
            }
            if (perlinDone)
            {
              if (!ExpandedInlandNodes()) { auxValue++; }
              else 
              {
                generation_phase++;
                auxValue = 0;
            } }
            Showcase();
            break;
          case 9:  // Checking area values and calling for a reset if any biome has less than 9k pixels
            CallDebug("Checking Biomes' area values");
            bool needsReset = false;
            for (int i=0; i<inlandNodesStringList.Count; i++) { if(zonesAreaArray[i]<9000) { needsReset = true; break; } }
            if (needsReset)
            {
              GenerationReset();
              generation_phase = 2;
            } else { generation_phase++; }
            Showcase();
            break;
          case 10: // If Village area is bellow average: switch with average area biome
            CallDebug("checking Village Biome size");
            CheckVillageArea();
            generation_phase++;
            Showcase();
            break;
          case 11: // Organizing Zone Data
            CallDebug("Organizing Zone Data");
            OrganizeFullZones(); // this could have been done in the inlandNodes expansion phase
            generation_phase++;
            Showcase();
            break;
          case 12: // Generating colors array; 
            CallDebug("Generating colors array");
            if (auxValue == 0) { colors = new Color[size,size]; }
            if (auxValue< size)
            {
              for (int i=0; i<size; i++) 
              { 
                colors[auxValue,i] = shape_bmp.GetPixel(i,auxValue);
              }
              auxValue++;
            } else {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 13: // Generating outlines and shoreline lists;
            CallDebug("Generating outlines and shoreline lists");
            if (auxValue == 0)
            {
              outlines = new List<Vector2Int>[zonesRValueArray.Length];
              shoreline = new List<Vector2Int>();
              sidelines = new List<Vector2Int>[fullZonesNames.Count];

              for (int i=0; i<fullZonesNames.Count; i++)
              {
                sidelines[i] = new List<Vector2Int>();
                if (i<zonesRValueArray.Length) { outlines[i] = new List<Vector2Int>(); }
            } }
            if (auxValue< size)
            {
              for (int i=0; i<size; i++)
              {
                Vector2Int point = new Vector2Int(i,auxValue);
                int index = FindIndexFromRGB(colors[point.y,point.x]);
                int index_outline = 0;
                bool isOutline = false;
                bool isShoreline = false;
                bool isSideline = false;
                if (colors[point.y,point.x].a != 0f)
                {
                  foreach(Vector2Int neighbour in point.Neighbours(size))
                  {
                    if (colors[point.y,point.x] != colors[neighbour.y,neighbour.x])
                    {
                      if (fullZonesNames[index].Contains("-"))
                      {
                        if (colors[neighbour.y,neighbour.x].a == 0f) 
                        { isShoreline = true; }
                        else
                        {
                          int neighbourIndex = FindIndexFromRGB(colors[neighbour.y,neighbour.x]);
                          if (fullZonesNames[neighbourIndex].Contains("-")) { isSideline = true;  }
                      } }
                      if (colors[point.y,point.x].r != colors[neighbour.y,neighbour.x].r) {
                        if (!isOutline)
                        {
                          isOutline = true;
                          index_outline = System.Array.IndexOf( zonesRValueArray , Mathf.RoundToInt(255f*colors[point.y,point.x].r) );
                    } } }
                    if (isOutline)   { outlines[index_outline].Add(point); }
                    if (isShoreline) { shoreline.Add(point);         }
                    if (isSideline)  { sidelines[index].Add(point);  }
              } } }
              auxValue++;
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 14: // Generating distToBorder, distToShore, and holes arrays
            CallDebug("Generating distToBorder, distToShore and holes arrays");
            if (auxValue == 0)
            {
              holes = new bool[size,size];
              distToBorder = new float[size,size];
              distToShore = new float[size,size];
              distToSide = new float[size,size];
            }
            if (auxValue < size)
            {
              for (int i=0; i<size; i++)
              {
                Vector2Int point = new Vector2Int(i,auxValue);
                Color col = colors[point.y,point.x];
                if (col.a == 0f)
                {
                  holes[point.y,point.x] = false;
                  distToBorder[point.y,point.x] = 0f;
                  distToShore[point.y,point.x] = 0f;
                  distToSide[point.y,point.x] = 0f;
                } else 
                {
                  holes[point.y,point.x] = true;
                  int index = FindIndexFromRGB(col);
                  int index_2 = System.Array.IndexOf(zonesRValueArray, Mathf.RoundToInt(col.r*255f));
                  distToBorder[point.y,point.x] = point.GetClosestDistance(outlines[index_2]);
                  distToShore[point.y,point.x] = point.GetClosestDistance(shoreline);
                  if (fullZonesNames[index].Contains("-"))
                  {
                    distToSide[point.y,point.x] = point.GetClosestDistance(sidelines[index]);
                  } else 
                  {
                    distToSide[point.y,point.x] = 200f;
              } } }
              auxValue++;
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 15: // Getting max/min values for distToBorder, and distToShpre
            CallDebug("Getting max/min values for distToBorder and distToShore");
            maxDistToShore = float.MinValue;
            for (int i=0; i<size; i++) { for (int j=0; j<size; j++) { if (distToShore[j,i]>maxDistToShore) {maxDistToShore = distToShore[j,i]; } } }
            maxDistToBorder = new float[zonesRValueArray.Length];
            for (int k=0; k<zonesRValueArray.Length; k++){
              maxDistToBorder[k] = 0f;
              foreach(Vector2Int point in zonesV2List[k]){
                if (distToBorder[point.y,point.x] > maxDistToBorder[k]) 
                { maxDistToBorder[k] = distToBorder[point.y,point.x]; }
              }
            }
            generation_phase++;
            auxValue=0;
            break;
          case 16: // Generating base Heightmap
            CallDebug("Generating base Heightmap");
            if (auxValue == 0)
            {
              heights = new float[size+1,size+1];
              initialPoint = new Vector2[10];
              initialPoint[0] = new Vector2(Random.Range(-500f,500f),Random.Range(-500f,500f));
            }
            if (auxValue<=size)
            {
              for (int i=0; i<=size; i++)
              {
                int i_ = Mathf.Min(i,size-1);
                int j_ = Mathf.Min(auxValue,size-1);
                if (colors[j_,i_].a != 0f) 
                { 
                  heights[auxValue,i] = 0.21f*distToShore[j_,i_]/maxDistToShore
                                      + 0.07f*2*(0.5f-SinglePointNoise(i,auxValue,initialPoint[0],5,2f,2.1f))
                                      + 0.01f;
                } else { heights[auxValue,i] = -0.1f; }
              }
              auxValue++;
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 17: // Calculating average heightmap value
            CallDebug("Calculating average heightmap value");
            if (auxValue == 0)
            {
              averageHeight = new float[zonesRValueArray.Length];
              pointCount = new int[zonesRValueArray.Length];
            }
            if (auxValue <= size)
            {
              for (int i=0; i<= size; i++)
              {
                int i_ = Mathf.Min(i,size-1);
                int j_ = Mathf.Min(auxValue,size-1);
                Color col = colors[j_,i_];
                if (col.a != 0f)
                {
                  int red = Mathf.RoundToInt(col.r*255f);
                  int index_R = System.Array.IndexOf(zonesRValueArray,red);
                  averageHeight[index_R]+=heights[auxValue,i];
                  pointCount[index_R]++;
              } } 
              auxValue++;
            } else
            {
              for (int i=0; i<averageHeight.Length; i++) { averageHeight[i] = averageHeight[i] / pointCount[i]; }
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 18: // Generating terrain Heightmap // HERE - create this step
            CallDebug("generating terrain Heightmap");
            if (auxValue == 0){
              initialPoint[1] = new Vector2(Random.Range(-500f,500f),Random.Range(-500f,500f));
              initialPoint[2] = new Vector2(Random.Range(-500f,500f),Random.Range(-500f,500f));
              initialPoint[3] = new Vector2(Random.Range(-500f,500f),Random.Range(-500f,500f));
              initialPoint[4] = new Vector2(Random.Range(-500f,500f),Random.Range(-500f,500f));
              initialPoint[5] = new Vector2(Random.Range(-500f,500f),Random.Range(-500f,500f));
            }
            if (auxValue<=size)
            {
              for (int i=0; i<=size; i++)
              {
                int i_ = Mathf.Min(i,size-1);
                int j_ = Mathf.Min(auxValue,size-1);
                int index_f = -1;
                Color col = colors[j_,i_];
                int index_R = System.Array.IndexOf(zonesRValueArray, Mathf.RoundToInt(col.r*255f));
                if (col.a != 0f) { index_f = FindIndexFromRGB(col); }
                if (index_f != -1)
                {
                  if (col.r != 0)
                  {
                    float n = 3f;
                    if (distToBorder[j_,i_] > n)
                    {
                      string name = "";
                      if (fullZonesNames[index_f].Contains("-")) { name = fullZonesNames[index_f].Split('-')[0]; }
                      else { name = fullZonesNames[index_f]; }
                      name = name.Substring(0,name.Length-3);
                      switch(name)
                      {
                        case "Alps": // RENAME THIS SHIT -> not making alps anymore also REDO this shit
                          float multiplier_alps = Mathf.Pow(Mathf.Max(0f,Mathf.Min(1f,(distToBorder[j_,i_] - n)/17f)),2f);
                          float noise_alps = SinglePointNoise(i,auxValue,initialPoint[7],3,2.8f,3.4f);
                          noise_alps = Mathf.Pow((1f - 2f*Mathf.Abs(0.5f - noise_alps)),2f);float noise_alps_2 = SinglePointNoise(i,auxValue,initialPoint[8],5,2f,2.1f);
                          noise_alps_2 = (2f*(0.3f - noise_alps_2));
                          heights[auxValue,i] -= multiplier_alps*2.2f*Mathf.Pow(Mathf.Max(0f,(0.3f*noise_alps+0.25f*noise_alps_2)),2f);
                          break;
                        case "Swamp":
                          // break;
                        case "Marsh":
                          // break;
                        case "RainForest":
                          // break;
                        case "Savannah":
                        case "GeyserField":
                          // break;
                        case "Hills":
                          // Here - mess with Noise Test until satisfied
                          // break;
                        case "Volcano": // HERE - copy lone island code 
                          // lone island code
                          // second pass: calculate highest point
                          // if point > 0.9f*highestpointHeight 
                          // { point = 0.9f*H - (((point.Height-0.9f*Highest)/(0.1H))^2)*0.1H; }
                          // break;
                        case "Ravine": 
                          // float multiplier_ravine = Mathf.Pow(Mathf.Max(0f,Mathf.Min(1f,(distToBorder[j_,i_] - n)/20f)),2f);
                          // float noise_ravine = SinglePointNoise(i,auxValue,initialPoint[7],3,2f,2.2f);
                          // noise_ravine = 2f*Mathf.Abs(0.5f - noise_ravine);
                          // heights[auxValue,i] += 0.2f*multiplier_ravine*noise_ravine;
                          // break;
                        case "Desert":
                          // break;
                        case "LencoisMaranhenses":
                          // break;
                        case "DuelingPeaks":
                          // break;
                        case "Tundra":
                        case "Taiga":
                        case "BambooForest":
                          // break;
                        case "MossForest":
                          float noise_mossForest = SinglePointNoise(i,auxValue,initialPoint[8],2,2f,1.8f);
                          noise_mossForest = Mathf.Pow(2f*Mathf.Abs(0.5f - noise_mossForest),2f);
                          heights[auxValue,i] += 0.02f*noise_mossForest;
                          break;
                        case "Mesa": // OK
                          if (MesaNoise(i,auxValue, initialPoint[1]))
                          { heights[auxValue,i] += 0.03f*(0.7f+0.3f*SinglePointNoise(i,auxValue,initialPoint[2],2,2f,1.6f)); }
                          break;
                        case "LoneMountain": // OK
                          float multiplier = Mathf.Pow((distToBorder[j_,i_]-n)/(maxDistToBorder[index_R]-n),2f);
                          heights[auxValue,i] += (0.25f+0.095f*SinglePointNoise(i,auxValue,initialPoint[3],2,2f,1.6f))*multiplier;
                          break;
                        case "Plateau": // HERE - add some height and shape variation
                          float newValue_plateau = 0.001f+3.3f*averageHeight[index_R];
                          if (newValue_plateau > heights[auxValue,i])
                          {
                            float difference_plateau = newValue_plateau - heights[auxValue,i];
                            heights[auxValue,i] += difference_plateau*difference_plateau+0.005f;
                          }
                          break;
                        case "RuralArea":
                        case "FlatLand":  // OK
                          // break;
                        case "AbandonedVillage":
                        case "RuinedVillage":
                        case "Village": // OK
                        default:
                          // Debug.Log("whaaaat: "+name);
                          break;
              } } } } }
              auxValue++;
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 19: // Generating shores Heightmap // HERE - create this step
            CallDebug("Generating shores Heightmap");
            if (auxValue<=size)
            {
              for (int i=0; i<=size; i++)
              {
                int i_ = Mathf.Min(i,size-1);
                int j_ = Mathf.Min(auxValue,size-1);
                int index_f = -1;
                if (colors[j_,i_].a != 0f) { index_f = FindIndexFromRGB(colors[j_,i_]); }
                if (index_f != -1)
                {
                  if (fullZonesNames[index_f].Contains("-"))
                  { 
                    string name = fullZonesNames[index_f].Split('-')[1];
                    name = name.Substring(0,name.Length-3);
                    switch (name)
                    {
                      case "Cliff":
                      case "Harbour":
                      case "RockyCoast":
                      case "Causeway":
                      case "StrandPoE":
                      case "Beach":
                        break;
                      default:
                        Debug.Log("coast whaaaat: "+name);
                        break;
              } } } }
              auxValue++;
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 20: // Creating terrain and applying holes and heights to it
            // HERE - create this step - create terrain, enable cameras to see it, position terrain, apply holes and heights
            TerrainData _terrainData = new TerrainData();
            _terrainData.heightmapResolution = 1025;
            _terrainData.SetHoles(0,0,holes);
            _terrainData.SetHeights(0,0,heights);
            _terrain = Terrain.CreateTerrainGameObject(_terrainData);
            _terrain.transform.name = "Terrain_0_0";
            _terrain.GetComponent<Terrain>().drawInstanced = true;
            _terrain.GetComponent<Terrain>().heightmapPixelError = 1;
            _terrainData.size = new Vector3(1600,maxHeight,1600);
            _terrain.transform.position = new Vector3(-800,0,-800);
            generation_phase++;
            auxValue = 0;
            break;
          case 21: // Generating bitmaps for data to save (distToborder, distToShore, distToSide, heights); 
            CallDebug("Generating bmp data to save");
            if (_save)
            {
              if (auxValue == 0)
              {
                distToBorder_bmp = new Texture2D(size,size,TextureFormat.ARGB32, false);
                distToShore_bmp = new Texture2D(size,size,TextureFormat.ARGB32, false);
                distToSide_bmp = new Texture2D(size,size,TextureFormat.ARGB32, false);
                heights_bmp = new Texture2D(size,size,TextureFormat.ARGB32, false);
              }
              if (auxValue < size)
              {
                for (int i = 0; i<size; i++)
                { 
                  if (holes[auxValue,i])
                  {
                    float r = Mathf.Max(0,distToBorder[auxValue,i]-51f)/25.5f;
                    float g = Mathf.Max(0,distToBorder[auxValue,i]-25.5f)/25.5f;
                    float b = Mathf.Max(0,distToBorder[auxValue,i])/25.5f;
                    distToBorder_bmp.SetPixel(i,auxValue,new Color(r,g,b,1f));

                    r = Mathf.Max(0,3*distToShore[auxValue,i]-2*maxDistToShore)/maxDistToShore;
                    g = Mathf.Max(0,3*distToShore[auxValue,i]-maxDistToShore)/maxDistToShore;
                    b = Mathf.Max(0,3*distToShore[auxValue,i])/maxDistToShore;
                    distToShore_bmp.SetPixel(i,auxValue,new Color(r,g,b,1f));

                    r = Mathf.Max(0,distToSide[auxValue,i]-51f)/25.5f;
                    g = Mathf.Max(0,distToSide[auxValue,i]-25.5f)/25.5f;
                    b = Mathf.Max(0,distToSide[auxValue,i])/25.5f;
                    distToSide_bmp.SetPixel(i,auxValue,new Color(r,g,b,1f));

                    r = Mathf.Min(1f,Mathf.Max(0,3*heights[auxValue,i]-2f));
                    g = Mathf.Min(1f,Mathf.Max(0,3*heights[auxValue,i]-1f)); // HERE - set the r,g,b values for heights_bmp
                    b = Mathf.Min(1f,3*heights[auxValue,i]);
                    heights_bmp.SetPixel(i,auxValue,new Color(r,g,b,1f));
                  } else 
                  {
                    distToBorder_bmp.SetPixel(i,auxValue,new Color(0,0,0,0));
                    distToShore_bmp.SetPixel(i,auxValue,new Color(0,0,0,0));
                    distToSide_bmp.SetPixel(i,auxValue,new Color(0,0,0,0));
                    heights_bmp.SetPixel(i,auxValue,new Color(0,0,0,0));
                } }
                auxValue++;
              } else 
              {
                generation_phase++;
                auxValue = 0;
              }
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 22: // Saving && / || Starting  nextGenerator.cs
            if (_save)
            {
              CallDebug("saving generated biome into files", false);
              Save();
            }
            if (_continue)
            {
              CallDebug("Starting Next Generator", false);
              // CreateHeightMapGeneratorScript();
            }
            generation_phase++;
            break;
          case 23: // Finished
            CallDebug("Finished BiomeGenerator.cs", false);
            if (dataCollect)
            {
              // HERE - Collect Data
            }
            finished = true;
            generation_phase++;
            RenderFinal();
            break;
          default:
            CallDebug("Default - Last step - finished BiomeGenerator.cs", false);
            break;
        }
      } else {
        if (!_continue) { UIMan.SetDone(true); }
        if (destroyOnEnd && UIdone)
        {
          if (debugging) { Debug.Log("Destroying this object's BiomeGenerator.cs script"); }
          destroyOnEnd = false;
          StartCoroutine(finalCountdown());
    } } }

// private methods ----------------------------
//         unique -----------------------------
    IEnumerator CalculatePerlinNoise()
    {
      System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
      stopwatch.Start();
      int yieldCount = 1;
      perlinPoint = new Vector2[inlandNodesStringList.Count];
      gradients = new float[inlandNodesStringList.Count][];
      for (int a=0; a<inlandNodesStringList.Count; a++)
      {
        perlinPoint[a] = new Vector2(Random.Range(-500f,500f),Random.Range(-500f,500f));
        gradients [a] = new float[size*size];
        float maxValue = float.MinValue;
        float minValue = float.MaxValue;
        for (int i=0; i<size; i++)
        {
          for (int j=0; j<size; j++)
          {
            if (stopwatch.ElapsedMilliseconds-yieldCount*20>0)
            {
              yieldCount++;
              yield return null;
            }
            float x = (float)i*0.39f;
            float y = (float)j*0.39f;
            float newValue = Mathf.PerlinNoise(x+perlinPoint[a].x,y+perlinPoint[a].y);
            float m = 1f;
            for (int k=0; k<5; k++)
            {
              x = x/2;
              y = y/2;
              m = m*2.1f;
              newValue+=Mathf.PerlinNoise(x+perlinPoint[a].x,y+perlinPoint[a].y)*m;
            }
            gradients[a][i*size+j]=newValue;
            if (newValue>maxValue) { maxValue = newValue; }
            if (newValue<minValue) { minValue = newValue; }
        } }
        for (int i=0; i<size; i++)
        {
          for (int j=0; j<size; j++)
          {
            float result = (gradients[a][i*size+j]-minValue)/(maxValue-minValue);
            result = result - result%0.05f;
            if (result >= 1-7*0.05f) { result = 0.99f; }
            if (result <= 7*0.05f) { result = 0f; }
            gradients[a][i*size+j] = result;
      } } }
      stopwatch.Stop();
      perlinDone = true;
    }
    void CleanCoastalLeftovers()
    {
      List<Vector2Int> auxList = new List<Vector2Int>();
      foreach(Vector2Int point in coastalNodesV2List)
      {
        Color newColor = shape_bmp.GetPixel(point.x,point.y);
        if (Mathf.RoundToInt(newColor.a*255)==253) { auxList.Add(point); } 
        else 
        {
          newColor.a = 1f;
          shape_bmp.SetPixel(point.x,point.y,newColor);
      } }
      coastalNodesV2List = auxList;
    }
    void CheckVillageArea()
    {
      int villageIndex = 0;
      int villageArea = 0;
      int averageArea = 0;
      bool aboveAverage = false;
      int maxArea = 0;
      int maxIndex = 0;
      for (int i=0; i<inlandNodesStringList.Count; i++)
      {
        if (inlandNodesStringList[i] == "Village")
        {
          villageIndex = i;
          villageArea = zonesAreaArray[i];
        }
        averageArea+=zonesAreaArray[i];
        if (zonesAreaArray[i]>maxArea)
        {
          maxArea = zonesAreaArray[i];
          maxIndex = i;
      } }
      averageArea /= inlandNodesStringList.Count;
      if (villageArea>=averageArea) { aboveAverage = true; }
      if (!aboveAverage)
      {
        string temp = inlandNodesStringList[maxIndex];
        inlandNodesStringList[maxIndex] = inlandNodesStringList[villageIndex];
        inlandNodesStringList[villageIndex] = temp;
    } }
    void ChooseCoastalNodes(int n)
    {
      List<string> coastalData = GatherCoastalData();
      List<string> auxList = new List<string>();
      foreach(string nodeName in coastalData)
      {
        if (nodeName.Contains("MustHave"))
        {
          n--;
          string newNodeName = nodeName.Replace("MustHave","");
          coastalNodesStringList.Add(newNodeName);
        } else { auxList.Add(nodeName); }
      }
      coastalData = auxList;
      for (int i=0; i<n; i++)
      {
        int rnd = Random.Range(0,coastalData.Count-1);
        string nodeName = coastalData[rnd];
        if (nodeName.Contains("Unlimited"))
        {
          string newNodeName = nodeName.Replace("Unlimited","");
          coastalNodesStringList.Add(newNodeName);
        } else 
        {
          coastalNodesStringList.Add(nodeName);
          coastalData.RemoveAt(rnd);
    } } }
    void ChooseInlandNodes(int n)
    {
      List<string> inlandData = GatherInlandData();
      List<string> auxList = new List<string>();
      foreach(string nodeName in inlandData)
      {
        if (nodeName.Contains("MustHave"))
        {
          n--;
          string newNodeName = nodeName.Replace("MustHave","");
          inlandNodesStringList.Add(newNodeName);
        } else { auxList.Add(nodeName); }
      }
      inlandData = auxList;
      for (int i=0; i<n; i++)
      {
        int rnd = Random.Range(0,inlandData.Count-1);
        string nodeName = inlandData[rnd];
        if (nodeName.Contains("Unlimited"))
        {
          string newNodeName = nodeName.Replace("Unlimited","");
          inlandNodesStringList.Add(newNodeName);
        } else 
        {
          inlandNodesStringList.Add(nodeName);
          inlandData.RemoveAt(rnd);
    } } }
    void ChooseNodes()
    {
      int numberOfCoastalNodes = 0;
      int minCN = 9;
      int maxCN = 14;
      int perimeterSize = shape_bmp.MeasurePerimeter();
      int minPer = 2000;
      int maxPer = 6000;
      numberOfCoastalNodes = Mathf.RoundToInt(((float)(perimeterSize-minPer)*(float)(maxCN-minCN)/(float)(maxPer-minPer))+minCN);

      int numberOfInlandNodes = 0;
      int minIN = 7;  //8
      int maxIN = 10;//12
      int islandArea = shape_bmp.MeasureArea();
      int minArea = 120000;
      int maxArea = 260000;
      numberOfInlandNodes = Mathf.RoundToInt(((float)(islandArea-minArea)*(float)(maxIN-minIN)/(float)(maxArea-minArea))+minIN);
      ChooseCoastalNodes(numberOfCoastalNodes);
      ChooseInlandNodes(numberOfInlandNodes);
    }
    void DebugCoastalNodesList(int n)
    {
      int i = 0;
      foreach(string nodeName in coastalNodesStringList)
      {
        Debug.Log("Debug"+n+", coastalNodesStringList string number "+i+": "+nodeName);
        i++;
    } }
    void DebugInlandNodesList(int n)
    {
      int i = 0;
      foreach(string nodeName in inlandNodesStringList)
      {
        Debug.Log("Debug"+n+", inlandNodesStringList string number "+i+": "+nodeName);
        i++;
    } }
    bool ExpandedCoastalNodes()
    {
      bool done = false;
      if (coastalNodesV2List.Count>0)
      {
        List<Vector2Int> auxList = new List<Vector2Int>();
        foreach(Vector2Int point in coastalNodesV2List)
        {
          Color newColor = shape_bmp.GetPixel(point.x,point.y);
          if (Mathf.RoundToInt(newColor.a*255)<254)
          {
            float rnd = Random.value;
            auxList.Add(point);
            if (rnd>0.2f) { newColor.a = (float)(Mathf.RoundToInt(newColor.a*255)+1)/255f; } // there is a chance it gets a free grow!
            shape_bmp.SetPixel(point.x,point.y,newColor);
            float rnd2 = Random.value;
            if (rnd2>0.3f)
            { // there is a chance it doesn't grow! at all
              foreach (Vector2Int neighbour in point.Neighbours(size))
              {
                float rnd3 = Random.value;
                if (rnd3>0.4f)
                { // there is a chance its growth isn't received by some neighbour
                  if (shape_bmp.GetPixel(neighbour.x,neighbour.y).a==1f)
                  {
                    shape_bmp.SetPixel(neighbour.x,neighbour.y,newColor);
                    auxList.Add(neighbour);
        } } } } } }
        coastalNodesV2List = auxList;
      } else { done = true; }
      return done;
    }
    bool ExpandedInlandNodes()
    {
      bool done = false;
      List<Vector2Int> auxList = new List<Vector2Int>();
      int minArea = int.MaxValue;
      int maxArea = int.MinValue;
      for (int i=0; i<zonesAreaArray.Length; i++)
      {
        if (zonesAreaArray[i]<minArea) { minArea = zonesAreaArray[i]; }
        if (zonesAreaArray[i]>maxArea) { maxArea = zonesAreaArray[i]; }
      }
      foreach (Vector2Int point in inlandNodesV2List)
      {
        Vector2Int[] neighbours = point.Neighbours(size);
        Color nodeColor = shape_bmp.GetPixel(point.x,point.y);
        int index = System.Array.IndexOf(zonesRValueArray,Mathf.RoundToInt(nodeColor.r*255f));
        bool freeNeighboursExist = false;
        float minRnd = 0.4f;
        foreach(Vector2Int neighbour in neighbours)
        {
          Color neighbourColor = shape_bmp.GetPixel(neighbour.x,neighbour.y);
          if (Mathf.RoundToInt(neighbourColor.a*255)==254) { minRnd /=6f; } 
        }
        int failCount = 0;
        foreach(Vector2Int neighbour in neighbours)
        {
          Color newColor = shape_bmp.GetPixel(neighbour.x,neighbour.y);
          if (Mathf.RoundToInt(newColor.a*255)>253)
          {
            freeNeighboursExist = true;
            if (zonesAreaArray[index]<maxArea+25)
            {
              float rnd = Random.value;
              int gradIndex = System.Array.IndexOf(zonesRValueArray,Mathf.RoundToInt(nodeColor.r*255f));
              if (rnd>1-minRnd-gradients[gradIndex][neighbour.x*size+neighbour.y]*0.5f)
              {
                newColor.a = 253f/255f;
                newColor.r = nodeColor.r;
                newColor.g = nodeColor.g;
                int insertIndex = Random.Range(0,auxList.Count-1);
                auxList.Insert(insertIndex,neighbour);
                shape_bmp.SetPixel(neighbour.x,neighbour.y,newColor);
                zonesAreaArray[index]++;
                zonesV2List[index].Add(neighbour);
              } else 
              {
                newColor.a = 254f/255f;
                failCount++;
                shape_bmp.SetPixel(neighbour.x,neighbour.y,newColor);
        } } } }
        if (failCount>4)
        {
          minRnd = 0.9f;
          foreach (Vector2Int neighbour in neighbours)
          {
            float rnd = Random.value;
            if (rnd > 1-minRnd)
            {
              Color neighbourColor = shape_bmp.GetPixel(neighbour.x,neighbour.y);
              if (Mathf.RoundToInt(neighbourColor.a*255)==254)
              {
                neighbourColor.a = 1f;
                minRnd-=0.15f;
                shape_bmp.SetPixel(neighbour.x,neighbour.y,neighbourColor);
        } } } }
        if (freeNeighboursExist)
        {
          int insertIndex = Random.Range(0,auxList.Count-1);
          auxList.Insert(insertIndex,point);
      } }
      inlandNodesV2List = auxList;
      if (inlandNodesV2List.Count == 0) { done = true; }
      return done;
    }
    int FindIndexFromRGB(Color col)
    {
      int red = Mathf.RoundToInt(col.r*255f);
      int blue = Mathf.RoundToInt(col.b*255f);
      int index = -100;
      int i =0;
      if (blue != 0) 
      {
        foreach (string biome in fullZonesNames)
        {
          if (biome.Contains("-"))
          {
            string[] part = biome.Split('-');
            if (part[0].Contains(red.ToString("D3")) && part[1].Contains(blue.ToString("D3"))) { index = i; break; }
          }
          i++;
        }
      } else 
      {
        foreach (string biome in fullZonesNames)
        {
          if (!biome.Contains("-")) { if (biome.Contains(red.ToString("D3"))) { index = i; break; } }
          i++;
      } }
      return index;
    }
    List<string> GatherCoastalData()
    {
      List<string> auxList = new List<string>();
      string path = Application.dataPath+"/Data/BGNodes.csv";
      string dataRead = File.ReadAllText(path);
      string[] dataLines = dataRead.Split('\n');
      foreach(string line in dataLines)
      {
        string[] lContent = line.Split(',');
        int enabled = int.Parse(lContent[5]);
        if (enabled == 1)
        {
          if (lContent[4] == "2")
          { //if it is a coastal node
            if (lContent[3] != "0")
            { //if MustHaveQtt != 0
              string newString = "MustHave"+lContent[0];
              int mhQuantity = int.Parse(lContent[3]);
              for (int i=0; i<mhQuantity; i++) { auxList.Add(newString); } 
              if (lContent[2]!="0")
              { //if it can only appear a limited ammount
                int totalQuantity = int.Parse(lContent[2]);
                if (totalQuantity>mhQuantity)
                {
                  int remainingQuantity = totalQuantity-mhQuantity;
                  for (int i_2=0; i_2<remainingQuantity; i_2++) { auxList.Add(lContent[0]); } 
                } 
              } else 
              { //if it can appear any number of times
                string newString_2 = "Unlimited"+lContent[0];
                auxList.Add(newString_2);
              } 
            } else 
            {  //if MustHaveQtt == 0
              if (lContent[2]!="0")
              { //if it can only appear a limited ammount
                int quantity = int.Parse(lContent[2]);
                for (int i_3=0; i_3<quantity; i_3++) { auxList.Add(lContent[0]); }
              } else 
              { //if it can appear any number of times
                string newString_3 = "Unlimited"+lContent[0];
                auxList.Add(newString_3);
      } } } } }
      return auxList;
    }
    List<string> GatherInlandData()
    {
      List<string> auxList = new List<string>();
      string path = Application.dataPath+"/Data/BGNodes.csv";
      string dataRead = File.ReadAllText(path);
      string[] dataLines = dataRead.Split('\n');
      foreach(string line in dataLines)
      {
        string[] lContent = line.Split(',');
        int enabled = int.Parse(lContent[5]);
        if (enabled == 1)
        { //if it is enabled
          if (lContent[4] != "2")
          { //if it isn't a coastal node
            if (lContent[4] == "0")
            { //if it is a strict inland node
              string newlContentZero = "Strict"+lContent[0];
              lContent[0] = newlContentZero;
            }
            if (lContent[3] != "0")
            { //if MustHaveQtt != 0
              string newString = "MustHave"+lContent[0];
              int mhQuantity = int.Parse(lContent[3]);
              for (int i=0; i<mhQuantity; i++) { auxList.Add(newString); } 
              if (lContent[2]!="0")
              { //if it can only appear a limited ammount
                int totalQuantity = int.Parse(lContent[2]);
                if (totalQuantity>mhQuantity)
                {
                  int remainingQuantity = totalQuantity-mhQuantity;
                  for (int i_2=0; i_2<remainingQuantity; i_2++) { auxList.Add(lContent[0]); } 
                } 
              } else 
              { //if it can appear any number of times
                string newString_2 = "Unlimited"+lContent[0];
                auxList.Add(newString_2);
              } 
            } else 
            {  //if MustHaveQtt == 0
              if (lContent[2]!="0")
              { //if it can only appear a limited ammount
                int quantity = int.Parse(lContent[2]);
                for (int i_3=0; i_3<quantity; i_3++) { auxList.Add(lContent[0]); }
              } else 
              { //if it can appear any number of times
                string newString_3 = "Unlimited"+lContent[0];
                auxList.Add(newString_3);
      } } } } }
      return auxList;
    }
    void GenerationReset()
    {
      generation_phase = 2;
      nodesChosen = false;
      auxValue = 0;
      inlandNodesStringList = new List<string>();
      inlandNodesV2List = new List<Vector2Int>();
      zonesRValueArray = null;
      zonesAreaArray = null;
      zonesV2List = new List<List<Vector2Int>>();
      perlinPoint = null;
      gradients = null;
      perlinDone = true;
      coastalNodesStringList = new List<string>();
      coastalNodesBlueValueList = new List<int>();
      coastalNodesV2List = new List<Vector2Int>();
      startingCoastalWalkPoint = new Vector2Int(0,0);
      coastSize = 0;
      fullZonesRGBValues = new List<Vector3Int>();
      fullZonesNames = new List<string>();
      fullZonesArea = new List<int>();
      fullZonesV2List = new List<List<Vector2Int>>();
      roundNumber++;
    }
    void LogAreaData()
    {
      string date = System.DateTime.Now.ToString("yyyy_MM_");
      string path = "D:/UnityProjects/Data/"+date+"PCG_BGenAreaData.csv";
      if (!File.Exists(path))
      {
        string dataHeader = "Zone Area"+","+
                            "ZA/100"+
                            System.Environment.NewLine;
        File.WriteAllText(path,dataHeader);
      }
      int totalArea = 0;
      for (int i=0; i<zonesAreaArray.Length; i++) { totalArea+=zonesAreaArray[i]; }
      string dataText = "";
      for (int i=0; i<zonesAreaArray.Length; i++)
      {
        float percentArea = (float)zonesAreaArray[i]/(float)totalArea;
        dataText+=zonesAreaArray[i].ToString()+","+percentArea.ToString()+System.Environment.NewLine;
      }
      File.AppendAllText(path,dataText);
    }
    void OrganizeFullZones()
    {
      for (int i=0; i<size; i++)
      {
        for (int j=0; j<size; j++)
        {
          Color nodeColor = shape_bmp.GetPixel(i,j);
          if (nodeColor.a!=0)
          {
            Vector3Int v3 = new Vector3Int();
            v3.x = Mathf.RoundToInt(nodeColor.r*255f);
            v3.y = Mathf.RoundToInt(nodeColor.g*255f);
            v3.z = Mathf.RoundToInt(nodeColor.b*255f);
            if (!fullZonesRGBValues.Contains(v3))
            {
              fullZonesRGBValues.Add(v3);
              int index = fullZonesRGBValues.Count-1;
              string name = inlandNodesStringList[System.Array.IndexOf(zonesRValueArray,v3.x)]+v3.x.ToString("D3");
              if (v3.z!=0) { name+="-"+coastalNodesStringList[coastalNodesBlueValueList.IndexOf(v3.z)]+v3.z.ToString("D3"); }
              fullZonesNames.Add(name);
              fullZonesArea.Add(1);
              fullZonesV2List.Add(new List<Vector2Int>());
              Vector2Int v2 = new Vector2Int(i,j);
              fullZonesV2List[index].Add(v2);
            } else 
            {
              int index = fullZonesRGBValues.IndexOf(v3);
              fullZonesArea[index]+=1;
              Vector2Int v2 = new Vector2Int(i,j);
              fullZonesV2List[index].Add(v2);
    } } } } }
    float[] PerlinGradients(int localSize,Vector2 initialPoint)
    {
      float x0 = initialPoint.x;
      float y0 = initialPoint.y;
      float mult = 0.39f;
      float mod = 0.05f;
      int multCount = 5;
      float maxValue = float.MinValue;
      float minValue = float.MaxValue;
      float[] values = new float[size*size];
      for (int i=0; i<localSize; i++)
      {
        for (int j=0; j<localSize; j++)
        {
          float x = (float)i*mult;
          float y = (float)j*mult;
          float newValue = Mathf.PerlinNoise(x+x0,y+y0);
          float m = 1f;
          for (int k=0; k<multCount; k++)
          {
            x = x/2;
            y = y/2;
            m = m*2.1f;
            newValue+=Mathf.PerlinNoise(x+x0,y+y0)*m;
          }
          values[i*size+j]=newValue;
          if (newValue>maxValue) { maxValue = newValue; }
          if (newValue<minValue) { minValue = newValue; }
      } }
      for (int i=0; i<localSize; i++)
      {
        for (int j=0; j<localSize; j++)
        {
          float result = (values[i*size+j]-minValue)/(maxValue-minValue);
          result = result - result%mod;
          if (result >= 1-7*mod) { result = 0.99f; }
          if (result <= 7*mod) { result = 0f; }
          values[i*localSize+j] = result;
      } }
      return values;
    }
    void PlaceCoastalNodes()
    {
      int[] blueValue = new int[coastalNodesStringList.Count];      
      int minusValue = Mathf.FloorToInt(0.4f*255f/(coastalNodesStringList.Count+1f));
      for (int i=0; i<coastalNodesStringList.Count; i++) { blueValue[i]=255-minusValue*i; }
      blueValue = blueValue.Shuffle();
      int turn = 1;
      int totalTurns = coastalNodesStringList.Count;
      int walkSize = Mathf.RoundToInt(Random.Range(0.95f,0.99f)*(float)coastSize/(float)totalTurns);
      int walkLeft = coastSize - walkSize;
      Vector2Int point = startingCoastalWalkPoint;
      int bluePointer = 0;
      float greenValue = 0f;
      float redValue = 0f;
      Vector2Int lastMove = new Vector2Int(0,0);
      for (int k=0; k<coastSize; k++)
      {
        int i = point.x;
        int j = point.y;
        int i_p = Mathf.Min(point.x+1,size-1);
        int i_m = Mathf.Max(0,point.x-1);
        int j_p = Mathf.Min(point.y+1,size-1);
        int j_m = Mathf.Max(0,point.y-1);
        int initialMove = 0;
        Vector2Int[] moves = new Vector2Int[7]
        {
          new Vector2Int(i_m,j),  // 0; - 0
          new Vector2Int(i,j_m),  // 1; 0 +
          new Vector2Int(i_p,j),  // 2; + 0
          new Vector2Int(i,j_p),  // 3; 0 -
          new Vector2Int(i_m,j),  // 4; - 0
          new Vector2Int(i,j_m),  // 5; 0 +
          new Vector2Int(i_p,j)   // 6; + 0
        };
        Vector2Int[] neighbours = new Vector2Int[4];
        bool found = false;
        if (lastMove == new Vector2Int(0,1)) { initialMove = 2; } 
        else
        {
          if (lastMove == new Vector2Int(-1,0)) { initialMove = 3; } 
          else { if (lastMove == new Vector2Int(1,0)) { initialMove = 1; } }
        }
        neighbours[0] = moves[initialMove];
        neighbours[1] = moves[initialMove+1];
        neighbours[2] = moves[initialMove+2];
        neighbours[3] = moves[initialMove+3];   
        foreach(Vector2Int neighbour in neighbours)
        { // walk
          if (Mathf.RoundToInt(shape_bmp.GetPixel(neighbour.x,neighbour.y).a*255) == 253)
          {
            lastMove = new Vector2Int(neighbour.x-point.x,neighbour.y-point.y);
            point.x = neighbour.x;
            point.y = neighbour.y;
            found = true;
            break;
          }
        }
        if (!found)
        { //try the diagonal neighbours
          neighbours[0] = new Vector2Int(i_p,j_p);  //+1,+1
          neighbours[1] = new Vector2Int(i_p,j_m);  //+1,-1
          neighbours[2] = new Vector2Int(i_m,j_p);  //-1,+1
          neighbours[3] = new Vector2Int(i_m,j_m);  //-1,-1
          foreach(Vector2Int neighbour in neighbours)
          {
            if (Mathf.RoundToInt(shape_bmp.GetPixel(neighbour.x,neighbour.y).a*255) == 253)
            {
              lastMove = new Vector2Int(neighbour.x-point.x,neighbour.y-point.y);
              point.x = neighbour.x;
              point.y = neighbour.y;
              found = true;
              break;
        } } }
        if (!found && debugging) { Debug.Log("hey yo this the end why did you continue looking for shit???"); } // it is done with the main isle coastline and we are left with the holes in the isle
        Color newColor = shape_bmp.GetPixel(point.x,point.y); 
        newColor.b = (float)blueValue[bluePointer]/255f;
        newColor.g = greenValue;
        newColor.r = redValue;
        int growth = 14; // make it random later if necessary
        newColor.a = (252f-(float)growth)/255f;
        shape_bmp.SetPixel(point.x,point.y,newColor);
        walkSize--;
        if (walkSize==0)
        {
          turn++;
          if (turn==totalTurns) { walkSize = walkLeft; }
          else 
          {
            walkSize = Mathf.RoundToInt(Random.Range(0.95f,0.99f)*(float)coastSize/(float)totalTurns);
            walkLeft -= walkSize;
          }
          redValue=Random.value;
          greenValue=Random.value;
          coastalNodesBlueValueList.Add(blueValue[bluePointer]);
          bluePointer++;
    } } }
    void PlaceInlandNodes()
    {
      int minusValue = Mathf.FloorToInt(255f/(inlandNodesStringList.Count+1f));
      float iterN = 0;
      if (zonesAreaArray == null) { zonesAreaArray = new int[inlandNodesStringList.Count]; }
      if (zonesRValueArray == null) { zonesRValueArray = new int[inlandNodesStringList.Count]; }
      int redValue = 255;
      int greenValue = Random.Range(50,255);
      for (int i=0; i<inlandNodesStringList.Count; i++)
      {
        float minDistance = (float)(size/2f);
        bool found = false;
        Vector2Int point = new Vector2Int(0,0);
        while (!found)
        {
          iterN++;
          point = new Vector2Int(Random.Range(0,size-1),Random.Range(0,size-1));
          Color nodeColor = shape_bmp.GetPixel(point.x,point.y);
          if (nodeColor.a == 1f && nodeColor.b == 0f)
          {
            float cDistance = point.GetClosestDistance(inlandNodesV2List);
            if (cDistance>minDistance) { found = true; iterN = 0; }
          }
          if (iterN>500) { iterN = 0; minDistance--; }
        }
        inlandNodesV2List.Add(point);
        zonesAreaArray[i]++;
        zonesRValueArray[i]+=redValue;
        zonesV2List.Add(new List<Vector2Int>());
        zonesV2List[i].Add(point);
        Color newColor = new Color((float)redValue/255f,(float)greenValue/255f,0f,253f/255f);
        shape_bmp.SetPixel(point.x,point.y,newColor);
        redValue-=minusValue;
        greenValue = Random.Range(50,255);
    } }
    void SetCoastToStartPlacing_part1()
    { // This perimeter includes all internal holes
      for (int i=0; i<size; i++)
      {
        for (int j=0; j<size; j++)
        {
          if (shape_bmp.GetPixel(i,j).a==1f)
          {
            int neighboursCount = 0;
            foreach (Vector2Int neighbour in new Vector2Int(i,j).Neighbours(size)) { neighboursCount+=Mathf.RoundToInt(shape_bmp.GetPixel(neighbour.x,neighbour.y).a); }
            if (neighboursCount<8)
            {
              Color newColor = shape_bmp.GetPixel(i,j);
              newColor.a = 254f/255f;
              shape_bmp.SetPixel(i,j,newColor);
              coastalNodesV2List.Add(new Vector2Int(i,j));
              if (startingCoastalWalkPoint == new Vector2Int(0,0)) { startingCoastalWalkPoint = new Vector2Int(i,j); }
    } } } } }
    void SetCoastToStartPlacing_part2()
    { // walk until we can't anymore and measure coastSize
      int perimeterSize = 0;
      bool done = false;
      Vector2Int point = startingCoastalWalkPoint;
      Vector2Int lastMove = new Vector2Int(0,0);
      while(!done)
      {
        int i = point.x;
        int j = point.y;
        int i_p = Mathf.Min(point.x+1,size-1);
        int i_m = Mathf.Max(0,point.x-1);
        int j_p = Mathf.Min(point.y+1,size-1);
        int j_m = Mathf.Max(0,point.y-1);
        int initialMove = 0;
        Vector2Int[] moves = new Vector2Int[7]
        {
          new Vector2Int(i_m,j),  // 0; - 0
          new Vector2Int(i,j_m),  // 1; 0 +
          new Vector2Int(i_p,j),  // 2; + 0
          new Vector2Int(i,j_p),  // 3; 0 -
          new Vector2Int(i_m,j),  // 4; - 0
          new Vector2Int(i,j_m),  // 5; 0 +
          new Vector2Int(i_p,j)   // 6; + 0
        };
        Vector2Int[] neighbours = new Vector2Int[4];
        bool found = false;
        if (lastMove == new Vector2Int(0,1)) { initialMove = 2; } 
        else 
        {  
          if (lastMove == new Vector2Int(-1,0)) { initialMove = 3; } 
          else { if (lastMove == new Vector2Int(1,0)) { initialMove = 1; } }
        }     
        neighbours[0] = moves[initialMove];
        neighbours[1] = moves[initialMove+1];
        neighbours[2] = moves[initialMove+2];
        neighbours[3] = moves[initialMove+3];   
        foreach(Vector2Int neighbour in neighbours)
        { // walk
          if (Mathf.RoundToInt(shape_bmp.GetPixel(neighbour.x,neighbour.y).a*255) == 254)
          {
            lastMove = new Vector2Int(neighbour.x-point.x,neighbour.y-point.y);
            point.x = neighbour.x;
            point.y = neighbour.y;
            found = true;
            break;
        } }
        if (!found)
        { //try the diagonal neighbours
          neighbours[0] = new Vector2Int(i_p,j_p);  //+1,+1
          neighbours[1] = new Vector2Int(i_p,j_m);  //+1,-1
          neighbours[2] = new Vector2Int(i_m,j_p);  //-1,+1
          neighbours[3] = new Vector2Int(i_m,j_m);  //-1,-1
          foreach(Vector2Int neighbour in neighbours)
          {
            if (Mathf.RoundToInt(shape_bmp.GetPixel(neighbour.x,neighbour.y).a*255) == 254)
            {
              lastMove = new Vector2Int(neighbour.x-point.x,neighbour.y-point.y);
              point.x = neighbour.x;
              point.y = neighbour.y;
              found = true;
              break;
        } } }
        if (!found) { done = true; } 
        else 
        {
          Color newColor = shape_bmp.GetPixel(point.x,point.y); 
          newColor.a = 253f/255f;
          shape_bmp.SetPixel(point.x,point.y,newColor); // set point
          perimeterSize++;
      } }
      coastSize = perimeterSize;
    }
    void SetStartingCoastalWalkPoint()
    {
      for (int k=startingCoastalWalkPoint.y; k>=0; k--)
      {
        if (Mathf.RoundToInt(shape_bmp.GetPixel(startingCoastalWalkPoint.x,k).a*255)==254)
        {
          startingCoastalWalkPoint.y = k;
    } } }
    float SinglePointNoise(int x, int y, Vector2 v0, int n, float v1_multiplier, float m_multiplier)
    {
      float result = 0;
      float mult = 0.39f;
      float maxVal = 0;
      Vector2 v1 = new Vector2(x,y)*mult;
      float m = 1f;
      result += Mathf.PerlinNoise(v0.x+v1.x,v0.y+v1.y);
      maxVal+=1f;
      for (int k=0; k<n; k++)
      {
        v1/=v1_multiplier;
        m*=m_multiplier;
        result += Mathf.PerlinNoise(v0.x+v1.x,v0.y+v1.y)*m;
        maxVal += m;
      }
      result/=maxVal;
      return result;
    }
    bool MesaNoise(int x, int y, Vector2 v0)
    {
      float value_1 = SinglePointNoise(x,y,v0,3,2f,2.1f);
      bool result = false;
      if (value_1 >= 0.590f) { result = true; } 
      else if (value_1 <= 0.345f) { result = true; } 
      return result;
    }
//         common -----------*normalize-these--
    void CallDebug(string text)
    {
      if (debugging) { Debug.Log("Case: "+generation_phase+"."+auxValue+"."+roundNumber+", "+text); }
      if (useDebugUI) { UIMan.SetPDebugText(updateCount.ToString("D5")+" BG"+generation_phase.ToString("D3")+"."+auxValue.ToString("D4")+"."+roundNumber.ToString("D2")+", "+text); }
    }
    void CallDebug(string text, bool primary)
    {
      if (debugging) { Debug.Log("Case: "+generation_phase+"."+auxValue+"."+roundNumber+", "+text); }
      if (useDebugUI) {
        if (primary) { UIMan.SetPDebugText(updateCount.ToString("D5")+" BG"+generation_phase.ToString("D3")+"."+auxValue.ToString("D4")+"."+roundNumber.ToString("D2")+", "+text); } 
        else { UIMan.SetSDebugText(updateCount.ToString("D5")+" BG"+generation_phase.ToString("D3")+"."+auxValue.ToString("D4")+"."+roundNumber.ToString("D2")+", "+text); }
    } }
    void Load(int n, bool rnd)
    {
      shape_bmp = new Texture2D(1,1,TextureFormat.ARGB32, false);
      string path = Application.dataPath+"/Data/SavedGen/TG/SavedTG.txt";
      if (File.Exists(path))
      {
        int N = int.Parse(File.ReadAllText(path)) - 1;
        if (rnd) { n = Random.Range(0,N); }
        if (n<0 || n>N) {Debug.LogError("n must be between 0 and "+(N).ToString()); }
      } else { Debug.LogError("No File in desired path ("+path+")"); }
      path = Application.dataPath + "/Data/SavedGen/TG/_png/SavedTG_"+n+".png";
      if (File.Exists(path))
      {
        shape_bmp.LoadImage(File.ReadAllBytes(path));
        size = shape_bmp.width;
      } else { Debug.LogError("No File in desired path ("+path+")"); }
    }
    void RenderFinal() { if (renderBool) { shape_bmp.Apply(); } }
    void Save()
    {
      int n = 0;
      string path = Application.dataPath+"/Data/SavedGen/BG/SavedBG.txt";
      if (!File.Exists(path)) { File.WriteAllText(path,""); } 
      else 
      {
        string dataRead = File.ReadAllText(path);
        string[] dataLines = dataRead.Split('\n');
        n = dataLines.Length-1;
      }
      byte[] bytes = shape_bmp.EncodeToPNG();
      File.WriteAllBytes(Application.dataPath + "/Data/SavedGen/BG/_colors/"+n+".png", bytes);
      bytes = distToBorder_bmp.EncodeToPNG();
      File.WriteAllBytes(Application.dataPath + "/Data/SavedGen/BG/_distToBorder/"+n+".png", bytes);
      bytes = distToShore_bmp.EncodeToPNG();
      File.WriteAllBytes(Application.dataPath + "/Data/SavedGen/BG/_distToShore/"+n+".png", bytes);
      bytes = distToSide_bmp.EncodeToPNG();
      File.WriteAllBytes(Application.dataPath + "/Data/SavedGen/BG/_distToSide/"+n+".png", bytes);
      bytes = heights_bmp.EncodeToPNG();
      File.WriteAllBytes(Application.dataPath + "/Data/SavedGen/BG/_heights/"+n+".png", bytes);
      string text = "";
      foreach(string str in fullZonesNames) { text+=str; text+=";"; }
      text+="\n";
      File.AppendAllText(path,text);
    }
    void SetupRenderer()
    {
      if (renderBool)
      {
        Renderer rend = GetComponent<Renderer>();
        if (rend!=null) { rend.material.mainTexture = shape_bmp; } 
        else { Debug.LogError("NO Renderer IN THIS GAME OBJECT"); }
      } else { if (debugging) { Debug.Log("Not setting texture to renderer"); } }
    }
    void Showcase() { if (showcase) { shape_bmp.Apply(); } }

    IEnumerator finalCountdown()
    {
      if (useDebugUI)
      {
        UIMan.SetSDebugText(updateCount.ToString("D5")+" BG"+" is going to be destroyed in 5");
        yield return new WaitForSeconds(1);
        UIMan.SetSDebugText(updateCount.ToString("D5")+" BG"+" is going to be destroyed in 4");
        yield return new WaitForSeconds(1);
        UIMan.SetSDebugText(updateCount.ToString("D5")+" BG"+" is going to be destroyed in 3");
        yield return new WaitForSeconds(1);
        UIMan.SetSDebugText(updateCount.ToString("D5")+" BG"+" is going to be destroyed in 2");
        yield return new WaitForSeconds(1);
        UIMan.SetSDebugText(updateCount.ToString("D5")+" BG"+" is going to be destroyed in 1");
        yield return new WaitForSeconds(1);
        UIMan.SetPDebugText("");
        UIMan.SetSDebugText("");
      }
      BiomeGenerator script = GetComponent<BiomeGenerator>();
      Destroy(script);
    }
// public methods -----------------------------
}