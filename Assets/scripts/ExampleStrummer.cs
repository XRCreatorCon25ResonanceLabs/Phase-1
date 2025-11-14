using System.Collections.Generic;
using UnityEngine;

public class ExampleStrummer : MonoBehaviour
{
    [SerializeField] private List<String> strings = new();
    [SerializeField] private float strumInterval = 0.1f;
    [SerializeField] private float restrumDelay = 5f;
    
    private void Start()
    {
        StartCoroutine(StrumAllStrings());
    }
    
    private IEnumerator<WaitForSeconds> StrumAllStrings()
    {
        while (true)
        {
            foreach (var str in strings)
            {
                str.Strum(1f);
                yield return new WaitForSeconds(strumInterval);
            }
            yield return new WaitForSeconds(restrumDelay);
        }
    }
}
