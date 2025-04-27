using System;
using UnityEngine;

public class CameraSetter : MonoBehaviour
{
    [SerializeField] CameraReference _cameraRef;
    [SerializeField] Camera _Camera;

    ISetter<Camera> SuperRef => _cameraRef;
    
    void Awake()
    {
        SuperRef.Provide(_Camera);
    }
}
