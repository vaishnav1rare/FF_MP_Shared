using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

public class HPHandler : NetworkBehaviour
{
    [Networked]
    byte HP { get; set; }

    [Networked]
    public bool isDead { get; set; }

    bool isInitialized = false;

    const byte startingHP = 5;

    public Color uiOnHitColor;
    public Image uiOnHitImage;

    //List<FlashMeshRenderer> flashMeshRenderers = new List<FlashMeshRenderer>();

    public GameObject playerModel;
    public GameObject deathGameObjectPrefab;

    public bool skipSettingStartValues = false;

    ChangeDetector changeDetector;

    //Other components
    HitboxRoot hitboxRoot;
    //CharacterMovementHandler characterMovementHandler;
    //NetworkInGameMessages networkInGameMessages;
    //NetworkPlayer networkPlayer;

    private void Awake()
    {
        //characterMovementHandler = GetComponent<CharacterMovementHandler>();
        hitboxRoot = GetComponentInChildren<HitboxRoot>();
        //networkInGameMessages = GetComponent<NetworkInGameMessages>();
        //networkPlayer = GetComponent<NetworkPlayer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!skipSettingStartValues)
        {
            HP = startingHP;
            isDead = false;
        }

        //ResetMeshRenderers();

        isInitialized = true;
    }

    public override void Render()
    {
        foreach (var change in changeDetector.DetectChanges(this, out var previousBuffer, out var currentBuffer))
        {
            switch (change)
            {
                case nameof(HP):
                    var byteReader = GetPropertyReader<byte>(nameof(HP));
                    var (previousByte, currentByte) = byteReader.Read(previousBuffer, currentBuffer);
                    OnHPChanged(previousByte, currentByte);
                    break;

                case nameof(isDead):
                    var boolReader = GetPropertyReader<bool>(nameof(isDead));
                    var (previousBool, currentBool) = boolReader.Read(previousBuffer, currentBuffer);
                    OnStateChanged(previousBool, currentBool);
                    break;
            }
        }


    }

    /*public void ResetMeshRenderers()
    {
        //Clear old
        flashMeshRenderers.Clear();

        MeshRenderer[] meshRenderers = playerModel.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in meshRenderers)
            flashMeshRenderers.Add(new FlashMeshRenderer(meshRenderer, null));


        SkinnedMeshRenderer[] skinnedMeshRenderers = playerModel.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            flashMeshRenderers.Add(new FlashMeshRenderer(null, skinnedMeshRenderer));
    }*/

    /*IEnumerator OnHitCO()
    {
        foreach (FlashMeshRenderer flashMeshRenderer in flashMeshRenderers)
            flashMeshRenderer.ChangeColor(Color.red);

        if (Object.HasInputAuthority)
            uiOnHitImage.color = uiOnHitColor;

        yield return new WaitForSeconds(0.2f);

        foreach (FlashMeshRenderer flashMeshRenderer in flashMeshRenderers)
            flashMeshRenderer.RestoreColor();

        if (Object.HasInputAuthority && !isDead)
            uiOnHitImage.color = new Color(0, 0, 0, 0);
    }*/

    IEnumerator ServerReviveCO()
    {
        yield return new WaitForSeconds(2.0f);

        //characterMovementHandler.RequestRespawn();
    }


    //Function only called on the server
    public void OnTakeDamage(string damageCausedByPlayerNickname, byte damageAmount)
    {
        //Only take damage while alive
        if (isDead)
            return;

        //Ensure that we cannot flip the byte as it can't handle minus values.
        if (damageAmount > HP)
            damageAmount = HP;

        HP -= damageAmount;

        Debug.Log($"{Time.time} {transform.name} took damage got {HP} left ");

        //Player died
        if (HP <= 0)
        {
            //networkInGameMessages.SendInGameRPCMessage(damageCausedByPlayerNickname, $"Killed <b>{networkPlayer.nickName.ToString()}</b>");

            Debug.Log($"{Time.time} {transform.name} died");

            StartCoroutine(ServerReviveCO());

            isDead = true;
        }
    }

    void OnHPChanged(byte previous, byte current)
    {
        //Check if the HP has been decreased
        if (current < previous)
            OnHPReduced();
    }

    private void OnHPReduced()
    {
        if (!isInitialized)
            return;

        //StartCoroutine(OnHitCO());
    }

    void OnStateChanged(bool previous, bool current)
    {
        //Handle on death for the player. Also check if the player was dead but is now alive in that case revive the player.
        if (current)
            OnDeath();
        else if (!current && previous)
            OnRevive();
    }

    private void OnDeath()
    {
        Debug.Log($"{Time.time} OnDeath");

        playerModel.gameObject.SetActive(false);
        hitboxRoot.HitboxRootActive = false;
       // characterMovementHandler.SetCharacterControllerEnabled(false);

        Instantiate(deathGameObjectPrefab, transform.position, Quaternion.identity);
    }

    private void OnRevive()
    {
        Debug.Log($"{Time.time} OnRevive");

        if (Object.HasInputAuthority)
            uiOnHitImage.color = new Color(0, 0, 0, 0);

        playerModel.gameObject.SetActive(true);
        hitboxRoot.HitboxRootActive = true;

        //characterMovementHandler.SetCharacterControllerEnabled(true);
    }

    public void OnRespawned()
    {
        //Reset variables
        HP = startingHP;
        isDead = false;
    }

    public override void Spawned()
    {
        changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }
}
