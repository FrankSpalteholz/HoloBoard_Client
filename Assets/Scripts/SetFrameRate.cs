using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetFrameRate : MonoBehaviour
{

    public int TargetFPS;

    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        
        Application.targetFrameRate = TargetFPS;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
