using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Walldawn : MonoBehaviour
{
    private Animation walldawnAnim;
    private bool isPlayed = false;
    private void Start()
    {
        walldawnAnim = GetComponent<Animation>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayed)
        {
            walldawnAnim.Play();
            isPlayed = true;
        }
    }
}
