
using System.Collections;
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
    [Networked] public float TimeInsideTrigger { get; set; }
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
                case nameof(TimeInsideTrigger):
                {
                    SetTimeInsideTrigger(TimeInsideTrigger);
                    break;
                }
            }
        }

        if (!IsCollecting)
            loadingIndicator.fillAmount = 1f;
        else
        {
            loadingIndicator.fillAmount = TimeInsideTrigger / 2f;
        }
    }
    void SetTimeInsideTrigger(float time)
    {
        TimeInsideTrigger = time;
    }
    void SetPlayer(Player player)
    {
        Player = player;
    }
    private void SetIsCollectingFlag(bool value)
    {
        IsCollecting = value;
    }

    
    private IEnumerator CollectOrderCoroutine(Player player)
    {
        IsCollecting = true;
        // Wait for 2 seconds
        TimeInsideTrigger = 0f;
        Color materialColor = orderMaterial.color;
        materialColor.a = 0.5f; // Adjust alpha value to make translucent
        orderMaterial.color = materialColor;
        
        while (TimeInsideTrigger < 2f)
        {
            // Check if the player has left the trigger prematurely
            if (!IsCollecting)
            {
                yield break; // Exit coroutine
            }
            
            TimeInsideTrigger += Time.deltaTime;
            yield return null;
        }
        UIManager.Instance.ShowOrderCollected(player.Username.ToString());
        player.OrderCount++;

        // Check if the current client is the master client
        if (Runner.IsSharedModeMasterClient)
        {
            //IsCollecting = false;
            Player = null;
            Runner.Despawn(Object);
            ChallengeManager.instance.SpawnNextOrder();
        }

        IsCollecting = false;
    }
    
    public void Collide(Player player)
    {
        if (!IsCollecting && Player == null)
        {
            StartCoroutine(CollectOrderCoroutine(player));
        }
    }

    public void UnCollide(Player player)
    {
        IsCollecting = false;
        Player = null;
        if (loadingIndicator != null)
        {
            loadingIndicator.fillAmount = 1; // Reset fill amount
        }
    }
    
}
