using UnityEngine;

public class DishSoap : MonoBehaviour
{
    private float lastSoapSfxTime = -999f;
    private const float SoapSfxCooldown = 0.12f;

    public void ReceiveSponge(DishSponge sponge)
    {
        if (sponge == null)
            return;

        if (!sponge.AddSoap())
            return;

        float timeSinceLastSoapSfx = Time.time - lastSoapSfxTime;
        if (timeSinceLastSoapSfx >= SoapSfxCooldown)
        {
            SoundEffectManager.Play("Soap");
            lastSoapSfxTime = Time.time;
        }

        Debug.Log("Sponge touched dishwashing liquid.");
    }
}