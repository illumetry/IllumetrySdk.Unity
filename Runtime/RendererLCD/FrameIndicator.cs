using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameIndicator : MonoBehaviour
{
    int i = 0;

    // Update is called once per frame
    void Update()
    {
        ++i;
        GetComponent<UnityEngine.UI.Image>().color = i%2 == 0 ? Color.black : Color.white;
    }
}
