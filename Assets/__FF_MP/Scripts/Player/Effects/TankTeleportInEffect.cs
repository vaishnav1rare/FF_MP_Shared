using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace OneRare.FoodFury.Multiplayer
{
    public class TankTeleportInEffect : MonoBehaviour
    {
        private Player _player;

        [Header("Time Settings")]
        [FormerlySerializedAs("_timeBeforeParticles")] 
        [SerializeField] private float timeBeforeParticles = 0.1f;
        [FormerlySerializedAs("_timeDelayImpactParticles")] 
        [SerializeField] private float timeDelayImpactParticles = 0.2f;

        [Header("Visuals")] 
        [SerializeField] private GameObject teleportTarget;
        [SerializeField] private ParticleSystem teleportBeamParticle;
        [SerializeField] private ParticleSystem teleportImpactParticle;
        [SerializeField] private ParticleSystem energyDischargeParticle;
        [SerializeField] private GameObject tankDummy;

        [Header("Audio")] 
        [SerializeField] private AudioEmitter audioEmitter;
        [SerializeField] private AudioClipData beamAudioClip;
        [SerializeField] private AudioClipData dischargeAudioClip;

        private Transform _tankDummyTurret;
        private Transform _tankDummyHull;

        private bool _endTeleportation;

        // Initialize dummy tank and set colors based on the assigned player
        public void Initialize(Player player)
        {
            _player = player;

            _tankDummyTurret = tankDummy.transform.Find("EnergyTankIn_Turret");
            _tankDummyHull = tankDummy.transform.Find("EnergyTankIn_Hull");

            ColorChanger.ChangeColor(transform, player.PlayerColor);

            ResetTeleporter();
        }

        public void EndTeleport()
        {
            _endTeleportation = true;
        }

        private void ResetTeleporter()
        {
            _endTeleportation = false;
            teleportTarget.SetActive(false);
            teleportBeamParticle.Stop();
            teleportImpactParticle.Stop();
            energyDischargeParticle.Stop();
            tankDummy.SetActive(false);
        }

        public void StartTeleport()
        {
            ResetTeleporter();
            StartCoroutine(TeleportIn());
        }

        private IEnumerator TeleportIn()
        {
            teleportTarget.SetActive(true);
            yield return new WaitForSeconds(timeBeforeParticles);

            // Play the downwards beam
            teleportBeamParticle.Play();
            //_audioEmitter.PlayOneShot(_beamAudioClip);
            yield return new WaitForSeconds(timeDelayImpactParticles);

            // Play impact particle
            teleportTarget.SetActive(false);
            teleportBeamParticle.Stop();
            teleportImpactParticle.Play();

            // Set the dummy tank
            tankDummy.SetActive(true);
            //_tankDummyTurret.rotation = _player.turretRotation;
            //_tankDummyHull.rotation = _player.hullRotation;

            // Waits for the tank to be ready before playing the discharge effect
            while (!_endTeleportation)
                yield return null;

            tankDummy.SetActive(false);
            energyDischargeParticle.Play();
            //_audioEmitter.PlayOneShot(_dischargeAudioClip);
        }
    }
}
