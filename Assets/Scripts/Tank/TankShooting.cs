using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tanks.Complete
{

    public static class TransformExtension
    {
        public static void DestroyAllChildren(this Transform t)
        {
            foreach (Transform el in t)
            {
                GameObject.Destroy(el.gameObject);
            }
        }

        public static IEnumerable<T> MyWhere<T>(this IEnumerable<T> @this, Func<T, bool> predicate)
        {
            foreach (T el in @this)
            {
                var canSend = predicate(el);
                if (canSend) yield return el;
            }
        }
        
        
    }
    
    
    public class TankShooting : MonoBehaviour
    {
        public Rigidbody m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
        public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        [Tooltip("The speed in unit/second the shell have when fired at minimum charge")]
        public float m_MinLaunchForce = 5f;        // The force given to the shell if the fire button is not held.
        [Tooltip("The speed in unit/second the shell have when fired at max charge")]
        public float m_MaxLaunchForce = 20f;        // The force given to the shell if the fire button is held for the max charge time.
        [Tooltip("The maximum time spent charging. When charging reach that time, the shell is fired at MaxLaunchForce")]
        public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.
        [Tooltip("The time that must pass before being able to shoot again after a shot")]
        public float m_ShotCooldown = 1.0f;         // The time required between 2 shots
        [Header("Shell Properties")]
        [Tooltip("The amount of health removed to a tank if they are exactly on the landing spot of a shell")]
        public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
        [Tooltip("The force of the explosion at the shell position. It is in newton, so it need to be high, so keep it 500 and above")]
        public float m_ExplosionForce = 1000f;              // The amount of force added to a tank at the centre of the explosion.
        [Tooltip("The radius of the explosion in Unity unit. Force decrease with distance to the center, and an tank further than this from the shell explosion won't be impacted by the explosion")]
        public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.

        [HideInInspector]
        public TankInputUser m_InputUser;           // The Input User component for that tanks. Contains the Input Actions. 
        
        public float CurrentChargeRatio =>
            (m_CurrentLaunchForce - m_MinLaunchForce) / (m_MaxLaunchForce - m_MinLaunchForce); //The charging amount between 0-1
        public bool IsCharging => m_IsCharging;
        
        public bool m_IsComputerControlled { get; set; } = false;

        private string m_FireButton;                // The input axis that is used for launching shells.
        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
        private bool m_Fired;                       // Whether or not the shell has been launched with this button press.
        private bool m_HasSpecialShell;             // has the tank a shell that makes extra damage?
        private float m_SpecialShellMultiplier;     // The amount that the special shell will multiply the damage.
        private InputAction fireAction;             // The Input Action for shooting, retrieve from TankInputUser
        private bool m_IsCharging = false;          // Are we currently charging the shot
        private float m_BaseMinLaunchForce;         // The initial value of m_MinLaunchForce
        private float m_ShotCooldownTimer;          // The timer counting down before a shot is allowed again
    
        bool _canFire;


        
        [ContextMenu("LINQ")]
        void coucou()
        {

            List<int> ages = new List<int>() { 2, 4, 89, 45, 30 };
            foreach (var VARIABLE in ages.MyWhere(i => i <= 30))
            {
                Debug.Log(VARIABLE);
            }


            //DestroyAllChildren(transform);

            //TransformExtension.DestroyAllChildren(transform);
            //transform.DestroyAllChildren();



        }


        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force, the UI and the power ups
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_BaseMinLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_BaseMinLaunchForce;
            m_HasSpecialShell = false;
            m_SpecialShellMultiplier = 1.0f;

            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;
        }
        CancellationTokenSource _cancel;

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();


            _cancel = new CancellationTokenSource();
        }

        private void Start ()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire";
            fireAction = m_InputUser.ActionAsset.FindAction(m_FireButton);
            
            fireAction.Enable();
            _canFire = true;

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }
        
        /// <summary>
        /// Used by AI to start charging
        /// </summary>
        public async UniTaskVoid StartCharging()
        {
            if (_canFire == false) return;
            if (m_IsCharging) return;
            
            // The slider should have a default value of the minimum launch force.
            m_IsCharging = true;
            m_Fired = false;
            m_AimSlider.value = m_BaseMinLaunchForce;
            m_CurrentLaunchForce = m_MinLaunchForce;
            
            while (true)
            {
                // Increment the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                m_AimSlider.value = m_CurrentLaunchForce;
                
                // If the max force has been exceeded and the shell hasn't yet been launched...
                if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
                {
                    // ... use the max force and launch the shell.
                    m_CurrentLaunchForce = m_MaxLaunchForce;
                    StopCharging();
                    return;
                }

                await UniTask.NextFrame();
            }
            
        }

        public async UniTaskVoid StopCharging()
        {
            if (m_IsCharging == false) return ;
            
            Fire();
            m_IsCharging = false;
            m_ShotCooldownTimer = 1f;
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_BaseMinLaunchForce;
            
            _canFire = false;
            
            //gameObject.GetCancellationTokenOnDestroy()
            
            bool hasBeenCancelled = await UniTask.Delay(10000, DelayType.DeltaTime, cancellationToken: _cancel.Token)
                .SuppressCancellationThrow();
            if (hasBeenCancelled)
            {
                _canFire = true;
            }
            
            //try
            //{
            //    await UniTask.Delay(10000, DelayType.DeltaTime, cancellationToken: _cancel.Token);
            //}
            //catch (Exception ec)
            //{
            //    Debug.Log("Cancelled");
            //}
            //finally
            //{
            //    _canFire = true;
            //}
        }
        
        [ContextMenu("Cancel")]
        public void CancelFire()
        {
            if (_canFire == true) return;
            
            _cancel.Cancel();
            _cancel = new CancellationTokenSource();
        }
        void ComputerUpdate()
        {
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_BaseMinLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // ... use the max force and launch the shell.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire ();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (m_IsCharging && !m_Fired)
            {
                // Increment the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                // ... launch the shell.
                Fire ();
                m_IsCharging = false;
            }
        }
        
        void HumanUpdate()
        {
            
        }
        
        private void Fire ()
        {
            // Set the fired flag so only Fire is only called once.
            m_Fired = true;

            // Create an instance of the shell and store a reference to it's rigidbody.
            Rigidbody shellInstance =
                Instantiate (m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            ShellExplosion explosionData = shellInstance.GetComponent<ShellExplosion>();
            explosionData.m_ExplosionForce = m_ExplosionForce;
            explosionData.m_ExplosionRadius = m_ExplosionRadius;
            explosionData.m_MaxDamage = m_MaxDamage;
            
            // Increase the damage if extra damage PowerUp is active
            if (m_HasSpecialShell)
            {
                explosionData.m_MaxDamage *= m_SpecialShellMultiplier;
                // Reset the default values after increasing the damage of the fired shell
                m_HasSpecialShell = false;
                m_SpecialShellMultiplier = 1f;
                
                PowerUpDetector powerUpDetector = GetComponent<PowerUpDetector>();
                if (powerUpDetector != null)
                    powerUpDetector.m_HasActivePowerUp = false;

                PowerUpHUD powerUpHUD = GetComponentInChildren<PowerUpHUD>();
                if (powerUpHUD != null)
                    powerUpHUD.DisableActiveHUD();
            }

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play ();

            // Reset the launch force.  This is a precaution in case of missing button events.
            m_CurrentLaunchForce = m_MinLaunchForce;

            m_ShotCooldownTimer = m_ShotCooldown;
        }


        public void EquipSpecialShell(float damageMultiplier)
        {
            m_HasSpecialShell = true;
            m_SpecialShellMultiplier = damageMultiplier;
        }

        /// <summary>
        /// Return the estyimated position the projectile will have with the charging level (between 0 & 1)
        /// </summary>
        /// <param name="chargingLevel">The fire charging level between 0 - 1</param>
        /// <returns>The position at which the projectile will be (ignore obstacle)</returns>
        public Vector3 GetProjectilePosition(float chargingLevel)
        {
            float chargeLevel = Mathf.Lerp (m_MinLaunchForce, m_MaxLaunchForce, chargingLevel);
            Vector3 velocity = chargeLevel * m_FireTransform.forward; 
            
            float a = 0.5f * Physics.gravity.y;
            float b = velocity.y;
            float c = m_FireTransform.position.y;
            
            float sqrtContent = b * b - 4 * a * c;
            //no solution
            if (sqrtContent <= 0)
            {
                return m_FireTransform.position;
            }

            float answer1 = (-b + Mathf.Sqrt(sqrtContent)) / (2 * a);
            float answer2 = (-b - Mathf.Sqrt(sqrtContent)) / (2 * a);

            float answer = answer1 > 0 ? answer1 : answer2;
            
            Vector3 position = m_FireTransform.position +
                               new Vector3(velocity.x, 0, velocity.z) *
                               answer;
            position.y = 0;

            return position;
        }

        
    }
}