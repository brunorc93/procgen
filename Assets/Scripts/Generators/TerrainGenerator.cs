using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using System.Linq;

public class TerrainGenerator : MonoBehaviour
{
    // HERE - organize this bunch of variables
    public int maxCircleCount = 30;
    public int maxDrillBaseN = 50;
    //---------- common vars --------public----
    public bool debugging = false;
    public bool useDebugUI = true;
    public bool useSeed = false;
    public int randomSeed = 42;
    public bool showcase = false;
    public bool renderBool = true;
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
    size = 128,
    updateCount = 0;
    [HideInInspector]
    public Texture2D shape_bmp;
    // [HideInInspector]
    // public Color[,] colors; // indexed as y,x
    //--- end of common vars ------------------
    // HERE - organize this bunch of variables
    private int multiplyingSteps = 3;
    private int alreadyMultiplied = 0;
    private int sizeMultiplier = 2;
    private int minDesiredLandSmallSize = 2000;
    private int maxDesiredLandSmallSize = 4000;
    private List<int[]> shapePoints = new List<int[]>();
    private int maxSmoothSteps = 1;
    private int maxResizedSmoothSteps = 4;
    private int maxBleedSteps = 6;
    private List<int[]> bleedPoints = new List<int[]>();
    private int numberOfTimesReset = 0;
    private string[] dataToSend = new string[5];

// main ---------------------------------------

    void _Start() 
    {
      if (useSeed) { Random.InitState(randomSeed); }
      shape_bmp = new Texture2D(size,size, TextureFormat.Alpha8, false);
      for(int i=0;i<size;i++) { for(int j=0;j<size;j++) { shape_bmp.SetPixel(i,j,Color.clear); } }
      if (showcase)
      {
        renderBool = true;
        shape_bmp.Apply();
      }
      if (renderBool)
      {
      Renderer rend = GetComponent<Renderer>();
        if (rend!=null) { rend.material.mainTexture = shape_bmp;
        } else { Debug.LogError("THERE IS NO Renderer IN THIS GAME OBJECT"); }
      } else { if (debugging) { Debug.Log("Not setting texture to renderer"); } }
    }

    void Update()
    {
      if (!finished)
      {
        updateCount++;
        switch(generation_phase)
        {
          case 0:  // Starting 
            if (auxValue == 0) { UIMan = GameObject.Find("/UI/Canvas").GetComponent<UIManager>(); }
            CallDebug("Starting TerrainGenerator.cs");
            _Start();
            generation_phase++;
            break;
          case 1:  // ** Generate Circles 
            CallDebug("generating initial circles");
            if (auxValue<maxCircleCount)
            {
              GenerateCircle();
              auxValue+=1;
            } else 
            {
              generation_phase++;
              auxValue=0;
            }
            Showcase();
            break;
          case 8:  // Smooth Island Borders
          case 2:  // Smooth Island Borders
            CallDebug("smoothing island borders");
            if (auxValue<maxSmoothSteps)
            {
              SmoothIslandBorders(3f,1f,0.5f,1.0f,0.5f); // (3ij + ipj + imj + ijp + ijm + (ipjp + imjp + ipjm + imjm)/2)/9 > 0.5f -> 1.0f; <=0.5f -> 0.0f
              auxValue++;
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            Showcase();
            break;
          case 9:  // Separate Islands
          case 3:  // Separate Islands
            CallDebug("separating islands");
            SeparateIslands();
            generation_phase++;
            Showcase();
            break;
          case 10: // Remove Extra Islands
          case 4:  // Remove Extra Islands
            CallDebug("removing extra islands");
            RemoveExtraIslands(auxValue);
            auxValue = 0;
            generation_phase++;
            Showcase();
            break;
          case 5:  // ** Bleed Island 
            CallDebug("bleeding island border");
            if (auxValue<maxBleedSteps)
            {
              auxValue++;
              BleedIslandWithDistanceToIsland(1f,1f,1f,auxValue,1f/3f);
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            Showcase();
            break;
          case 6:  // ** Drill Island Centered in Bleeds  
            CallDebug("drilling island");
            if (auxValue<maxDrillBaseN)
            {
              GenerateDrillCircle();
              auxValue++;
            } else 
            {
              generation_phase++;
              auxValue = 0;
            }
            Showcase();
            break;
          case 7:  // Remove Bleed and Reset Generation if nescessary according to land Size
            CallDebug("removing bleeded borders");
            shape_bmp = shape_bmp.ClearIfAlphaNot1();
            bleedPoints = new List<int[]>();
            generation_phase++;
            break;
          case 11: // ** BLEED FOR SMOOTHING DATA 
            CallDebug("bleeding borders for easier scaling and smoothing");
            int landSmallSize = shape_bmp.MeasureArea();
            if (alreadyMultiplied==0 && debugging) { Debug.Log("Size before resizing texture: "+landSmallSize); }
            if (alreadyMultiplied==0 && (landSmallSize<=minDesiredLandSmallSize || landSmallSize>=maxDesiredLandSmallSize))
            {
              GenerationReset();
              numberOfTimesReset++;
            } else 
            {
              if (debugging) { Debug.Log("Case: "+generation_phase+"."+alreadyMultiplied+", bleeding island border for smoothing data, round:"+(roundNumber+1).ToString()); }
              BleedIslandSimple(0f, 255, 100); // bleeding outer edge
              BleedIslandSimple(0f, 100, 100); // bleeding outer edge
              BleedIslandSimple(1f, 100, 200); // bleeding inner edge
              BleedIslandSimple(1f, 200, 200); // bleeding inner edge
              generation_phase++;
            }
            Showcase();
            break;
          case 12: // LOAD SHAPE DATA INTO LIST
            CallDebug("loading shape data into a list");
            LoadShapeDataIntoList();
            generation_phase++;
            Showcase();
            break;
          case 13: // LOAD LIST DATA INTO RESIZED SHAPE
            CallDebug("loading list data into resized shape");
            LoadListDataIntoNewSizedShape();
            generation_phase++;
            Showcase();
            break;
          case 14: // ** SMOOTH RESIZED SHAPE BORDERS 
            CallDebug("smoothing island's borders");
            if (auxValue<maxResizedSmoothSteps)
            {
              if (alreadyMultiplied==2)
              {
                BoxBlur(sizeMultiplier+2, 0.51f);
                auxValue++;
              } else { auxValue = maxResizedSmoothSteps; }
            } else 
            {
              generation_phase++;
              auxValue = 0;
              alreadyMultiplied++;
              if(alreadyMultiplied<multiplyingSteps)
              {
                generation_phase = 11;
                DebugMultiplications();
              }
            }
            Showcase();
            break;
          case 15: // saving generated TG
            if (_save)
            {
              CallDebug("saving generated biome into file");
              Save();
            }
            if (_continue)
            {
              CallDebug("Starting BiomeGenerator", false);
              CreateBiomeGeneratorScript();
            }
            generation_phase++;
            break;
          case 16: // Finished
            if (dataCollect)
            {
              // HERE - collect data
            }
            CallDebug("Finished Terrain (shape) Generation");
            finished = true;
            numberOfTimesReset = 0;
            generation_phase++;
            RenderFinal();
            break;
          default:
            CallDebug("Default - Last step - finished TerrainGenerator.cs");
            break;
        }
      } else 
      {
        if (destroyOnEnd) {
          if (debugging) { Debug.Log("Destroying this object's TerrainGenerator.cs script"); }
          StartCoroutine(finalCountdown());
          destroyOnEnd = false;
        }
      }
    }

// private methods ----------------------------
//         unique -----------------------------
    void BleedIslandSimple(float target, int neighbourValue, int newValue)
    { // here_ - ???
      List<int[]> auxList = new List<int[]>();
      for (int i=0; i<size; i++)
      {
        for (int j=0; j<size; j++)
        {
          if (shape_bmp.GetPixel(i,j).a == target)
          {
            int i_p = Mathf.Min(i+1,size-1);
            int i_m = Mathf.Max(i-1,0);
            int j_p = Mathf.Min(j+1,size-1);
            int j_m = Mathf.Max(j-1,0);
            List<float> neighbours = new List<float>();
            neighbours.Add(Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j).a*255)); //i+ j
            neighbours.Add(Mathf.RoundToInt(shape_bmp.GetPixel(i,j_p).a*255)); //i j+
            neighbours.Add(Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j).a*255)); //i- j
            neighbours.Add(Mathf.RoundToInt(shape_bmp.GetPixel(i,j_m).a*255)); //i j-
            neighbours.Add(Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j_p).a*255)); //i+ j+
            neighbours.Add(Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j_m).a*255)); //i- j-
            neighbours.Add(Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j_p).a*255)); //i- j+
            neighbours.Add(Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j_m).a*255)); //i+ j-
            if (neighbours.Contains(neighbourValue)) { auxList.Add(new int[2]{i,j}); }
          }
        }
      }
      foreach (int[] pair in auxList) { shape_bmp.SetPixel(pair[0],pair[1],new Color(1f,1f,1f,((float)newValue/255f))); }
    }
    void BleedIslandWithDistanceToIsland(float a, float b, float c, int d, float e)
    { 
      // similar to smooth but with some different rules: > -> >= ; ignores recently created values; uses (255-d)/255f as new alpha value
      // used in code as: BleedIsland(1f,1f,1f,auxValue,1f/3f); 
      d = Mathf.RoundToInt((float)d*(float)size/128f);
      for(int i=0;i<size;i++)
      {
        for(int j=0;j<size;j++)
        {
          if (shape_bmp.GetPixel(i,j).a==0f)
          {
  					int i_p = Mathf.Min (i + 1, size - 1);
					  int j_p = Mathf.Min (j + 1, size - 1);
            int i_m = Mathf.Max(i-1,0);
            int j_m = Mathf.Max(j-1,0);
            float newValue = (a*(Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255) > 255-d ? 1f : 0f )+ 
                              b*(Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j).a*255) > 255-d ? 1f : 0f ) + 
                              b*(Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j).a*255) > 255-d ? 1f : 0f ) + 
                              b*(Mathf.RoundToInt(shape_bmp.GetPixel(i,j_m).a*255) > 255-d ? 1f : 0f ) + 
                              b*(Mathf.RoundToInt(shape_bmp.GetPixel(i,j_p).a*255) > 255-d ? 1f : 0f ) + 
                              c*(Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j_p).a*255) > 255-d ? 1f : 0f ) + 
                              c*(Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j_m).a*255) > 255-d ? 1f : 0f ) + 
                              c*(Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j_p).a*255) > 255-d ? 1f : 0f ) + 
                              c*(Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j_m).a*255) > 255-d ? 1f : 0f ))/(a+4*b+4*c);
            if (newValue >= e)
            {
              shape_bmp.SetPixel(i,j,new Color(1,1,1,((255f-d)/255f)));
              bleedPoints.Add(new int[2]{i,j});
            } else { shape_bmp.SetPixel(i,j,Color.clear); }
          }
        }
      }
    }
    void BoxBlur(int k, float treshold)
    {
      List<int[]> auxList = new List<int[]>();
      foreach (int[] pair in bleedPoints)
      {
        float boxValue = 0f;
        for (int i=-k; i<=k; i++)
        {
          for (int j=-k; j<=k; j++)
          {
            if (pair[0]+i>=0 && pair[0]+i<size && pair[1]+j>=0 && pair[1]+j<size)
            {
              float kernel = 1f/((2*k+1)*(2*k+1));
              boxValue+=shape_bmp.GetPixel(pair[0]+i,pair[1]+j).a*kernel;
            }
          }
        }
        int intBoxValue = 0;
        if (boxValue>treshold) { intBoxValue= 255; }
        auxList.Add(new int[3]{pair[0],pair[1],intBoxValue});
      }
      foreach (int[] info in auxList)
      {
        Color newColor = new Color(1f,1f,1f,(float)info[2]/255f);
        shape_bmp.SetPixel(info[0],info[1],newColor);
      }
    }
    void CreateBiomeGeneratorScript()
    {
      BiomeGenerator BGscript = gameObject.AddComponent<BiomeGenerator>() as BiomeGenerator;
      BGscript.debugging = debugging;
      BGscript.useDebugUI = useDebugUI;
      BGscript.useSeed = useSeed;
      BGscript.randomSeed = randomSeed;
      BGscript.showcase = showcase;
      BGscript.renderBool = renderBool;
      BGscript.UIMan = UIMan;
      BGscript.dataCollect = dataCollect;
      BGscript.size = size;
      BGscript.shape_bmp = shape_bmp.Equal().TexChangeFormat(TextureFormat.ARGB32);
      BGscript._save = _save;
      BGscript._continue = _continue;

      BGscript.generation_phase = 1;
    }
    void DebugMultiplications()
    {
      string auxText = "th";
      switch(alreadyMultiplied)
      {
        case 1:
          auxText = "st";
          break;
        case 2:
          auxText = "nd";
          break;
        case 3:
          auxText = "rd";
          break;
      }
      if (debugging) { Debug.Log("rebooting to step="+generation_phase+", "+alreadyMultiplied+auxText+" time"); }
    }
    void GenerationReset()
    {
      roundNumber++;
      if (debugging) { Debug.Log("Restarting TerrainGenerator.cs"); }
      size = 128;
      alreadyMultiplied = 0;
      shapePoints = new List<int[]>();
      generation_phase = 1;
      auxValue = 0;
      finished = false;
      bleedPoints = new List<int[]>();
      shape_bmp = new Texture2D(size,size, TextureFormat.Alpha8, false);
      for(int i=0;i<size;i++) { for(int j=0;j<size;j++) { shape_bmp.SetPixel(i,j,Color.clear); } }
      if (renderBool)
      {
        Renderer rend = GetComponent<Renderer>();
        if (rend!= null) { rend.material.mainTexture = shape_bmp;
        } else { Debug.LogError("THERE IS NO Renderer IN THIS GAME OBJECT"); }
      } else { if (debugging) { Debug.Log("Not setting texture to renderer"); } }
      if (renderBool) { shape_bmp.Apply(); }
    }
    void GenerateCircle()
    {  
      Vector2Int point = new Vector2Int();
			float distanceFromTextureCenter = (0.8f*Random.value + 0.2f)*size/2f;
			float initialPointDirection = Random.value*2*Mathf.PI;
      point.x = Mathf.RoundToInt (size/2 + distanceFromTextureCenter*Mathf.Cos(initialPointDirection));
      point.y = Mathf.RoundToInt (size/2 + distanceFromTextureCenter*Mathf.Sin(initialPointDirection));
      int r1 = Mathf.RoundToInt(0.5f*((3.0f+2.0f*((float)size/128f))*Random.value + 2.0f*((float)size/128f))*((float)size/distanceFromTextureCenter));
      int minPerturbationRadius = Mathf.Max(1 ,r1 - Mathf.RoundToInt(1.0f + 3.0f*Random.value));
      for(int i=-r1; i<=r1; i++)
      {
        for(int j=-r1; j<=r1; j++)
        {
          if (i*i+j*j<=r1*r1)
          {
            Vector2Int newPoint = new Vector2Int(point.x+i,point.y+j);
            if (i*i+j*j<=minPerturbationRadius*minPerturbationRadius) 
            { shape_bmp.SetPixel(newPoint.x,newPoint.y,Color.white); } 
            else { if (Random.value<0.26f) 
            { shape_bmp.SetPixel(newPoint.x,newPoint.y,Color.white); } }
          }
        }
      }
    }
    void GenerateDrillCircle()
    {
      // should it be perimeter dependant?
      int pointIndex = Mathf.RoundToInt(Random.Range(0, bleedPoints.Count));
      int[] point = bleedPoints[pointIndex];
      bleedPoints.RemoveAt(pointIndex);
      int radius = 255 - Mathf.RoundToInt(shape_bmp.GetPixel(point[0],point[1]).a*255);
      if (radius == 254) { radius = 1; }
      radius = radius + Mathf.RoundToInt((1f + 2f*Random.value)*(float)size/128f);
      int minPerturbationRadius = Mathf.Min(radius-1, radius - Mathf.RoundToInt(3.0f*Random.value));
      int alphaValue = 255 - Mathf.Max(1, 1*Mathf.RoundToInt((float)size/128f));
      for(int i=-radius; i<=radius; i++)
      {
        for(int j=-radius; j<=radius; j++)
        {
          int currentRadius = Mathf.RoundToInt(Mathf.Sqrt(i*i+j*j));
          if(currentRadius<=radius)
          {
            if (currentRadius<=minPerturbationRadius) 
            { 
              if (shape_bmp.GetPixel(point[0]+i, point[1]+j).a==1f) 
              { shape_bmp.SetPixel(point[0]+i, point[1]+j, new Color(1,1,1,1f/255f)); } 
            } else { if (Random.value<0.2f) { 
              if (shape_bmp.GetPixel(point[0]+i, point[1]+j).a==1f) 
              { shape_bmp.SetPixel(point[0]+i, point[1]+j, new Color(1,1,1,1f/255f)); } 
            } }
          }
        }
      }
    }
    void LoadListDataIntoNewSizedShape()
    {
      size = sizeMultiplier*size;
      shape_bmp.Resize(size,size);
      for (int i=0; i<size; i++) { for (int j=0; j<size; j++) { shape_bmp.SetPixel(i,j,Color.clear); } }
      List<int[]> auxList = new List<int[]>();
      foreach(int[] pair in shapePoints) { for (int i=0; i<sizeMultiplier; i++) { for (int j=0; j<sizeMultiplier; j++) 
      { 
        shape_bmp.SetPixel(pair[0]*sizeMultiplier+i,pair[1]*sizeMultiplier+j,Color.white); 
      } } }
      foreach(int[] pair in bleedPoints) { for (int i=0; i<sizeMultiplier; i++) { for (int j=0; j<sizeMultiplier; j++) 
      { 
        auxList.Add(new int[2]{pair[0]*sizeMultiplier+i,pair[1]*sizeMultiplier+j}); 
      } } }
      bleedPoints = auxList;
      shapePoints =  new List<int[]>();
    }
    void LoadShapeDataIntoList()
    {
      for (int i=0; i<size; i++)
      {
        for (int j=0; j<size; j++)
        {
          if (Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255)==255) { shapePoints.Add(new int[2]{i,j}); } 
          else 
          {
            if (Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255)==200)
            {
              shapePoints.Add(new int[2]{i,j});
              bleedPoints.Add(new int[2]{i,j});
            } else { if (Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255)==100) { bleedPoints.Add(new int[2]{i,j}); } }
          }
        }
      }
    }
    void RemoveExtraIslands(int biggerIslandAlpha)
    {
      for(int i=0;i<size;i++)
      {
        for(int j=0;j<size;j++)
        {
          if (Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255)!=biggerIslandAlpha)
          { shape_bmp.SetPixel(i,j,Color.clear); }
          else 
          { shape_bmp.SetPixel(i,j,Color.white); }
        }
      }
    }
    void SeparateIslands()
    {
      List<int> islandAlphas = new List<int>(); // float
      List<int> islandSizes = new List<int>(); // ushort
      for (int i=254; i>0; i--)
      {
        islandAlphas.Add(i);
        islandSizes.Add(0);
      }
      int listPointer = 0;
      for(int i=0;i<size;i++)
      { // j goes up i goes right
        for(int j=0;j<size;j++)
        {
          if (shape_bmp.GetPixel(i,j).a==1f)
          {
            int neighbourCount = 0;
            int firstNeighbour = 0;
            int i_m = Mathf.Max(i-1,0);
            int j_m = Mathf.Max(j-1,0);
            int[] neighbourValues = new int[2] {Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j).a*255),Mathf.RoundToInt(shape_bmp.GetPixel(i,j_m).a*255)};
            foreach (int value in neighbourValues)
            {
              List<int> auxList = islandAlphas.GetRange(0,listPointer);
              if (auxList.Contains(value))
              {
                neighbourCount++;
                if (firstNeighbour==0) { firstNeighbour=value; }
              }
            }
            switch (neighbourCount)
            {
              case 0: 
                islandSizes[listPointer]++;
                shape_bmp.SetPixel(i,j,new Color(1,1,1,islandAlphas[listPointer]/255f));
                listPointer++;
                break;
              case 1: 
                int auxListPointer = islandAlphas.IndexOf(firstNeighbour);
                islandSizes[auxListPointer]++;
                shape_bmp.SetPixel(i,j,new Color(1,1,1,firstNeighbour/255f));
                break;
              case 2:
                int auxListPointer_0 = islandAlphas.IndexOf(neighbourValues[0]);
                int auxListPointer_1 = islandAlphas.IndexOf(neighbourValues[1]);
                islandSizes[auxListPointer_0]++;
                shape_bmp.SetPixel(i,j,new Color(1,1,1,neighbourValues[0]/255f));
                if (neighbourValues[0]!=neighbourValues[1])
                {
                  for (int i2=0;i2<=i;i2++)
                  {
                    for (int j2=0;j2<size;j2++)
                    {
                      if (Mathf.RoundToInt(shape_bmp.GetPixel(i2,j2).a*255)==neighbourValues[1])
                      {
                        islandSizes[auxListPointer_0]++;
                        islandSizes[auxListPointer_1]--;
                        if (islandSizes[auxListPointer_1]==0)
                        {
                          islandSizes.Add(0);
                          islandAlphas.Add(islandAlphas[auxListPointer_1]);
                          islandSizes.RemoveAt(auxListPointer_1);
                          islandAlphas.RemoveAt(auxListPointer_1);
                          listPointer--;
                        }
                        shape_bmp.SetPixel(i2,j2,new Color(1,1,1,neighbourValues[0]/255f));
                      }
                    }
                  }
                }
                break;
            }
          }
        }
      }
      auxValue = islandAlphas[System.Array.IndexOf(islandSizes.ToArray(),islandSizes.Max())];
    }
    void SmoothIslandBorders(float a, float b, float c, float d, float e)
    { 
      // smoothes island borders by taking the average pixel alpha value of point i,j and its neighbours weighted by 'a', 'b' and 'c' where a weights the point, 'b' weights its axis neighbours and 'c' weights its diagonal neighbours. if the average is higher than 'e' the point receives the new value of 'd' in its alpha, otherwise it receives 0 in its alpha
      for(int i=0;i<size;i++)
      {
        for(int j=0;j<size;j++)
        {
          int i_p = Mathf.Min (i + 1, size - 1);
          int j_p = Mathf.Min (j + 1, size - 1);
          int i_m = Mathf.Max(i-1,0);
          int j_m = Mathf.Max(j-1,0);
          float newValue = (a*shape_bmp.GetPixel(i,j).a + b*shape_bmp.GetPixel(i_m,j).a + b*shape_bmp.GetPixel(i_p,j).a + b*shape_bmp.GetPixel(i,j_m).a + b*shape_bmp.GetPixel(i,j_p).a + c*shape_bmp.GetPixel(i_p,j_p).a + c*shape_bmp.GetPixel(i_m,j_m).a + c*shape_bmp.GetPixel(i_m,j_p).a + c*shape_bmp.GetPixel(i_p,j_m).a)/(a+4*b+4*c);
          if (newValue > e) { shape_bmp.SetPixel(i,j,new Color(1,1,1,d)); } 
          else { shape_bmp.SetPixel(i,j,Color.clear); }
        }
      }
    }
//         common -----------*normalize-these--
    void CallDebug(string text)
    {
      if (debugging) { Debug.Log("Case: "+generation_phase+"."+auxValue+"."+roundNumber+", "+text); }
      if (useDebugUI) { UIMan.SetPDebugText(updateCount.ToString("D5")+" TG"+generation_phase.ToString("D3")+"."+auxValue.ToString("D4")+"."+roundNumber.ToString("D2")+", "+text); }
    }
    void CallDebug(string text, bool primary)
    {
      if (debugging) { Debug.Log("Case: "+generation_phase+"."+auxValue+"."+roundNumber+", "+text); }
      if (useDebugUI)
      {
        if (primary) { UIMan.SetPDebugText(updateCount.ToString("D5")+" TG"+generation_phase.ToString("D3")+"."+auxValue.ToString("D4")+"."+roundNumber.ToString("D2")+", "+text); } 
        else { UIMan.SetSDebugText(updateCount.ToString("D5")+" TG"+generation_phase.ToString("D3")+"."+auxValue.ToString("D4")+"."+roundNumber.ToString("D2")+", "+text); }
      }
    }
    void RenderFinal() { if (renderBool) { shape_bmp.Apply(); } }
    void Save()
    {
      int n = 0;
      string path = Application.dataPath+"/Data/SavedGen/TG/SavedTG.txt";
      if (!File.Exists(path)) { File.WriteAllText(path,"1"); } 
      else 
      {
        string dataRead = File.ReadAllText(path);
        n = int.Parse(dataRead);
        File.WriteAllText(path,(n+1).ToString());
      }
      byte[] bytes = shape_bmp.EncodeToPNG();
      File.WriteAllBytes(Application.dataPath + "/Data/SavedGen/TG/_png/SavedTG_"+n+".png", bytes);
    }
    void Showcase() { if (showcase) { shape_bmp.Apply(); } }

    IEnumerator finalCountdown()
    {
      if (useDebugUI)
      {
        UIMan.SetSDebugText(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 5");
        yield return new WaitForSeconds(1);
        UIMan.SetSDebugText(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 4");
        yield return new WaitForSeconds(1);
        UIMan.SetSDebugText(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 3");
        yield return new WaitForSeconds(1);
        UIMan.SetSDebugText(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 2");
        yield return new WaitForSeconds(1);
        UIMan.SetSDebugText(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 1");
        yield return new WaitForSeconds(1);
        if (!_continue) { UIMan.SetPDebugText(""); }
        UIMan.SetSDebugText("");
      }
      TerrainGenerator script = GetComponent<TerrainGenerator>();
      Destroy(script);
    }
// not in use ---------------------------------
//         unique -----------------------------
    void GenerateSinusoidalCircle()
    {
      Vector2Int point = new Vector2Int();
			float distanceFromTextureCenter = (0.8f*Random.value + 0.2f)*size/2f;
			float initialPointDirection = Random.value*2*Mathf.PI;
      point.x = Mathf.RoundToInt (size/2 + distanceFromTextureCenter*Mathf.Cos(initialPointDirection));
      point.y = Mathf.RoundToInt (size/2 + distanceFromTextureCenter*Mathf.Sin(initialPointDirection));
      int circleRadius = Mathf.RoundToInt(0.5f*((3.0f+2.0f*((float)size/128f))*Random.value + 2.0f*((float)size/128f))*((float)size/distanceFromTextureCenter));
      
      int r1 = circleRadius;
      int r2 = Random.Range(1,2);
      int k = Random.Range(15,30);
      for (int i=-r1-r2-1; i<=r1+r2+1; i++)
      {
        for (int j=-r1-r2-1; j<=r1+r2+1; j++)
        {
          float radius = (float)r1+(float)r2*Mathf.Sin(Mathf.Atan2(j,i)*k);
          if (i*i+j*j<=radius*radius)
          {
            Vector2Int newPoint = new Vector2Int(Mathf.Min(Mathf.Max(0,point.x+i),size-1),Mathf.Min(Mathf.Max(0,point.y+j),size-1));
            shape_bmp.SetPixel(newPoint.x,newPoint.y,Color.white);
          }
        }
      }
    } // NOT IN USE
    void GenerateSinusoidalDrillCircle()
    {
      // should it be perimeter dependant?
      int pointIndex = Mathf.RoundToInt(Random.Range(0, bleedPoints.Count));
      int[] point = bleedPoints[pointIndex];
      bleedPoints.RemoveAt(pointIndex);
      int r1 = 255 - Mathf.RoundToInt(shape_bmp.GetPixel(point[0],point[1]).a*255);
      if (r1 == 254) { r1 = 1; }
      r1 = r1+1+Random.Range(0,1);
      int r2 = 1+Random.Range(0,1);
      if (r1<3) { r2 = 0; }
      int k = Random.Range(5,15);
      int alphaValue = 255 - Mathf.Max(1, 1*Mathf.RoundToInt((float)size/128f));
      for (int i=-r1-r2; i<=r1+r2; i++)
      {
        for (int j=-r1-r2; j<=r1+r2; j++)
        {
          float radius = (float)r1+(float)r2*Mathf.Sin(Mathf.Atan2(j,i)*k);
          if (i*i+j*j<=radius*radius)
          {
            if (shape_bmp.GetPixel(point[0]+i, point[1]+j).a==1f)
            { shape_bmp.SetPixel(point[0]+i, point[1]+j, new Color(1,1,1,1f/255f)); }
          }
        }
      }
    } // NOT IN USE
    void SmoothResizedIslandBorders(float a, float b, float c, bool d)
    {
      // commonly used with the following values: SmoothResizedIslandBorders(1f,0.71f,3.4f,true);
      List<int[]> auxListWhite = new List<int[]>();
      List<int[]> auxListClear = new List<int[]>();
      foreach (int[] pair in bleedPoints)
      {
        int i = pair[0];
        int j = pair[1];
        if (shape_bmp.GetPixel(i,j).a == 0f)
        {
          int i_p = Mathf.Min(i+1,size-1);
          int i_m = Mathf.Max(i-1,0);
          int j_p = Mathf.Min(j+1,size-1);
          int j_m = Mathf.Max(j-1,0);
          float neighbours =  a*shape_bmp.GetPixel(i_p,j).a +   // i+j0
                              a*shape_bmp.GetPixel(i_m,j).a +   // i0j+
                              a*shape_bmp.GetPixel(i,j_p).a +   // i-j0
                              a*shape_bmp.GetPixel(i,j_m).a +   // i0j-
                              b*shape_bmp.GetPixel(i_p,j_p).a + // i+j+
                              b*shape_bmp.GetPixel(i_m,j_m).a + // i-j-
                              b*shape_bmp.GetPixel(i_m,j_p).a + // i+j-
                              b*shape_bmp.GetPixel(i_p,j_m).a;  // i-j+
          if (neighbours >= c) { auxListWhite.Add(new int[2]{i,j}); } 
          else 
          {
            if (neighbours == b && d == true)
            {
              int[] newPair = new int[2];
              if (shape_bmp.GetPixel(i_p,j_p).a == 1f) {newPair = new int[2]{i_p,j_p};}
              if (shape_bmp.GetPixel(i_m,j_m).a == 1f) {newPair = new int[2]{i_m,j_m};}
              if (shape_bmp.GetPixel(i_p,j_m).a == 1f) {newPair = new int[2]{i_p,j_m};}
              if (shape_bmp.GetPixel(i_m,j_p).a == 1f) {newPair = new int[2]{i_m,j_p};}
              auxListClear.Add(newPair);
            }
          }
        }
      }
      foreach (int[] pair in auxListClear) { shape_bmp.SetPixel(pair[0],pair[1],Color.clear); }
      foreach (int[] pair in auxListWhite) { shape_bmp.SetPixel(pair[0],pair[1],Color.white); }
    } // NOT IN USE
//         common -----------------------------
// public methods -----------------------------
    public void CallReset() { GenerationReset(); }
}