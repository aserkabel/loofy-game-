using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// ==========================================
// 1. MASTER GAME MANAGER & STORY CONTROLLER
// ==========================================
public class LoofyGameController : MonoBehaviour
{
    public static LoofyGameController instance;

    [Header("--- UI & HUD ELEMENTS ---")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI dialogueText;
    public GameObject dialogueBox;
    public GameObject fadeToBlackUI;
    public GameObject powerOutAlertText;

    [Header("--- AUDIO ---")]
    public AudioSource playerAudioSource;
    public AudioSource typeSound;
    public AudioClip powerZapSound;

    [Header("--- HOUSE & 3D LIGHTS ---")]
    public GameObject pcScreenLight;
    public GameObject houseLightsGroup;
    public GameObject parentsDoorLock;
    public GameObject monsterStalkerModel;
    public GameObject monsterWindowEnding;

    [Header("--- 3D COMBAT & ORBS ---")]
    public GameObject orbPrefab;
    public Transform throwPoint;
    public float throwForce = 25f;
    public int monsterHealth = 3;

    // --- GAME STATES ---
    public enum GameState { Intro, FindBedroom, SearchFuses, PowerRestored, OrbCombat, Ending }
    [HideInInspector] public GameState currentState;

    private int fusesCollected = 0;
    private int totalFusesNeeded = 8;
    private int orbsHeld = 0;

    // Interaction Flags
    private bool nearBedroom = false;
    private bool nearPowerBox = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentState = GameState.Intro;
        if (houseLightsGroup != null) houseLightsGroup.SetActive(false);
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (fadeToBlackUI != null) fadeToBlackUI.SetActive(false);
        if (powerOutAlertText != null) powerOutAlertText.SetActive(false);
        if (monsterStalkerModel != null) monsterStalkerModel.SetActive(false);
        if (monsterWindowEnding != null) monsterWindowEnding.SetActive(false);

        StartCoroutine(PlayIntroSequence());
    }

    void Update()
    {
        // 1. Bedroom Interaction ('E' key)
        if (currentState == GameState.FindBedroom && nearBedroom && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(PlayNightmareSequence());
        }

        // 2. Power Box Interaction ('E' key)
        if (currentState == GameState.SearchFuses && nearPowerBox && Input.GetKeyDown(KeyCode.E))
        {
            if (fusesCollected >= totalFusesNeeded)
            {
                RestorePower();
            }
            else
            {
                int remaining = totalFusesNeeded - fusesCollected;
                StartCoroutine(ShowTypewriter("Power Box", "Missing " + remaining + " more fuses to restore power!", 2.5f));
            }
        }

        // 3. Throw Orbs (Left Click during Orb Combat)
        if (currentState == GameState.OrbCombat && Input.GetMouseButtonDown(0) && orbsHeld > 0)
        {
            ThrowOrb();
        }
    }

    // --- STORY SEQUENCES ---

    IEnumerator PlayIntroSequence()
    {
        yield return ShowTypewriter("Loofy", "Just one more match and I'm logging off...", 3f);

        // Power Zap Sound & Screen Off
        if (powerZapSound != null && playerAudioSource != null) 
        {
            playerAudioSource.PlayOneShot(powerZapSound);
        }
        if (pcScreenLight != null) pcScreenLight.SetActive(false);

        if (powerOutAlertText != null) powerOutAlertText.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        if (powerOutAlertText != null) powerOutAlertText.SetActive(false);

        // Dad Grounding Scene
        yield return ShowTypewriter("Loofy", "Wait... why did the power go out in the middle of my match?!", 3f);
        yield return ShowTypewriter("Dad", "Loofy! Stay in your room, I'm going outside to check the circuit breaker.", 3.5f);
        yield return ShowTypewriter("Dad", "Loofy, ur grounded for cutting the wires!", 4f);

        // Objective Update
        currentState = GameState.FindBedroom;
        SetObjective("Objective: Find your bedroom and go to sleep");
    }

    IEnumerator PlayNightmareSequence()
    {
        currentState = GameState.Intro; // Pause interactions during fade
        if (fadeToBlackUI != null) fadeToBlackUI.SetActive(true);
        yield return new WaitForSeconds(1.5f);

        // Nightmare Monster Stalker Flash
        if (monsterStalkerModel != null) monsterStalkerModel.SetActive(true);
        yield return new WaitForSeconds(1f);
        if (monsterStalkerModel != null) monsterStalkerModel.SetActive(false);

        if (fadeToBlackUI != null) fadeToBlackUI.SetActive(false);

        yield return ShowTypewriter("Loofy", "What a horrible dream... Their door is locked! I need to get electricity back on.", 4f);

        currentState = GameState.SearchFuses;
        SetObjective("Fuses Collected: 0 / " + totalFusesNeeded);
    }

    public void CollectFuse()
    {
        fusesCollected++;
        SetObjective("Fuses Collected: " + fusesCollected + " / " + totalFusesNeeded);
        StartCoroutine(ShowTypewriter("Loofy", "Found a fuse! (" + fusesCollected + "/" + totalFusesNeeded + ")", 2f));
    }

    void RestorePower()
    {
        currentState = GameState.PowerRestored;
        if (houseLightsGroup != null) houseLightsGroup.SetActive(true);
        if (parentsDoorLock != null) Destroy(parentsDoorLock);

        StartCoroutine(PlayPhaseTwoSequence());
    }

    IEnumerator PlayPhaseTwoSequence()
    {
        yield return ShowTypewriter("Loofy", "The power is back on! Mom? Dad?!", 3f);
        yield return ShowTypewriter("Loofy", "NO! That white monster... it did this to them! Search for Orbs to fight it!", 4f);

        currentState = GameState.OrbCombat;
        SetObjective("Objective: Search for Glowing Orbs & Throw them [Left Click] at the monster! (Orbs: " + orbsHeld + ")");
    }

    public void CollectOrb()
    {
        orbsHeld++;
        SetObjective("Objective: Throw Orbs [Left Click] at the monster! (Orbs: " + orbsHeld + ")");
    }

    void ThrowOrb()
    {
        orbsHeld--;
        SetObjective("Objective: Throw Orbs [Left Click] at the monster! (Orbs: " + orbsHeld + ")");

        if (orbPrefab != null && throwPoint != null)
        {
            GameObject orb = Instantiate(orbPrefab, throwPoint.position, throwPoint.rotation);
            Rigidbody rb = orb.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(throwPoint.forward * throwForce, ForceMode.Impulse);
            }
            Destroy(orb, 4f);
        }
    }

    public void HitMonster()
    {
        monsterHealth--;
        if (monsterHealth <= 0)
        {
            if (monsterStalkerModel != null) monsterStalkerModel.SetActive(false);
            StartCoroutine(PlayEndingSequence());
        }
    }

    IEnumerator PlayEndingSequence()
    {
        currentState = GameState.Ending;
        yield return ShowTypewriter("Loofy", "It... it's over... it was all just a nightmare...", 3f);

        if (fadeToBlackUI != null) fadeToBlackUI.SetActive(true);
        yield return new WaitForSeconds(2.5f);

        // Final Reveal: Monster watching outside window
        if (fadeToBlackUI != null) fadeToBlackUI.SetActive(false);
        if (monsterWindowEnding != null) monsterWindowEnding.SetActive(true);

        yield return ShowTypewriter("System", "THE LOOFY HORROR - Made by JayFX the Tuff", 6f);
        yield return new WaitForSeconds(4f);

        SceneManager.LoadScene(0); // Reload Main Menu
    }

    // --- TYPEWRITER & HUD HELPERS ---
    public IEnumerator ShowTypewriter(string speaker, string message, float duration)
    {
        if (dialogueBox != null && dialogueText != null)
        {
            dialogueBox.SetActive(true);
            dialogueText.text = "";
            string fullText = speaker + ": " + message;

            foreach (char letter in fullText.ToCharArray())
            {
                dialogueText.text += letter;
                if (typeSound != null && letter != ' ') typeSound.Play();
                yield return new WaitForSeconds(0.03f);
            }

            yield return new WaitForSeconds(duration);
            dialogueBox.SetActive(false);
        }
    }

    void SetObjective(string text)
    {
        if (objectiveText != null) objectiveText.text = text;
    }

    public void SetNearBedroom(bool status) { nearBedroom = status; }
    public void SetNearPowerBox(bool status) { nearPowerBox = status; }
}


// ==========================================
// 2. 3D PLAYER MOVEMENT
// ==========================================
public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 5f;
    public float gravity = -9.81f;
    private Vector3 velocity;

    void Update()
    {
        if (controller == null) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}


// ==========================================
// 3. 3D FIRST-PERSON CAMERA LOOK
// ==========================================
public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}


// ==========================================
// 4. ITEM INTERACTION (FUSES, ORBS, DOORS)
// ==========================================
public class InteractiveItem : MonoBehaviour
{
    public enum ItemType { Fuse, Orb, BedroomDoor, PowerBox }
    public ItemType itemType;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (itemType == ItemType.BedroomDoor && LoofyGameController.instance != null) 
                LoofyGameController.instance.SetNearBedroom(true);
            if (itemType == ItemType.PowerBox && LoofyGameController.instance != null) 
                LoofyGameController.instance.SetNearPowerBox(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (itemType == ItemType.BedroomDoor && LoofyGameController.instance != null) 
                LoofyGameController.instance.SetNearBedroom(false);
            if (itemType == ItemType.PowerBox && LoofyGameController.instance != null) 
                LoofyGameController.instance.SetNearPowerBox(false);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            if (LoofyGameController.instance == null) return;

            if (itemType == ItemType.Fuse)
            {
                LoofyGameController.instance.CollectFuse();
                Destroy(gameObject);
            }
            else if (itemType == ItemType.Orb)
            {
                LoofyGameController.instance.CollectOrb();
                Destroy(gameObject);
            }
        }
    }
}


// ==========================================
// 5. MONSTER HIT DETECTION
// ==========================================
public class MonsterHitDetector : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Orb") && LoofyGameController.instance != null)
        {
            LoofyGameController.instance.HitMonster();
            Destroy(collision.gameObject);
        }
    }
}
