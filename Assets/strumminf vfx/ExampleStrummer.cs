using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleStrummer : MonoBehaviour
{
    [SerializeField] private List<String> strings = new();
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float strumInterval = 0.1f;
    [SerializeField] private float restrumDelay = 2f;
    [SerializeField] private ParticleSystem vfx;       // <-- assign your VFX here

    private Coroutine strumRoutine;

    private void OnEnable()
    {
        // Start playing VFX and strumming when the object is ENABLED
        strumRoutine = StartCoroutine(StrumAllStrings());
    }

    private void OnDisable()
    {
        // Stop coroutine and VFX instantly when object DISABLES
        if (strumRoutine != null)
            StopCoroutine(strumRoutine);

        if (vfx != null)
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private IEnumerator StrumAllStrings()
    {
        // Optional delay before starting
        yield return new WaitForSeconds(initialDelay);

        // Play VFX when strumming starts
        if (vfx != null)
            vfx.Play();

        // Run 2 full cycles
        for (int i = 0; i < 2; i++)
        {
            foreach (var str in strings)
            {
                str.Strum(1f);
                yield return new WaitForSeconds(strumInterval);
            }

            if (i < 1)
                yield return new WaitForSeconds(restrumDelay);
        }

        // Stop VFX after strumming ends
        if (vfx != null)
            vfx.Stop();
    }
}
