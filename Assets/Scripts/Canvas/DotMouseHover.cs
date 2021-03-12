using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DotMouseHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
  public GameObject lblBG;
  public GameObject lbl;
  public GameObject biomeQ;

  public void OnPointerEnter(PointerEventData eventData)
  {
    this.transform.parent.SetAsLastSibling();
    lblBG.SetActive(true);
    lbl.SetActive(true);
    biomeQ.SetActive(true);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    lblBG.SetActive(false);
    lbl.SetActive(false);
    biomeQ.SetActive(false);
  }

  public void SetBiomeQuadGO(GameObject newGO)
  {
    biomeQ = newGO;
  }
}
