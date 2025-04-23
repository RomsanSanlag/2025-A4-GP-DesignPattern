using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _buttontxt;
    [SerializeField] GameObject _prefab;
    [SerializeField] float _interval;
    [SerializeField] float _randomSphereRadius;
    [SerializeField] Transform _center;
        
    bool _isOn;
    Coroutine _routine;
    
    void UpdateTxt() => _buttontxt.text = _isOn ? "ON" : "OFF";

    void Reset()
    {
        _buttontxt = GetComponentInChildren<TextMeshProUGUI>();
        _prefab = null;
        _interval = 0.2f;
        _randomSphereRadius = 1f;
        _center = transform;
    }

    void Start()
    {
        UpdateTxt();
    }

    public void Toggle()
    {
        _isOn = !_isOn;
        UpdateTxt();

        if (_isOn)
        {

            _routine = StartCoroutine(SpawnRoutine());
        }
        else
        {
            if(_routine!=null) StopCoroutine(_routine);
            _routine = null;
        }


        IEnumerator SpawnRoutine()
        {
            var w = new WaitForSeconds(_interval);
            while (true)
            {
                var randomPoint = _center.position + (Random.insideUnitSphere * _randomSphereRadius);
                var go = Instantiate(_prefab, randomPoint, Quaternion.identity);
                Destroy(go, 3f);
                yield return w;
            }
        }
    }
}
