using UnityEngine;

public class AmbientZone : MonoBehaviour
{
    public AudioSource rainSource;       // outdoor rain
    public AudioSource roomSource;       // indoor ambience
    public float fadeSpeed = 1.5f;

    bool insideRoom = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            insideRoom = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            insideRoom = false;
    }

    void Update()
    {
        float targetRain = insideRoom ? 0f : 1f;
        float targetRoom = insideRoom ? 1f : 0f;

        rainSource.volume = Mathf.MoveTowards(rainSource.volume, targetRain, fadeSpeed * Time.deltaTime);
        roomSource.volume = Mathf.MoveTowards(roomSource.volume, targetRoom, fadeSpeed * Time.deltaTime);
    }
}
