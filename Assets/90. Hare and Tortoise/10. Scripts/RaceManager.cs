using System;
using UnityEngine;

namespace HareAndTortoise
{
    public class RaceManager : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"{other.name} Goal");
        }
    }
}
