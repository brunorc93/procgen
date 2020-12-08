using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

public class TerrainGenerator : MonoBehaviour
{
    private int size = 128;
    public bool debugging = true;
    private bool destroyOnEnd = true;
    private int roundNumber = 0;
    private int multiplyingSteps = 3;
    private int alreadyMultiplied = 0;
    private int sizeMultiplier = 2;
    private int minDesiredLandSmallSize = 2000;
    private int maxDesiredLandSmallSize = 4000;
    private Texture2D shape_bmp;
    private List<int[]> shapePoints = new List<int[]>();
    private int generation_phase = -1;
    public int maxCircleCount = 30;
    private int auxValue = 0;
    private int maxSmoothSteps = 1;
    [System.NonSerialized] // X
    private int maxResizedSmoothSteps = 4;
    [System.NonSerialized] // X
    private int maxBleedSteps = 6;
    private bool finished = false;
    public int maxDrillBaseN = 50;
    private List<int[]> bleedPoints = new List<int[]>();
    public bool useSeed = false;
    public int randomSeed = 42;
    public bool showcase = false;
    private bool dataCollect = false;
    private int numberOfTimesReset = 0;
    private string[] dataToSend = new string[5];
    public bool renderBool = true;
    private bool canStart = false;

    void RealStart() {
      if (useSeed){
        Random.InitState(randomSeed);
      }
      shape_bmp = new Texture2D(size,size, TextureFormat.Alpha8, false);
      for(int i=0;i<size;i++){
        for(int j=0;j<size;j++){
          shape_bmp.SetPixel(i,j,Color.clear);
        }
      }
      if (showcase){
        renderBool = true;
        shape_bmp.Apply();
      }
      if (renderBool){
      Renderer rend = GetComponent<Renderer>();
        if (rend!=null){
          rend.material.mainTexture = shape_bmp;
        } else {
          Debug.LogError("THERE IS NO Renderer IN THIS GAME OBJECT");
        }
      } else {
        if (debugging){
          Debug.Log("Not setting texture to renderer");
        }
      }
    }

    void Update()  {
      if (!finished){
        switch(generation_phase) {
          case -1: // Starting
            if (debugging){
              Debug.Log("Case: "+generation_phase+",Starting TerrainGenerator.cs, round:"+(roundNumber+1).ToString());
            }
            RealStart();
            generation_phase++;
            break;
          case 0: // Generate Circles
            if (debugging){
              Debug.Log("Case: "+generation_phase+", generating initial circles, round:"+(roundNumber+1).ToString());
            }
            if (auxValue<maxCircleCount){
              GenerateCircle();
              if (showcase){
                shape_bmp.Apply();
              }
              auxValue+=1;
            } else {
              generation_phase++;
              auxValue=0;
            }
            break;
          case 7: // Smooth Island Borders
          case 1: // Smooth Island Borders
            if (debugging){
              Debug.Log("Case: "+generation_phase+", smoothing island borders, round:"+(roundNumber+1).ToString());
            }
            if (auxValue<maxSmoothSteps){
              SmoothIslandBorders(3f,1f,0.5f,1.0f,0.5f); // (3ij + ipj + imj + ijp + ijm + (ipjp + imjp + ipjm + imjm)/2)/9 > 0.5f -> 1.0f; <=0.5f -> 0.0f
              if (showcase){
                shape_bmp.Apply();
              }
              auxValue++;
            } else {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 8: // Separate Islands
          case 2: // Separate Islands
            if (debugging){
              Debug.Log("Case: "+generation_phase+", separating islands, round:"+(roundNumber+1).ToString());
            }
            SeparateIslands();
              if (showcase){
                shape_bmp.Apply();
              }
            generation_phase++;
            break;
          case 9: // Remove Extra Islands
          case 3: // Remove Extra Islands
            if (debugging){
              Debug.Log("Case: "+generation_phase+", removing extra islands, round:"+(roundNumber+1).ToString());
            }
            RemoveExtraIslands(auxValue);
            if (showcase){
              shape_bmp.Apply();
            }
            auxValue = 0;
            generation_phase++;
            break;
          case 4: // Bleed Island
            if (debugging){
              Debug.Log("Case: "+generation_phase+", bleeding island border, round:"+(roundNumber+1).ToString());
            }
            if (auxValue<maxBleedSteps){
              auxValue++;
              BleedIslandWithDistanceToIsland(1f,1f,1f,auxValue,1f/3f); 
              if (showcase){
                shape_bmp.Apply();
              }
            } else {
              generation_phase++;
              auxValue = 0;
            }  
            break;
          case 5: // Drill Island Centered in Bleeds
            if (debugging){
              Debug.Log("Case: "+generation_phase+", drilling island, round:"+(roundNumber+1).ToString());
            }
            if (auxValue<maxDrillBaseN){
              GenerateDrillCircle();
              if (showcase){
                shape_bmp.Apply();
              }
              auxValue++;
            } else {
              generation_phase++;
              auxValue = 0;
            }
            break;
          case 6: // Remove Bleed and Reset Generation if nescessary according to land Size
            if (debugging){
              Debug.Log("Case: "+generation_phase+", removing bleeded borders, round:"+(roundNumber+1).ToString());
            }
            RemoveBleed();
            if (showcase){
              shape_bmp.Apply();
            }
            generation_phase++;
            break;
          case 10:// BLEED FOR SMOOTHING DATA          
            int landSmallSize = MeasureArea();
            if (alreadyMultiplied==0 && debugging){
              Debug.Log("Size before resizing texture: "+landSmallSize);
              if (dataCollect){
                dataToSend[2] = landSmallSize.ToString();
              }
            }
            if (alreadyMultiplied==0 && (landSmallSize<=minDesiredLandSmallSize || landSmallSize>=maxDesiredLandSmallSize)){
              GenerationReset();
              numberOfTimesReset++;
            } else {
              if (debugging){
                Debug.Log("Case: "+generation_phase+"."+alreadyMultiplied+", bleeding island border for smoothing data, round:"+(roundNumber+1).ToString());
              }
              BleedIslandSimple(0f, 255, 100); // bleeding outer edge
              BleedIslandSimple(0f, 100, 100); // bleeding outer edge
              BleedIslandSimple(1f, 100, 200); // bleeding inner edge
              BleedIslandSimple(1f, 200, 200); // bleeding inner edge
              if (showcase){
                shape_bmp.Apply();
              }
              generation_phase++;
            }
            break;
          case 11:// LOAD SHAPE DATA INTO LIST
            if (debugging){
              Debug.Log("Case: "+generation_phase+"."+alreadyMultiplied+", loading shape data into a list, round:"+(roundNumber+1).ToString());
            }
            LoadShapeDataIntoList();
            if (showcase){
              shape_bmp.Apply();
            }
            generation_phase++;
            break;
          case 12:// LOAD LIST DATA INTO RESIZED SHAPE
            if (debugging){
              Debug.Log("Case: "+generation_phase+"."+alreadyMultiplied+", loading list data into resized shape, round:"+(roundNumber+1).ToString());
            }
            LoadListDataIntoNewSizedShape();
            if (showcase){
              shape_bmp.Apply();
            }
            generation_phase++;
            break;
          case 13:// SMOOTH RESIZED SHAPE BORDERS
            if (debugging){
              Debug.Log("Case: "+generation_phase+"."+alreadyMultiplied+", smoothing island borders, round:"+(roundNumber+1).ToString());
            }
            if (auxValue<maxResizedSmoothSteps){
              if (alreadyMultiplied==2){
                BoxBlur(sizeMultiplier+2, 0.51f);
                if (showcase){
                  shape_bmp.Apply();
                }
                auxValue++;
              } else {
                auxValue = maxResizedSmoothSteps;
              }
            } else {
              generation_phase++;
              auxValue = 0;
              alreadyMultiplied++;
              if(alreadyMultiplied<multiplyingSteps){
                generation_phase = 10;
                string auxText = "th";
                switch(alreadyMultiplied){
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
                if (debugging){
                  Debug.Log("rebooting to step="+generation_phase+", "+alreadyMultiplied+auxText+" time");
                }
              }
            }
            break;
          case 14:// Finished
            if (debugging){
              Debug.Log("Case: "+generation_phase+", finished TerrainGenerator.cs, round:"+(roundNumber+1).ToString());
            }
            if (dataCollect){
              if (debugging){
                Debug.Log("Sending Data, round:"+(roundNumber+1).ToString());
              }
              if (numberOfTimesReset!=0){
                dataToSend[0]="YES";
              } else {
                dataToSend[0]="NO";
              }
              dataToSend[1] = numberOfTimesReset.ToString();
              int landSize = MeasureArea();
              int landPerimeter = MeasurePerimeter();
              dataToSend[3] = landSize.ToString();
              dataToSend[4] = landPerimeter.ToString();
              string dataText = string.Join(",", dataToSend);
              TGDataCollector TGDCscript = this.transform.parent.GetComponent<TGDataCollector>();
              if (TGDCscript!=null){
                TGDCscript.SetData(dataText);
              } else {
                Debug.LogError("THERE IS NO TGDataCollector IN THIS GAME OBJECT");
              }
            }
            finished = true;
            numberOfTimesReset = 0;
            if (renderBool){
              shape_bmp.Apply();
            }
            generation_phase++;
            break;
          default:
            if (debugging){
              Debug.Log("Default - Last step - finished TerrainGenerator.cs, round:"+(roundNumber+1).ToString());
            }
            break;
        }
      } else {
        if (destroyOnEnd){
          if (debugging){
            Debug.Log("Destroying this object's TerrainGenerator.cs script, round:"+(roundNumber+1).ToString());
          }
          TerrainGenerator script = GetComponent<TerrainGenerator>();
          Destroy(script);
        }
      }
    }

    void BleedIslandSimple(float target, int neighbourValue, int newValue){ // HERE
      List<int[]> auxList = new List<int[]>();
      for (int i=0; i<size; i++){
        for (int j=0; j<size; j++){
          if (shape_bmp.GetPixel(i,j).a == target){
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
            if (neighbours.Contains(neighbourValue)){
              auxList.Add(new int[2]{i,j});
            }
          }
        }
      }
      foreach (int[] pair in auxList){
        shape_bmp.SetPixel(pair[0],pair[1],new Color(1f,1f,1f,((float)newValue/255f)));
      }
    }   
    void BleedIslandWithDistanceToIsland(float a, float b, float c, int d, float e){ 
      // similar to smooth but with some different rules: > -> >= ; ignores recently created values; uses (255-d)/255f as new alpha value
      // used in code as: BleedIsland(1f,1f,1f,auxValue,1f/3f); 
      d = Mathf.RoundToInt((float)d*(float)size/128f);
      for(int i=0;i<size;i++){
        for(int j=0;j<size;j++){
          if (shape_bmp.GetPixel(i,j).a==0f){
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
            if (newValue >= e){
  						shape_bmp.SetPixel(i,j,new Color(1,1,1,((255f-d)/255f)));
              bleedPoints.Add(new int[2]{i,j});
					  } else {
  						shape_bmp.SetPixel(i,j,Color.clear);
            }
          }
        }
      }
    }
    void BoxBlur(int k, float treshold){
      List<int[]> auxList = new List<int[]>();
      foreach (int[] pair in bleedPoints){
        float boxValue = 0f;
        for (int i=-k; i<=k; i++){
          for (int j=-k; j<=k; j++){
            if (pair[0]+i>=0 && pair[0]+i<size && pair[1]+j>=0 && pair[1]+j<size){
              float kernel = 1f/((2*k+1)*(2*k+1));
              boxValue+=shape_bmp.GetPixel(pair[0]+i,pair[1]+j).a*kernel;
            }
          }
        }
        int intBoxValue = 0;
        if (boxValue>treshold){
          intBoxValue= 255;
        }
        auxList.Add(new int[3]{pair[0],pair[1],intBoxValue});
      }
      foreach (int[] info in auxList){
        Color newColor = new Color(1f,1f,1f,(float)info[2]/255f);
        shape_bmp.SetPixel(info[0],info[1],newColor);
      }
    }
    public void CallReset(){
      GenerationReset();
    }
    void GenerateSinusoidalCircle(){ // NOT IN USE
      Vector2Int point = new Vector2Int();
			float distanceFromTextureCenter = (0.8f*Random.value + 0.2f)*size/2f;
			float initialPointDirection = Random.value*2*Mathf.PI;
      point.x = Mathf.RoundToInt (size/2 + distanceFromTextureCenter*Mathf.Cos(initialPointDirection));
      point.y = Mathf.RoundToInt (size/2 + distanceFromTextureCenter*Mathf.Sin(initialPointDirection));
      int circleRadius = Mathf.RoundToInt(0.5f*((3.0f+2.0f*((float)size/128f))*Random.value + 2.0f*((float)size/128f))*((float)size/distanceFromTextureCenter));
      
      int r1 = circleRadius;
      int r2 = Random.Range(1,2);
      int k = Random.Range(15,30);
      for (int i=-r1-r2-1; i<=r1+r2+1; i++){
        for (int j=-r1-r2-1; j<=r1+r2+1; j++){
          float radius = (float)r1+(float)r2*Mathf.Sin(Mathf.Atan2(j,i)*k);
          if (i*i+j*j<=radius*radius){
            Vector2Int newPoint = new Vector2Int(Mathf.Min(Mathf.Max(0,point.x+i),size-1),Mathf.Min(Mathf.Max(0,point.y+j),size-1));
            shape_bmp.SetPixel(newPoint.x,newPoint.y,Color.white);
          }
        }
      }
    }
    void GenerateCircle(){  
      Vector2Int point = new Vector2Int();
			float distanceFromTextureCenter = (0.8f*Random.value + 0.2f)*size/2f;
			float initialPointDirection = Random.value*2*Mathf.PI;
      point.x = Mathf.RoundToInt (size/2 + distanceFromTextureCenter*Mathf.Cos(initialPointDirection));
      point.y = Mathf.RoundToInt (size/2 + distanceFromTextureCenter*Mathf.Sin(initialPointDirection));
      int r1 = Mathf.RoundToInt(0.5f*((3.0f+2.0f*((float)size/128f))*Random.value + 2.0f*((float)size/128f))*((float)size/distanceFromTextureCenter));
      int minPerturbationRadius = Mathf.Max(1 ,r1 - Mathf.RoundToInt(1.0f + 3.0f*Random.value));
      for(int i=-r1; i<=r1; i++){
        for(int j=-r1; j<=r1; j++){
          if (i*i+j*j<=r1*r1){
            Vector2Int newPoint = new Vector2Int(point.x+i,point.y+j);
            if (i*i+j*j<=minPerturbationRadius*minPerturbationRadius){
              shape_bmp.SetPixel(newPoint.x,newPoint.y,Color.white);
            } else {
              float perturbation = Random.value;
              if (perturbation<0.26f){
                shape_bmp.SetPixel(newPoint.x,newPoint.y,Color.white);
              }
            }
          }
        }
      }
    }
    void GenerateSinusoidalDrillCircle(){ // NOT IN USE
      // should it be perimeter dependant?
      int pointIndex = Mathf.RoundToInt(Random.Range(0, bleedPoints.Count));
      int[] point = bleedPoints[pointIndex];
      bleedPoints.RemoveAt(pointIndex);
      int r1 = 255 - Mathf.RoundToInt(shape_bmp.GetPixel(point[0],point[1]).a*255);
      if (r1 == 254){
        r1 = 1;
      }
      r1 = r1+1+Random.Range(0,1);
      int r2 = 1+Random.Range(0,1);
      if (r1<3){
        r2 = 0;
      }
      int k = Random.Range(5,15);
      int alphaValue = 255 - Mathf.Max(1, 1*Mathf.RoundToInt((float)size/128f));
      for (int i=-r1-r2; i<=r1+r2; i++){
        for (int j=-r1-r2; j<=r1+r2; j++){
          float radius = (float)r1+(float)r2*Mathf.Sin(Mathf.Atan2(j,i)*k);
          if (i*i+j*j<=radius*radius){
            if (shape_bmp.GetPixel(point[0]+i, point[1]+j).a==1f){
              shape_bmp.SetPixel(point[0]+i, point[1]+j, new Color(1,1,1,1f/255f));
            }          
          }          
        }
      }
    }
    void GenerateDrillCircle(){
      // should it be perimeter dependant?
      int pointIndex = Mathf.RoundToInt(Random.Range(0, bleedPoints.Count));
      int[] point = bleedPoints[pointIndex];
      bleedPoints.RemoveAt(pointIndex);
      int radius = 255 - Mathf.RoundToInt(shape_bmp.GetPixel(point[0],point[1]).a*255);
      if (radius == 254){
        radius = 1;
      }
      radius = radius + Mathf.RoundToInt((1f + 2f*Random.value)*(float)size/128f);
      int minPerturbationRadius = Mathf.Min(radius-1, radius - Mathf.RoundToInt(3.0f*Random.value));
      int alphaValue = 255 - Mathf.Max(1, 1*Mathf.RoundToInt((float)size/128f));
      for(int i=-radius; i<=radius; i++){
        for(int j=-radius; j<=radius; j++){
          int currentRadius = Mathf.RoundToInt(Mathf.Sqrt(i*i+j*j));
          if(currentRadius<=radius){
            if (currentRadius<=minPerturbationRadius){
              if (shape_bmp.GetPixel(point[0]+i, point[1]+j).a==1f){
                shape_bmp.SetPixel(point[0]+i, point[1]+j, new Color(1,1,1,1f/255f));
              }
            } else {
              float perturbation = Random.value;
              if (perturbation<0.2f){
                if (shape_bmp.GetPixel(point[0]+i, point[1]+j).a==1f){
                  shape_bmp.SetPixel(point[0]+i, point[1]+j, new Color(1,1,1,1f/255f));
                }
              }
            }
          }
        }
      }
    }
    void GenerationReset(){
      roundNumber++;
      if (debugging){
        Debug.Log("Restarting TerrainGenerator.cs");
      }
      size = 128;
      alreadyMultiplied = 0;
      shapePoints = new List<int[]>();
      generation_phase = 0;
      auxValue = 0;
      finished = false;
      bleedPoints = new List<int[]>();
      shape_bmp = new Texture2D(size,size, TextureFormat.Alpha8, false);
      for(int i=0;i<size;i++){
        for(int j=0;j<size;j++){
          shape_bmp.SetPixel(i,j,Color.clear);
        }
      }
      if (renderBool){
        Renderer rend = GetComponent<Renderer>();
        if (rend!= null){
          rend.material.mainTexture = shape_bmp;
        } else {
          Debug.LogError("THERE IS NO Renderer IN THIS GAME OBJECT");
        }
      } else {
        if (debugging){
          Debug.Log("Not setting texture to renderer");
        }
      }
      if (renderBool){
        shape_bmp.Apply();
      }
    }
    public bool GetFinished(){
      return finished;
    }
    public Texture2D GetShape_bmp(){
      return shape_bmp;
    }
    public int GetSize(){
      return size;
    }
    void LoadListDataIntoNewSizedShape(){
      size = sizeMultiplier*size;
      shape_bmp.Resize(size,size);
      for (int i=0; i<size; i++){
        for (int j=0; j<size; j++){
          shape_bmp.SetPixel(i,j,Color.clear);
        }
      }
      List<int[]> auxList = new List<int[]>();
      foreach(int[] pair in shapePoints) {
        for (int i=0; i<sizeMultiplier; i++){
          for (int j=0; j<sizeMultiplier; j++){
            shape_bmp.SetPixel(pair[0]*sizeMultiplier+i,pair[1]*sizeMultiplier+j,Color.white);
          }
        }
      }
      foreach(int[] pair in bleedPoints){
        for (int i=0; i<sizeMultiplier; i++){
          for (int j=0; j<sizeMultiplier; j++){
            auxList.Add(new int[2]{pair[0]*sizeMultiplier+i,pair[1]*sizeMultiplier+j});
          }
        }
      }
      bleedPoints = auxList;
      shapePoints =  new List<int[]>();
    }
    void LoadShapeDataIntoList(){
      for (int i=0; i<size; i++){
        for (int j=0; j<size; j++){
          if (Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255)==255) {
            shapePoints.Add(new int[2]{i,j});
          } else {
            if (Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255)==200){
              shapePoints.Add(new int[2]{i,j});
              bleedPoints.Add(new int[2]{i,j});
            } else {
              if (Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255)==100) {
                bleedPoints.Add(new int[2]{i,j});
              }
            }
          }
        }
      }
    }
    int MeasureArea(){
      if (debugging){
        Debug.Log("Measuring Island Size");
      }
      int islandSize = 0;
      for(int i=0;i<size;i++){
        for(int j=0;j<size;j++){
          if (shape_bmp.GetPixel(i,j).a==1f){
            islandSize++;
          }
        }
      }
      return islandSize;
    }
    int MeasurePerimeter(){ // HERE
      int perimeterSize = 0;
      for(int i=0;i<size;i++){
        for(int j=0;j<size;j++){
          if (shape_bmp.GetPixel(i,j).a==1f){
					  int i_p = Mathf.Min (i + 1, size - 1);
					  int j_p = Mathf.Min (j + 1, size - 1);
            int i_m = Mathf.Max(i-1,0);
            int j_m = Mathf.Max(j-1,0);
            int neighbours =  Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j).a) + 
                              Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j).a) + 
                              Mathf.RoundToInt(shape_bmp.GetPixel(i,j_m).a) + 
                              Mathf.RoundToInt(shape_bmp.GetPixel(i,j_p).a) + 
                              Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j_p).a) + 
                              Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j_m).a) + 
                              Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j_p).a) + 
                              Mathf.RoundToInt(shape_bmp.GetPixel(i_p,j_m).a);
            if (neighbours<8){
              perimeterSize++;
            }
          }
        }
      }
      return perimeterSize;
    }
    void RemoveBleed(){
      for(int i=0; i<size; i++){
        for(int j=0; j<size; j++){
          float pixelAlpha = shape_bmp.GetPixel(i,j).a;
          if(pixelAlpha!=1f){
            shape_bmp.SetPixel(i,j,Color.clear);
          }
        }
      }
      bleedPoints = new List<int[]>();
    }
    void RemoveExtraIslands(int biggerIslandAlpha){
      for(int i=0;i<size;i++){
        for(int j=0;j<size;j++){
          if (Mathf.RoundToInt(shape_bmp.GetPixel(i,j).a*255)!=biggerIslandAlpha){
            shape_bmp.SetPixel(i,j,Color.clear);
          } else {
            shape_bmp.SetPixel(i,j,Color.white);
          }
        }
      }
    }
    void RescaleIsland(int landSize){ // TODO // ALSO NOT IN USE
      // TODO
      // check size and scale to fit required area
      // try to reach size of 240.000;
    }
    void SeparateIslands(){
      List<int> islandAlphas = new List<int>(); // float
      List<int> islandSizes = new List<int>(); // ushort
      for (int i=254; i>0; i--){
        islandAlphas.Add(i);
        islandSizes.Add(0);
      }
      int listPointer = 0;
      for(int i=0;i<size;i++){ // j goes up i goes right
        for(int j=0;j<size;j++){
          if (shape_bmp.GetPixel(i,j).a==1f){
            int neighbourCount = 0;
            int firstNeighbour = 0;
            int i_m = Mathf.Max(i-1,0);
            int j_m = Mathf.Max(j-1,0);
            int[] neighbourValues = new int[2] {Mathf.RoundToInt(shape_bmp.GetPixel(i_m,j).a*255),Mathf.RoundToInt(shape_bmp.GetPixel(i,j_m).a*255)};
            foreach (int value in neighbourValues){
              List<int> auxList = islandAlphas.GetRange(0,listPointer);
              if (auxList.Contains(value)){
                neighbourCount++;
                if (firstNeighbour==0){
                  firstNeighbour=value;
                }
              }
            }
            switch (neighbourCount){
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
                if (neighbourValues[0]!=neighbourValues[1]){
                  for (int i2=0;i2<=i;i2++){
                    for (int j2=0;j2<size;j2++){
                      if (Mathf.RoundToInt(shape_bmp.GetPixel(i2,j2).a*255)==neighbourValues[1]){
                        islandSizes[auxListPointer_0]++;
                        islandSizes[auxListPointer_1]--;
                        if (islandSizes[auxListPointer_1]==0){
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
    public void SetCanStart(bool newBool){
      canStart = newBool;
    }
    public void SetDataCollect(bool newBool){
      dataCollect = newBool;
    }
    public void SetDebugging(bool newBool){
      debugging = newBool;
    }
    public void SetDestroyOnEnd(bool newBool){
      destroyOnEnd = newBool;
    }
    public void SetFinished(bool newBool){
      finished = newBool;
    }
    public void SetRandomSeed(int newSeed){
      useSeed = true;
      randomSeed = newSeed;
    }
    public void SetRenderBool(bool newBool){
      renderBool = newBool;
    }
    public void SetShowcase(bool newBool){
      showcase = newBool;
    }
    void SmoothIslandBorders(float a, float b, float c, float d, float e){ 
      // smoothes island borders by taking the average pixel alpha value of point i,j and its neighbours weighted by 'a', 'b' and 'c' where a weights the point, 'b' weights its axis neighbours and 'c' weights its diagonal neighbours. if the average is higher than 'e' the point receives the new value of 'd' in its alpha, otherwise it receives 0 in its alpha
      for(int i=0;i<size;i++){
        for(int j=0;j<size;j++){
					int i_p = Mathf.Min (i + 1, size - 1);
					int j_p = Mathf.Min (j + 1, size - 1);
          int i_m = Mathf.Max(i-1,0);
          int j_m = Mathf.Max(j-1,0);
          float newValue = (a*shape_bmp.GetPixel(i,j).a + b*shape_bmp.GetPixel(i_m,j).a + b*shape_bmp.GetPixel(i_p,j).a + b*shape_bmp.GetPixel(i,j_m).a + b*shape_bmp.GetPixel(i,j_p).a + c*shape_bmp.GetPixel(i_p,j_p).a + c*shape_bmp.GetPixel(i_m,j_m).a + c*shape_bmp.GetPixel(i_m,j_p).a + c*shape_bmp.GetPixel(i_p,j_m).a)/(a+4*b+4*c);
          if (newValue > e) {
						shape_bmp.SetPixel(i,j,new Color(1,1,1,d));
					} else {
						shape_bmp.SetPixel(i,j,Color.clear);
          }
        }
      }
    }
    void SmoothResizedIslandBorders(float a, float b, float c, bool d){ // NOT IN USE
      // commonly used with the following values: SmoothResizedIslandBorders(1f,0.71f,3.4f,true);
      List<int[]> auxListWhite = new List<int[]>();
      List<int[]> auxListClear = new List<int[]>();
      foreach (int[] pair in bleedPoints){
        int i = pair[0];
        int j = pair[1];
        if (shape_bmp.GetPixel(i,j).a == 0f){
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
          if (neighbours >= c){
            auxListWhite.Add(new int[2]{i,j});
          } else {
            if (neighbours == b && d == true){
              int[] newPair = new int[2];
              if (shape_bmp.GetPixel(i_p,j_p).a == 1f){newPair = new int[2]{i_p,j_p};}
              if (shape_bmp.GetPixel(i_m,j_m).a == 1f){newPair = new int[2]{i_m,j_m};}
              if (shape_bmp.GetPixel(i_p,j_m).a == 1f){newPair = new int[2]{i_p,j_m};}
              if (shape_bmp.GetPixel(i_m,j_p).a == 1f){newPair = new int[2]{i_m,j_p};}
              auxListClear.Add(newPair);
            }
          }
        }
      }
      foreach (int[] pair in auxListClear){
        shape_bmp.SetPixel(pair[0],pair[1],Color.clear);
      }
      foreach (int[] pair in auxListWhite){
        shape_bmp.SetPixel(pair[0],pair[1],Color.white);
      }
    }
}
