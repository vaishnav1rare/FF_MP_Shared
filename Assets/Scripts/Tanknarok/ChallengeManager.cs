using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public enum ChallengeType {
    RaceToDeliveries,
    RaceToPoints,
    MostPointsInTime,
    RaceToCollectItems
}

public class ChallengeManager : NetworkBehaviour, ISceneLoadDone
{
      
    public event Action<ChallengeType> OnChallengeStarted;
    public event Action OnTimerEnd;
    
    [SerializeField] private GameObject orderPrefab;
    [SerializeField] private int deliveryGoal = 3; 
    [SerializeField] private int itemGoal = 5;
    [SerializeField] private Transform[] orderSpawnPoints;
    [Networked] public string Time { get; set; } = "00:00";
    [Networked] public TickTimer MatchTimer { get; set; }
    [Networked] public int RandomIndex { get; set; }
    [Networked] public Vector3 OrderPosition { get; set; }
    [Networked] public bool IsMatchOver { get; set; }
    [Networked] public bool IsMatchStarted { get; set; } = false;
    
    private bool _isChallengeActive;
    private GameUI _gameUI;
    private ChangeDetector _changeDetector;
    private float _challengeDuration = 150f;
    
    
    public static ChallengeManager instance;
   private void Awake()
   {
       instance = this;
   }
    
    
    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _gameUI = FindObjectOfType<GameUI>();
    }
    
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(OrderPosition):
                {
                    SetTargetObject(OrderPosition);
                    break;
                }
                case nameof(RandomIndex):
                {
                    SetRandomIndex(RandomIndex);
                    break;
                }
                case nameof(Time):
                {
                    SetTime(Time);
                    break;
                }
                case nameof(IsMatchOver):
                {
                    SetGameOver();
                    break;
                }
            }
        }
    }

    void SetGameOver()
    {
        UIManager.Instance.ShowResult();
    }
    private void SetTime(string time)
    {
        _gameUI.UpdateTime(time);
    }

    public void SetTargetObject(Vector3 newTarget)
    {
        OrderPosition =  newTarget;
    }

    public void SetRandomIndex(int index)
    {
        RandomIndex = index;
    }
    public void StartChallenge(ChallengeType challengeType) {
       if ( Runner.IsSharedModeMasterClient == false)
            return;
        OnChallengeStarted?.Invoke(challengeType);
        MatchTimer = TickTimer.CreateFromSeconds(Runner, _challengeDuration);
        switch (challengeType) {
            case ChallengeType.RaceToDeliveries:
            {
                Invoke("StartRaceToDeliveries",2f);
                break;
            }
            case ChallengeType.RaceToPoints:
            {
                StartRaceToPoints();
                break;
            }
            case ChallengeType.MostPointsInTime:
            {
                StartMostPointsInTime();
                break;
            }
            case ChallengeType.RaceToCollectItems:
            {
                StartRaceToCollectItems();
                break;
            }
            default:
            {
                Debug.LogWarning("Unknown challenge type!");
                break;
            }
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (MatchTimer.Expired(Runner) == false && MatchTimer.RemainingTime(Runner).HasValue)
        {  
            var timeSpan = TimeSpan.FromSeconds(MatchTimer.RemainingTime(Runner).Value);
            var outPut = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            Time = outPut;
            
        }
        else if (MatchTimer.Expired(Runner))
        {
            //MatchIsOver = true;
            MatchTimer = TickTimer.None;
            OnTimerEnd?.Invoke();
            IsMatchOver = true;
        }
    }

    public void SpawnNextOrder()
    {
      SpawnOrder();
    }

    void SpawnOrder()
    {
        int randomIndex = Random.Range(0, orderSpawnPoints.Length);
        Vector3 randomSpawnPosition = orderSpawnPoints[randomIndex].position;
        OrderPosition = randomSpawnPosition;
        RPC_SetOrderPosition(OrderPosition);
        RandomIndex = randomIndex;
        Runner.Spawn(orderPrefab, OrderPosition, Quaternion.identity);
    }
    
    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    private void RPC_SetOrderPosition(Vector3 orderPosition)
    {
        OrderPosition = orderPosition;
    }
    private void StartRaceToDeliveries()
    {
        SpawnOrder();
        IsMatchStarted = true;
    }
    

    private void StartRaceToPoints() {

    }

    private void StartMostPointsInTime() {

    }

    private void StartRaceToCollectItems() {

    }

   
    public void SceneLoadDone(in SceneLoadDoneArgs sceneInfo)
    {
        Debug.Log("OnSceneLoadDone");
    }
}
