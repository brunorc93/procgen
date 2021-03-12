using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

  public Text PdebugText;
  public Text SdebugText;
  public Text Timer;
  private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

  public GameObject biomeLabel;
  private bool done = false;

  private int sidebarCount = 0;

    void Start()
    {
      SetPDebugText("");
      SetSDebugText("");
      StartTimer();
    }

    void Update()
    {
      if (!done)
      {
        System.TimeSpan ts = stopwatch.Elapsed;
        Timer.text = ts.ToString(@"mm\:ss\.fff");
      }
    }

  public void SetPDebugText(string text) { PdebugText.text = text; }

  public void SetSDebugText(string text) { SdebugText.text = text; }

  void StartTimer()
  {
    Timer.text = "00:00.000";
    stopwatch.Start();
  }

  public void SetDone(bool newBool)
  {
    done = newBool;
    stopwatch.Stop();
  }

  public void AddLabel(string text, Color color, Vector2 position, float area, GameObject biome)
  {
    GameObject newGO = Instantiate(biomeLabel);
    if (position == Vector2.zero)
    {
      position = new Vector2(30+25*(sidebarCount%2),1670-sidebarCount*25);
      sidebarCount++;
      newGO.transform.Find("Dot").GetComponent<RectTransform>().sizeDelta = new Vector2(30,30);
    }
    newGO.transform.name = text;
    float lblLength = 0;
    for (int i=0; i<=text.Length; i++) { lblLength+=20; }
    RectTransform rt = newGO.GetComponent<RectTransform>();
    rt.anchoredPosition = new Vector2(position.x-25,position.y-20);
    Transform lblBG = newGO.transform.Find("LabelBG");
    RectTransform lblBGrt = lblBG.GetComponent<RectTransform>();
    if (position.x>1300)
    {
      lblBGrt.anchorMin = new Vector2(1,0);
      lblBGrt.anchorMax = new Vector2(1,1);
      lblBGrt.pivot = new Vector2(1,0.5f);
      lblBGrt.anchoredPosition = new Vector2(-50,0);
    }
    lblBGrt.sizeDelta = new Vector2(lblLength,0);
    newGO.transform.SetParent(this.transform.Find("LabelOrganizer"));
    newGO.transform.Find("Dot").GetComponent<Image>().color = color;
    Transform lbl = lblBG.Find("Label");
    lbl.GetComponent<Text>().text = text;
    lbl.gameObject.SetActive(false);
    lblBG.gameObject.SetActive(false);
    newGO.transform.Find("Dot").GetComponent<DotMouseHover>().SetBiomeQuadGO(biome);
  }
}