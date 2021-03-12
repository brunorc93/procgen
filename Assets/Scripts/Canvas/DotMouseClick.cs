using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class DotMouseClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
  public GameObject uniqueConstructsGO;

  public void OnPointerClick(PointerEventData eventData)
  {
    uniqueConstructsGO.SetActive(!uniqueConstructsGO.activeSelf);
    string text = uniqueConstructsGO.activeSelf ? "ON" : "OFF";
    this.transform.Find("OnOffLabel").GetComponent<Text>().text = text;
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    Color newColor = new Color(58f/255f,0f,1f,1f);
    this.transform.GetComponent<Image>().color = newColor;
  }
  
  public void OnPointerExit(PointerEventData eventData)
  {
    Color oldColor = new Color(0f,0f,0f,1f);
    this.transform.GetComponent<Image>().color = oldColor;
  }

}
