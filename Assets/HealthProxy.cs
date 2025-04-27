using Tanks.Complete;
using UnityEngine;

public interface IHealth
{
    public void TakeDamage(float amount);
}

public class HealthProxy : MonoBehaviour, IHealth
{
    [SerializeField] TankHealth _health;

    public void TakeDamage(float amount) => _health.TakeDamage(amount);

}
