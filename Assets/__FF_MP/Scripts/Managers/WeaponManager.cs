using System.Collections;
using System.Collections.Generic;
using Fusion;
using OneRare.FoodFury.Multiplayer;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    public enum WeaponInstallationType
    {
    			PRIMARY,
    			SECONDARY,
    			BUFF
    };
    [SerializeField] private Weapon[] _weapons;
    [SerializeField] private Player _player;
    public RocketHandler rocketPrefab;
    [Networked]
    public byte selectedPrimaryWeapon { get; set; }

    [Networked]
    public byte selectedSecondaryWeapon { get; set; }

    [Networked]
    public TickTimer primaryFireDelay { get; set; }

    [Networked]
    public TickTimer secondaryFireDelay { get; set; }

    [Networked]
    public byte primaryAmmo { get; set; }

    [Networked]
    public byte secondaryAmmo { get; set; }

    private byte _activePrimaryWeapon;
    private byte _activeSecondaryWeapon;
    NetworkObject networkObject;

    public override void Spawned()
    {
	    networkObject = GetComponent<NetworkObject>();
    }
    public override void Render()
    {
	    ShowAndHideWeapons();
    }

    private void ShowAndHideWeapons()
    {
	    // Animates the scale of the weapon based on its active status
	    for (int i = 0; i < _weapons.Length; i++)
	    {
		    _weapons[i].Show(i == selectedPrimaryWeapon || i == selectedSecondaryWeapon);
	    }

	    // Whenever the weapon visual is fully visible, set the weapon to be active - prevents shooting when changing weapon
	    SetWeaponActive(selectedPrimaryWeapon, ref _activePrimaryWeapon);
	    SetWeaponActive(selectedSecondaryWeapon, ref _activeSecondaryWeapon);
    }
    void SetWeaponActive(byte selectedWeapon, ref byte _activeWeapon)
    {
	    if (_weapons[selectedWeapon].isShowing)
		    _activeWeapon = selectedWeapon;
    }
    
    public void ActivateWeapon(WeaponInstallationType weaponType, int weaponIndex)
    {
	    byte selectedWeapon = weaponType == WeaponInstallationType.PRIMARY ? selectedPrimaryWeapon : selectedSecondaryWeapon;
	    byte activeWeapon = weaponType == WeaponInstallationType.PRIMARY ? _activePrimaryWeapon : _activeSecondaryWeapon;

	    if (!_player.IsActivated || selectedWeapon != activeWeapon)
		    return;

	    // Fail safe, clamp the weapon index within weapons list bounds
	    weaponIndex = Mathf.Clamp(weaponIndex, 0, _weapons.Length - 1);

	    if (weaponType == WeaponInstallationType.PRIMARY)
	    {
		    selectedPrimaryWeapon = (byte)weaponIndex;
		    primaryAmmo = _weapons[(byte) weaponIndex].ammo;
	    }
	    
    }
    
    public void FireWeapon(WeaponInstallationType weaponType)
    {
	    if (!IsWeaponFireAllowed(weaponType))
		    return;

	    byte ammo = weaponType == WeaponInstallationType.PRIMARY ? primaryAmmo : secondaryAmmo;

	    TickTimer tickTimer = weaponType==WeaponInstallationType.PRIMARY ? primaryFireDelay : secondaryFireDelay;
	    if (tickTimer.ExpiredOrNotRunning(Runner) && ammo > 0)
	    {
		    byte weaponIndex = weaponType == WeaponInstallationType.PRIMARY ? _activePrimaryWeapon : _activeSecondaryWeapon;
		    Weapon weapon = _weapons[weaponIndex];
		    
		    weapon.Fire(Runner,Object.InputAuthority,_player.velocity);

		    if (!weapon.infiniteAmmo)
			    ammo--;

		    if (weaponType == WeaponInstallationType.PRIMARY)
		    {
			    primaryFireDelay = TickTimer.CreateFromSeconds(Runner, weapon.delay);
			    primaryAmmo = ammo;
		    }
		    else
		    {
			    secondaryFireDelay = TickTimer.CreateFromSeconds(Runner, weapon.delay);
			    secondaryAmmo = ammo;
		    }
					
		    if (/*Object.HasStateAuthority &&*/ ammo == 0)
		    {
			    ResetWeapon(weaponType);
		    }
	    }
    }
    TickTimer rocketFireDelay = TickTimer.None;
    [Header("Aim")]
    public Transform aimPoint;

    public void FireRocket(Vector3 aimForwardVector, Vector3 cameraPosition)
    {
	    //Check that we have not recently fired a grenade. 
	    if (rocketFireDelay.ExpiredOrNotRunning(Runner))
	    {
		    CalculateFireDirection(aimForwardVector, cameraPosition, out Vector3 fireDirection);

		    Runner.Spawn(rocketPrefab, aimPoint.position + fireDirection * 1.5f, Quaternion.LookRotation(fireDirection), Object.InputAuthority, (runner, spawnedRocket) =>
		    {
			    spawnedRocket.GetComponent<RocketHandler>().Fire(Object.InputAuthority, networkObject,  "networkPlayer.nickName.ToString()");
		    });

		    //Start a new timer to avoid grenade spamming
		    rocketFireDelay = TickTimer.CreateFromSeconds(Runner, 2f);
	    }
    }
    
    [Header("Collision")]
    public LayerMask collisionLayers;
    
    float maxHitDistance = 200;
     HPHandler CalculateFireDirection(Vector3 aimForwardVector, Vector3 cameraPosition, out Vector3 fireDirection)
    {
        LagCompensatedHit hitinfo = new LagCompensatedHit();

        fireDirection = aimForwardVector;
        float hitDistance = maxHitDistance;

        //Do a raycast from the 3rd person camera
        /*if (networkPlayer.is3rdPersonCamera)
        {
            Runner.LagCompensation.Raycast(cameraPosition, fireDirection, hitDistance, Object.InputAuthority, out hitinfo, collisionLayers, HitOptions.IgnoreInputAuthority | HitOptions.IncludePhysX);

            //Check against other players
            if (hitinfo.Hitbox != null)
            {
                fireDirection = (hitinfo.Point - aimPoint.position).normalized;
                hitDistance = hitinfo.Distance;

                Debug.DrawRay(cameraPosition, aimForwardVector * hitDistance, new Color(0.4f, 0, 0), 1);
            }
            //Check aginst PhysX colliders if we didn't hit a player
            else if (hitinfo.Collider != null)
            {
                fireDirection = (hitinfo.Point - aimPoint.position).normalized;
                hitDistance = hitinfo.Distance;

                Debug.DrawRay(cameraPosition, aimForwardVector * hitDistance, new Color(0, 0.4f, 0), 1);
            }
            else
            {
                Debug.DrawRay(cameraPosition, fireDirection * hitDistance, Color.gray, 1);

                fireDirection = ((cameraPosition + fireDirection * hitDistance) - aimPoint.position).normalized;
            }
        }*/

        //Reset hit distance
        hitDistance = maxHitDistance;

        //Check if we hit anything with the fire
        Runner.LagCompensation.Raycast(aimPoint.position, fireDirection, maxHitDistance, Object.InputAuthority, out hitinfo, collisionLayers, HitOptions.IgnoreInputAuthority | HitOptions.IncludePhysX);

        //Check against other players
        if (hitinfo.Hitbox != null)
        {
            hitDistance = hitinfo.Distance;
            HPHandler hitHPHandler = null;

            if (Object.HasStateAuthority)
            {
                hitHPHandler = hitinfo.Hitbox.transform.root.GetComponent<HPHandler>();
                Debug.DrawRay(aimPoint.position, fireDirection * hitDistance, Color.red, 1);

                return hitHPHandler;
            }
        }
        //Check aginst PhysX colliders if we didn't hit a player
        else if (hitinfo.Collider != null)
        {
            hitDistance = hitinfo.Distance;

            Debug.DrawRay(aimPoint.position, fireDirection * hitDistance, Color.green, 1);
        }
        else Debug.DrawRay(aimPoint.position, fireDirection * hitDistance, Color.black, 1);

        return null;
    }
    private bool IsWeaponFireAllowed(WeaponInstallationType weaponType)
    {
	    if (!_player.IsActivated)
		    return false;

	    // Has the selected weapon become fully visible yet? If not, don't allow shooting
	    if (weaponType == WeaponInstallationType.PRIMARY && _activePrimaryWeapon != selectedPrimaryWeapon)
		    return false;
	    if (weaponType == WeaponInstallationType.SECONDARY && _activeSecondaryWeapon != selectedSecondaryWeapon)
		    return false;
	    return true;
    }
    
    public void ResetAllWeapons()
    {
	    ResetWeapon(WeaponInstallationType.PRIMARY);
	    ResetWeapon(WeaponInstallationType.SECONDARY);
    }

    void ResetWeapon(WeaponInstallationType weaponType)
    {
	    if (weaponType == WeaponInstallationType.PRIMARY)
	    {
		    ActivateWeapon(weaponType, 0);
	    }
	    else if (weaponType == WeaponInstallationType.SECONDARY)
	    {
		    ActivateWeapon(weaponType, 4);
	    }
    }
    
    /*public void InstallWeapon(PowerupElement powerup)
    {
	    int weaponIndex = GetWeaponIndex(powerup.powerupType);
	    ActivateWeapon(powerup.weaponInstallationType, weaponIndex);
    }*/

    /*private int GetWeaponIndex(PowerupType powerupType)
    {
	    for (int i = 0; i < _weapons.Length; i++)
	    {
		    if (_weapons[i].powerupType == powerupType)
			    return i;
	    }

	    Debug.LogError($"Weapon {powerupType} was not found in the weapon list, returning <color=red>0 </color>");
	    return 0;
    }*/
}
