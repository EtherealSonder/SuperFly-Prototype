using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public PlayerMovement playerMovement;
    public PlayerFlight playerFlight;
    public PlayerManager manager;
    public Animator animator;

    private int isGroundedHash;
    private int isFallingAirHash;
    private int isFallingLandingHash;
    private int isFlyingHash;
    private int moveAmountHash;
    private int isBoostingHash;


    void Start()
    {
        isGroundedHash = Animator.StringToHash("isGrounded");
        isFallingAirHash = Animator.StringToHash("isFallingAir");
        isFallingLandingHash = Animator.StringToHash("isFallingLanding");
        isFlyingHash = Animator.StringToHash("isFlying");
        moveAmountHash = Animator.StringToHash("moveAmount");
        isBoostingHash = Animator.StringToHash("isBoosting");


    }

    void LateUpdate()
    {
        bool isFlying = manager.currentState == PlayerManager.PlayerState.Flying;

        animator.SetBool(isFlyingHash, isFlying);
        animator.SetBool(isBoostingHash, isFlying && playerFlight.IsBoosting);

        if (isFlying)
        {
            // Use flight move input
            float flightMove = playerFlight.CurrentInput.magnitude;
            animator.SetFloat(moveAmountHash, flightMove);
        }
        else
        {
            // Use ground logic
            float groundMove = playerMovement.moveValue.magnitude;
            animator.SetFloat(moveAmountHash, groundMove);

            animator.SetBool(isGroundedHash, playerMovement._AniGrounded);
            animator.SetBool(isFallingAirHash, playerMovement.isFallingAir);
            animator.SetBool(isFallingLandingHash, playerMovement.isFallingLanding);
        }
    }

}
