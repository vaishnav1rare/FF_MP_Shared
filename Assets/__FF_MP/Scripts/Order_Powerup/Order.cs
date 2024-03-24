
using Fusion;
using OneRare.FoodFury.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class Order : NetworkBehaviour,ICollidable
{
    public Material orderMaterial;
    public Image loadingIndicator;
    private float totalWaitTime = 2f;
    private float currentWaitTime;
    private ChangeDetector _changeDetector;
    [Networked] public bool IsCollecting { get; set; }
    [Networked] public Player Player{ get; set; }
    public override void Spawned()
    {
        base.Spawned();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        transform.position = ChallengeManager.instance.OrderPosition;
        currentWaitTime = totalWaitTime;
    }
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsCollecting):
                {
                    SetIsCollectingFlag(IsCollecting);
                    break;
                }
                case nameof(Player):
                {
                    SetPlayer(Player);
                    break;
                }
            }
        }
    }

    void SetPlayer(Player player)
    {
        Player = player;
    }
    private void SetIsCollectingFlag(bool value)
    {
        IsCollecting = value;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collected: "+other.gameObject.name);
        if(IsCollecting || Player !=null)
        {
            Debug.Log("Already Being Collected");
            return;
        }
            
        if (other.gameObject.TryGetComponent(out Player player))
        {
            Collide(player);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("UnCollected: "+other.gameObject.name);
        if (other.gameObject.TryGetComponent(out Player player))
        {
            UnCollide(player);
        }
    }
    
    public void Collide(Player player)
    {

        if (IsCollecting || Player != null)
        {
            Debug.Log("Already Being Collected");
        }
        else
        {
            IsCollecting = true;
            Player = player;
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

    public void UnCollide(Player player)
    {
        IsCollecting = false;
        //StopCoroutine(CollectOrderCoroutine(vehicle));
        Player = null;
        if (loadingIndicator != null)
        {
            loadingIndicator.fillAmount = 1; // Reset fill amount
        }
        
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
                if (Player)
                {
                    Player.OrderCount++;
                    if (Runner.IsSharedModeMasterClient)
                    {
                        IsCollecting = false;
                        Player = null;
                        Runner.Despawn(Object);
                        ChallengeManager.instance.SpawnNextOrder();
                    }
                }
            }
        }
        
    }

    
}
