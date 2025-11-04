using UnityEngine;
using TMPro;
using DG.Tweening;

public class BlinkManager : MonoBehaviour
{
    public LoopType loopType;
    public TextMeshProUGUI text;

    void Start()
    {
        text.DOFade(0f, 1f).SetLoops(-1, loopType);
    }
}