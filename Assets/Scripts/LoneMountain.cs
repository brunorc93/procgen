using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoneMountain : MonoBehaviour{

    public int size = 1024;

    public Vector2 initialPoint = Vector2.zero;
    public Vector2 peakPoint = Vector2.zero;

    public Terrain terr;
    // Start is called before the first frame update
    void Start(){
      float maxDist = 300f;
      peakPoint.x = Random.Range(400,800);
      peakPoint.y = Random.Range(400,800);
      float x0 = initialPoint.x;
      float y0 = initialPoint.y;
      float mult = 0.39f;
      int multCount = 5;
      float maxValue = float.MinValue;
      float minValue = float.MaxValue;
      float[] values = new float[size*size];
      float[,] heights = new float[size,size];
      for (int i=0; i<size; i++){
        for (int j=0; j<size; j++){
          float x = (float)i*mult;
          float y = (float)j*mult;
          float newValue = Mathf.PerlinNoise(x+x0,y+y0);
          float m = 1f;
          for (int k=0; k<multCount; k++){
            x = x/2;
            y = y/2;
            m = m*2.1f;
            newValue+=Mathf.PerlinNoise(x+x0,y+y0)*m;
          }
          values[i*size+j]=newValue;
          if (newValue>maxValue){
            maxValue = newValue;
          }
          if (newValue<minValue){
            minValue = newValue;
          }
        }
      }
      for (int i=0; i<size; i++){
        for (int j=0; j<size; j++){
          float height = + 0.2f*(values[i*size+j]-minValue)/(maxValue-minValue) 
                         + 0.8f*(maxDist - Mathf.Min(Vector2.Distance(peakPoint,new Vector2(i,j)),maxDist))/maxDist;
          heights[j,i] = height;
        }
      }
      terr.terrainData.SetHeights(0,0,heights);
    }
}
