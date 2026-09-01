using UnityEngine;

public class Pellet : MonoBehaviour
{
    public int scoreValue = 10;

    private bool collected = false;

    public void Collect()
    {
        if (collected) return;
        collected = true;

        if (GameManagerPacman.Instance != null)
        {
            GameManagerPacman.Instance.AddScore(scoreValue);
        }

        Destroy(gameObject);
    }
}