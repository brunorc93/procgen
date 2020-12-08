using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TGSnapShooter : MonoBehaviour
{
    public bool debugging = false;
    public GameObject terrainGenerator;
    private GameObject lastchild;
    private bool finished = false;
    public int photoCount = 50;
    private int photosTakenNumber = 0;
    private bool photoComplete = false;
    private bool waiting = false;
    private int waitedTurns = 0;
    public bool destroyOnEnd = true;
    
    void Start()
    { 
      if (debugging){
        Debug.Log("Starting TGSnapShooter.cs");
      }
      if (terrainGenerator!=null){
          lastchild = Instantiate(terrainGenerator, Vector3.zero, Quaternion.identity);
          lastchild.transform.parent = this.transform;
          lastchild.transform.name = "TGQuad";
          lastchild.GetComponent<TerrainGenerator>().SetDestroyOnEnd(false);
          lastchild.GetComponent<TerrainGenerator>().SetRenderBool(true);
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
        if (!waiting){
          if (lastchild.GetComponent<TerrainGenerator>().GetFinished() == true){
            if (photoComplete == false){
              TakePhoto();
              waiting = true;
            }
          }
        } else {
          if (waitedTurns<20){
            waitedTurns++;
          } else {
            if (photosTakenNumber<photoCount){
              waiting = false;
              lastchild.GetComponent<TerrainGenerator>().CallReset();
              photoComplete = false;
            } else {
              finished = true;
              lastchild.GetComponent<TerrainGenerator>().SetDestroyOnEnd(true);
            }
          }
        }
      } else {
        if (destroyOnEnd){
          if (debugging){
            Debug.Log("Destroying this object's TGSnapShooter.cs script");
          }
          TGSnapShooter script = GetComponent<TGSnapShooter>();
          Destroy(script);
        }
      }
    }

    void TakePhoto(){
      photosTakenNumber++;
      string date = System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
      string path = "D:/UnityProjects/SS/solo_";
      string photoN = (photosTakenNumber).ToString();
      string fullPath = path+date+"_n_"+photoN+".png";
      ScreenCapture.CaptureScreenshot(fullPath);
      photoComplete = true;
    }
}
