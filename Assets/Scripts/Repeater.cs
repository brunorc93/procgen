using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Repeater : MonoBehaviour
{
    public string repeatingGenerator;
    private int repCount = 0;
    public int maxRepetitions = 100;
    [HideInInspector]
    public UIManager UIMan;
    private bool finished = false;

    private bool stopContinue = false;

    void Start()
    {
      UIMan = GameObject.Find("/UI/Canvas").GetComponent<UIManager>();
    }

    void Update()
    {
      if (!finished)
      {
        if (repCount<maxRepetitions)
        {
          if (repeatingGenerator == "TG")
          { if (GetComponent<TerrainGenerator>() == null) { RepeatTG(false);}
          } else if (repeatingGenerator == "BG") { if (GetComponent<BiomeGenerator>() == null){ RepeatBG(false); }
          } else if (repeatingGenerator == "TGBG")
          {
            if (GetComponent<TerrainGenerator>() == null && GetComponent<BiomeGenerator>() == null)
            {
              RepeatTG(true);
              stopContinue = true;
            }
            if (stopContinue){ if (GetComponent<BiomeGenerator>() != null){ StopAtBG(); } }
          } else { Debug.LogError("chose an appropriate string"); finished = true; }
        } else { finished = true; }
      }
    }

    void RepeatTG(bool cont)
    {
      repCount++;
      TerrainGenerator TG = gameObject.AddComponent<TerrainGenerator>() as TerrainGenerator;
      TG.useDebugUI = true;
      TG.showcase = true;
      TG.UIMan = UIMan;
      TG.dataCollect = true;
      TG._save = true;
      TG._continue = cont;
    }
    void RepeatBG(bool cont)
    {
      repCount++;
      BiomeGenerator BG = gameObject.AddComponent<BiomeGenerator>() as BiomeGenerator;
      BG.useDebugUI = false;
      BG.showcase = true;
      BG.UIMan = UIMan;
      BG.dataCollect = true;
      BG._save = true;
      BG._continue = cont;
    }
    void SetMaxRepCount_savedBGqtt()
    {
      int n=0;
      string path = Application.dataPath+"/Data/SavedGen/BG/SavedBG.txt";
      if (File.Exists(path))
      {
        string dataRead = File.ReadAllText(path);
        string[] dataLines = dataRead.Split('\n');
        n = dataLines.Length;
      }
      maxRepetitions = n;
    }
    void StopAtBG()
    {
      stopContinue = false;
      BiomeGenerator BG = gameObject.GetComponent<BiomeGenerator>();
      BG._continue = false;
    }
}