using System;
using UnityEngine;

public interface ISetter<T>
{
    void Provide(T value);
}

public class Reference<T> : ScriptableObject, ISetter<T>
{
    public T Instance { get; protected set; }

    void ISetter<T>.Provide(T value) => Instance = value;


    void coucou()
    {
        var refe = ScriptableObject.CreateInstance<Reference<T>>();

        refe.Instance = this.Instance;




    }

}

