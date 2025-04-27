using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Update = UnityEngine.PlayerLoop.Update;

namespace Tanks.Complete
{
    public class TankHealth : MonoBehaviour, IDamage, IHealth
    {
        public float m_StartingHealth = 100f;               // The amount of health each tank starts with.
        public GameObject m_ExplosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies.
        [HideInInspector] public bool m_HasShield;          // Has the tank picked up a shield power up?
        
        private AudioSource m_ExplosionAudio;               // The audio source to play when the tank explodes.
        private ParticleSystem m_ExplosionParticles;        // The particle system the will play when the tank is destroyed.
        private float m_CurrentHealth;                      // How much health the tank currently has.
        private bool m_Dead;                                // Has the tank been reduced beyond zero health yet?
        private float m_ShieldValue;                        // Percentage of reduced damage when the tank has a shield.
        private bool m_IsInvincible;                        // Is the tank invincible in this moment?

        public float CurrentHealth
        {
            get => m_CurrentHealth;
            private set
            {
                var old = m_CurrentHealth;
                m_CurrentHealth = value;
            }
        } 

        public event Action<float, float> OnHealthChanged;
        [SerializeField] UnityEvent _onDamage;
        [SerializeField] UnityEvent _onRevive;

        private void Awake ()
        {
            // Instantiate the explosion prefab and get a reference to the particle system on it.
            m_ExplosionParticles = Instantiate (m_ExplosionPrefab).GetComponent<ParticleSystem> ();

            // Get a reference to the audio source on the instantiated prefab.
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource> ();

            // Disable the prefab so it can be activated when it's required.
            m_ExplosionParticles.gameObject.SetActive (false);
        }

        private void OnDestroy()
        {
            if(m_ExplosionParticles != null)
                Destroy(m_ExplosionParticles.gameObject);
        }

        private void OnEnable()
        {
            // When the tank is enabled, reset the tank's health and whether or not it's dead.
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            m_HasShield = false;
            m_ShieldValue = 0;
            m_IsInvincible = false;

            
        }

        //void IDamage.TakeDamage(float amount)     // EXPLICITE
        public void TakeDamage (float amount)       // IMPLICITE
        {
            var old = CurrentHealth;

            // Check if the tank is not invincible
            if (!m_IsInvincible)
            {
                // Reduce current health by the amount of damage done.
                m_CurrentHealth = CurrentHealth - amount * (1 - m_ShieldValue);

                // If the current health is at or below zero and it has not yet been registered, call OnDeath.
                if (CurrentHealth <= 0f && !m_Dead)
                {
                    OnDeath ();
                }
            }
            
            OnHealthChanged?.Invoke(old, CurrentHealth);
            _onDamage.Invoke();
        }

        public void IncreaseHealth(float amount)
        {
            var old = CurrentHealth;
            // Check if adding the amount would keep the health within the maximum limit
            if (CurrentHealth + amount <= m_StartingHealth)
            {
                // If the new health value is within the limit, add the amount
                m_CurrentHealth = CurrentHealth + amount;
            }
            else
            {
                // If the new health exceeds the starting health, set it at the maximum
                m_CurrentHealth = m_StartingHealth;
            }
            
            OnHealthChanged?.Invoke(old, CurrentHealth);
            _onRevive.Invoke();
        }

        public void ToggleShield (float shieldAmount)
        {
            // Inverts the value of has shield.
            m_HasShield = !m_HasShield;

            // Stablish the amount of damage that will be reduced by the shield
            if (m_HasShield)
            {
                m_ShieldValue = shieldAmount;
            }
            else
            {
                m_ShieldValue = 0;
            }
        }

        public void ToggleInvincibility()
        {
            m_IsInvincible = !m_IsInvincible;
        }

        private void OnDeath ()
        {
            // Set the flag so that this function is only called once.
            m_Dead = true;

            // Move the instantiated explosion prefab to the tank's position and turn it on.
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive (true);

            // Play the particle system of the tank exploding.
            m_ExplosionParticles.Play ();

            // Play the tank explosion sound effect.
            m_ExplosionAudio.Play();

            // Turn the tank off.
            gameObject.SetActive (false);
        }
    }
}