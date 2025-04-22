using UnityEngine;

public class Caisse : MonoBehaviour, IDamage
{
    public void TakeDamage(float amount)
    {
        Debug.Log("Oh no");
    }
}
