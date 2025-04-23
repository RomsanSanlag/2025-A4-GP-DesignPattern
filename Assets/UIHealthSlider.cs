using System;
using Tanks.Complete;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class UIHealthSlider : MonoBehaviour
{
    [SerializeField] TankHealth _health;
    
    [SerializeField] Slider m_Slider;                             // The slider to represent how much health the tank currently has.
    [SerializeField] Image m_FillImage;                           // The image component of the slider.
    [SerializeField] Color m_FullHealthColor = Color.green;    // The color the health bar will be when on full health.
    [SerializeField] Color m_ZeroHealthColor = Color.red;      //

    [SerializeField] UnityEvent _a;
    
    void Start()
    {
        m_Slider.maxValue = _health.m_StartingHealth;

        SetHealthUI();
        _health.OnHealthChanged += HealthOnOnHealthChanged;
    }

    void OnDestroy()
    {
        _health.OnHealthChanged -= HealthOnOnHealthChanged;
    }

    void HealthOnOnHealthChanged(float old, float current)
    {
        // Update the health slider's value and color.
        SetHealthUI();
    }

    private void SetHealthUI ()
    {
        // Set the slider's value appropriately.
        m_Slider.value = _health.CurrentHealth;

        // Interpolate the color of the bar between the choosen colours based on the current percentage of the starting health.
        m_FillImage.color = Color.Lerp (m_ZeroHealthColor, m_FullHealthColor, _health.CurrentHealth / _health.m_StartingHealth);
    }

    
}
