using Unity.Cinemachine;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    public enum PlayerState { Grounded, Flying }
    public PlayerState currentState = PlayerState.Grounded;

    public IPlayerMode playerMode { get; private set; }
    enum Mode { ThirdPerson = 0, FirstPerson, None }
    [SerializeField] Mode startMode;
    [SerializeField] CinemachineCamera firstPerson;
    [SerializeField] CinemachineCamera thirdPerson;
    [SerializeField] Transform PlayerMesh;
    [SerializeField] Transform Player;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerFlight playerFlight;
    [SerializeField] private BetterFalls betterFalls;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Rigidbody rb;

    Mode currentMode = Mode.None;
    void SwitchPlayerModeTo(Mode to)
    {
        if (currentMode != to)
        {
            if (to == Mode.ThirdPerson)
            {
                currentMode = Mode.ThirdPerson;
                playerMode = new ThirdPersonMode();
                PlayerMesh.gameObject.SetActive(true);
                thirdPerson.Priority.Value = 2;
                firstPerson.Priority.Value = 1;
            }
            else if (to == Mode.FirstPerson)
            {
                currentMode = Mode.FirstPerson;
                playerMode = new FirstPersonMode();
                PlayerMesh.gameObject.SetActive(false);
                thirdPerson.Priority.Value = 1;
                firstPerson.Priority.Value = 2;
                CinemachinePanTilt panTilt = firstPerson.GetComponent<CinemachinePanTilt>();
                panTilt.PanAxis.Value = Player.rotation.eulerAngles.y;
                panTilt.TiltAxis.Value = Player.rotation.eulerAngles.x;
            }
        }
    }


    void Start()
    {
        SwitchPlayerModeTo(startMode);
        inputReader.jumpEvent += OnJumpPressed;
    }

    private void OnDestroy()
    {
        inputReader.jumpEvent -= OnJumpPressed;
    }

    private void OnJumpPressed()
    {
        if (currentState == PlayerState.Grounded)
        {
            currentState = PlayerState.Flying;

            playerMovement.SetActiveState(false);
            betterFalls.enabled = false;
            playerFlight.SetActiveState(true);

            rb.useGravity = false;

            playerFlight.TriggerLaunchBoost();
        }
        else
        {
            currentState = PlayerState.Grounded;

            playerFlight.SetActiveState(false);
            playerMovement.SetActiveState(true);
            betterFalls.enabled = true;

            rb.useGravity = true;
        }

    }

    void Update()
    {
      
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Inside"))
        {
            SwitchPlayerModeTo(Mode.FirstPerson);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Inside"))
        {
            SwitchPlayerModeTo(Mode.ThirdPerson);
        }
    }
}
