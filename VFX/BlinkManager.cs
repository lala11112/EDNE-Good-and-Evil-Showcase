using DG.Tweening;
using TMPro;
using UnityEngine;

public class BlinkManager : MonoBehaviour
{
    public LoopType loopType;
    public TextMeshProUGUI text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text.DOFade(0.0f, 1).SetLoops(-1, loopType);
    }

}
