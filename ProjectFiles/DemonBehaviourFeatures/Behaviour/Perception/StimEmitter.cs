// using AdvancedController; // see https://www.youtube.com/watch?v=jSauntZrQro  toto nebudeme pouzivat
using UnityEngine;

namespace Perception {
    public class StimEmitter : MonoBehaviour {
        [SerializeField] float walkSpeed = 1.4f;
        [SerializeField] float sprintSpeed = 3.6f;
        [SerializeField] float walkRadius = 8f;
        [SerializeField] float sprintRadius = 18f;

        [SerializeField] bool verbose = false;
        
        scr_CharacterController player;
        Transform body;

        void Awake() {
            player = GetComponent<scr_CharacterController>();
            body = transform;
        }

        void Update() {
            if (!player || !body) return;
            // var vel = player.GetMovementVelocity(); 
            // var speed = vel.magnitude; 

            var vel = player.GetCurrentMovementVector(); // Assuming GetMovementVelocity() is a method that returns the player's velocity as a Vector3
            var speed = vel.magnitude;

            if (verbose) Debug.Log($"Player speed: {speed}, walkSpeed: {walkSpeed}, sprintSpeed: {sprintSpeed}, velocity: {player.GetCurrentMovementVector()}");
            
            if (speed < walkSpeed) return;
            var loud = speed >= sprintSpeed;
            
            PerceptionHub.Emit(
                new Stim(
                    loud ? StimType.AudioLoud : StimType.AudioMovement,
                    body,
                    body.position,
                    loud ? sprintRadius : walkRadius)
                );
        }
    }
}