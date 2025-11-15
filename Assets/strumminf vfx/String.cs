using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class String : MonoBehaviour
{
    [SerializeField] private float decayRate = 0.1f;
    [SerializeField] private VisualEffect stringVFX;
    [SerializeField] private MeshRenderer stringRenderer;
    [SerializeField] private AudioSource stringAudioSource;
    
    private Color originalColor;
    
    private Coroutine strumCoroutine;
    
    [ContextMenu("Strum")]
    private void ContextStrum() => Strum(1);
    public void Strum(float strength = .5f)
    {
        stringVFX.SendEvent("Strum");
        stringAudioSource.Play();
        stringAudioSource.volume = strength * .7f;
        
        if (strumCoroutine != null)
        {
            StopCoroutine(strumCoroutine);
        }
        strumCoroutine = StartCoroutine(StrumDecay(strength));
    }
    
    private IEnumerator StrumDecay(float initialStrength)
    {
        var currentStrength = Mathf.Clamp01(initialStrength);
        while (currentStrength > 0)
        {
            currentStrength -= decayRate * Time.deltaTime;
            stringRenderer.material.SetFloat("_StrumStrength", currentStrength);
            yield return null;
        }
        stringRenderer.sharedMaterial.SetFloat("_StrumStrength", 0);
    }
    
}
