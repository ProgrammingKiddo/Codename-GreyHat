/*
 * Copyright (c) Borja Fernández
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalScreen : MonoBehaviour
{

    #region Variables
    public GameObject goodEnding;
    public GameObject badEnding;
    public GameObject worseEnding;
    #endregion


    #region UnityMethods

    void Start()
    {
        switch(PlayerPrefs.GetInt("Final"))
        {
            case -1:
                worseEnding.SetActive(true);
                break;
            case 0:
                badEnding.SetActive(true);
                break;
            case 1:
                goodEnding.SetActive(true);
                break;
        }
    }

    void Update()
    {
        
    }

    #endregion
}
