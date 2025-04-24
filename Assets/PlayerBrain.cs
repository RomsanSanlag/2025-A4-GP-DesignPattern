using System;
using Tanks.Complete;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBrain : MonoBehaviour
{
    [SerializeField] TankShooting _shooting;

    InputAction _fireAction;
    TankInputUser m_InputUser;
    
    void Start()
    {
        // On récupère la manette
        m_InputUser = GetComponent<TankInputUser>();
        _fireAction = m_InputUser.ActionAsset.FindAction("Fire");
        
        // input
        _fireAction.started += StartShoot;
        _fireAction.canceled += StopShoot;
    }

    void OnDestroy()
    {
        _fireAction.started -= StartShoot;
        _fireAction.canceled -= StopShoot;
    }

    void StopShoot(InputAction.CallbackContext obj)
    {
        _shooting.StopCharging();
    }

    void StartShoot(InputAction.CallbackContext obj)
    {
        _shooting.StartCharging();
    }
}
