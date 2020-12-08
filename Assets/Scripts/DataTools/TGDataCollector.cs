using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TGDataCollector : MonoBehaviour
{
    public bool debugging = false;
    public GameObject terrainGenerator;
    private GameObject lastchild;
    private int dataPointsCount = 0;
    public int maxDataPoints = 100;
    private bool finished = false;
    public bool destroyOnEnd = true;
    private bool receivedAllData = false;
    private bool dataSaved = false;
    private string dataText = "";
    
    void Start()
    {
      if (debugging){
        Debug.Log("Starting TGDataCollector.cs");
      }
      if (terrainGenerator!=null){        
          lastchild = Instantiate(terrainGenerator, Vector3.zero, Quaternion.identity);
          lastchild.transform.parent = this.transform;
          lastchild.transform.name = "TGQuad";
          lastchild.GetComponent<TerrainGenerator>().SetDestroyOnEnd(false);
          lastchild.GetComponent<TerrainGenerator>().SetDataCollect(true);
          lastchild.GetComponent<TerrainGenerator>().SetRenderBool(false);
          lastchild.GetComponent<TerrainGenerator>().SetCanStart(true);
          if (!debugging){
            lastchild.GetComponent<TerrainGenerator>().SetDebugging(false);
          }
      } else {
        Debug.LogError("TERRAIN GENERATOR NOT SET");
      }
    }

    void Update()
    {
      if (!finished){
        if (dataPointsCount<maxDataPoints){
          if (receivedAllData){
            if (debugging){
              Debug.Log("Saving Received Data, round: "+(dataPointsCount+1).ToString());
            }
            LogData();
            dataPointsCount++;
            ResetTempData();
          }
          if (lastchild.GetComponent<TerrainGenerator>().GetFinished() == true && dataSaved == true){
            if (debugging){
              Debug.Log("Reseting Generation to collect new datapoints, round: "+(dataPointsCount+1).ToString());
            }
            lastchild.GetComponent<TerrainGenerator>().CallReset();
            dataSaved = false;
          }
        } else {
          finished = true;
        }
      } else {
        lastchild.GetComponent<TerrainGenerator>().SetDestroyOnEnd(true);
        if (destroyOnEnd){
          if (debugging){
            Debug.Log("Destroying this object's TGDataCollector.cs script");
          }
          TGDataCollector script = GetComponent<TGDataCollector>();
          Destroy(script);
        }
      }
    }

    void LogData(){
      string date = System.DateTime.Now.ToString("yyyy_MM_");
      string path = "D:/UnityProjects/Data/"+date+"PCG_dataTGen.csv";
      if (!File.Exists(path)){
        string dataHeader = "Was Reset"+","+
                            "Number of times reset"+","+
                            "LandSize when small"+","+
                            "Final LandSize"+","+
                            "Final Perimeter"+","+
                            System.Environment.NewLine;
        File.WriteAllText(path,dataHeader);
      }
      dataText+=System.Environment.NewLine;
      File.AppendAllText(path,dataText);
      if (debugging){
        Debug.Log("DATA SAVED, YEAH");
      }
      dataSaved = true;
    }

    void ResetTempData(){
      receivedAllData = false;
    }

    public void SetData(string receivedData){
      if (debugging){
        Debug.Log("Receiving Data, round: "+(dataPointsCount+1).ToString());
      }
      dataText = receivedData;
      receivedAllData = true;
    }
}
