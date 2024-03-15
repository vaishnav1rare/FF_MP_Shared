using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class Order : NetworkBehaviour,ICollidable
{
    public Material orderMaterial;
    public Image loadingIndicator;
    private float totalWaitTime = 2f;
    private float currentWaitTime;
    [Networked] public bool IsCollecting { get; set; }
    [Networked] public VehicleEntity VehicleEntity{ get; set; }
    public override void Spawned()
    {
        base.Spawned();
        transform.position = GlobalManager.Instance.ChallengeManager.OrderPosition;
        currentWaitTime = totalWaitTime;
        Debug.Log("!Order Spawned!");
    }
    public void Collide(VehicleEntity vehicle)
    {
        if (IsCollecting || VehicleEntity != null)
        {
            Debug.Log("Already Being Collected");
        }
        else
        {
            IsCollecting = true;
            VehicleEntity = vehicle;
            Color materialColor = orderMaterial.color;
            materialColor.a = 0.5f; // Adjust alpha value to make translucent
            orderMaterial.color = materialColor;
            currentWaitTime = totalWaitTime;
            if (loadingIndicator != null)
            {
                loadingIndicator.fillAmount = 1; // Reset fill amount
            }
            //StartCoroutine(CollectOrderCoroutine(vehicle));
        }
        
    }

    public void UnCollide(VehicleEntity vehicle)
    {
        IsCollecting = false;
        //StopCoroutine(CollectOrderCoroutine(vehicle));
        VehicleEntity = null;
        if (loadingIndicator != null)
        {
            loadingIndicator.fillAmount = 1; // Reset fill amount
        }
        Debug.Log("UnCollected: "+loadingIndicator.fillAmount);
    }
    private void Update()
    {
        if (IsCollecting && currentWaitTime > 0f)
        {
            // Update radial UI loading indicator gradually
            // Calculate the elapsed percentage of time
            float elapsedPercentage = 1f - (currentWaitTime / totalWaitTime);

            // Update radial UI loading indicator
            loadingIndicator.fillAmount = elapsedPercentage;

            currentWaitTime -= Time.deltaTime;

            if (currentWaitTime <= 0f)
            {
                // Order collection complete
                if (VehicleEntity)
                {
                    VehicleEntity.OrderCount++;
                    if (VehicleEntity.Object.HasStateAuthority)
                    {
                        IsCollecting = false;
                        VehicleEntity = null;
                        Runner.Despawn(Object);
                        GlobalManager.Instance.ChallengeManager.SpawnNextOrder();
                    }
                }
            }
        }
        
    }

    /*private IEnumerator CollectOrderCoroutine(VehicleEntity vehicleEntity)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.fillAmount = 0; // Reset fill amount
        }

       
        // Wait for the specified time
        while (currentWaitTime > 0f && IsCollecting)
        {
            yield return new WaitForSeconds(0.1f); // Update UI every 0.1 seconds

            currentWaitTime -= 0.1f;
            
            // Update radial UI loading indicator
            if (loadingIndicator != null)
            {
                loadingIndicator.fillAmount = 1 - (currentWaitTime / 2f); // Calculate fill amount
            }
        }

        if (vehicleEntity && IsCollecting)
        {
            vehicleEntity.OrderCount++;
            if ( vehicleEntity.Object.HasStateAuthority ) {
                IsCollecting = false;
                Runner.Despawn(Object);
                GlobalManager.Instance.ChallengeManager.SpawnNextOrder();
            }
        }
       
    }*/
}
