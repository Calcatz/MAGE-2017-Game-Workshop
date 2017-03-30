using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class CharacterCombat : NetworkBehaviour {

    public GameObject body;
    public GameObject arms;
    public Transform aimOrigin;
    public int health;
    public int ammo;
    public int attack;
    public AudioClip gunshotSound;
    public AudioClip reloadSound;
    public ParticleSystem shootParticle;

    UIElements uiElements;
    Image reloadImage;
    Text ammoText;
    Image healthImage;
    Image capturedTimeImage;
    Text gameOverText;
    
    float capturedTime;
    int maxAmmo;
    int maxHealth;
    bool isShooting;
    bool gameOver;
    Transform spawnPoint;

    //Fungsi ini hanya dipanggil satu kali ketika game baru dimulai
    private void Start() {
        uiElements = GameObject.Find("Canvas").GetComponent<UIElements>();
        gameOverText = uiElements.gameOverText;

        if (!isLocalPlayer) {
            arms.SetActive(false);
            return;
        }

        GetComponentInChildren<Camera>().enabled = true;
        GetComponentInChildren<AudioListener>().enabled = true;
        body.SetActive(false);
        if (isServer) {
            spawnPoint = GameObject.Find("Spawn Point 1").transform;
        } else {
            spawnPoint = GameObject.Find("Spawn Point 2").transform;
        }
        transform.position = spawnPoint.position;

        reloadImage = uiElements.reloadImage;
        ammoText = uiElements.ammoText;
        healthImage = uiElements.healthImage;
        capturedTimeImage = uiElements.capturedTimeImage;
        
        capturedTime = 0;
        maxAmmo = ammo;
        maxHealth = health;
        ammoText.text = ammo + "/" + maxAmmo;
        healthImage.transform.localScale = new Vector3((float)health / maxHealth, 1, 1);
        capturedTimeImage.transform.localScale = new Vector3((float)capturedTime / 30f, 1, 1);
    }

    //Fungsi ini diulang terus menerus selama game berjalan
	private void Update () {
        if (!isLocalPlayer) {
            return;
        }

        if (Input.GetButton("Horizontal") || Input.GetButton("Vertical")) {
            GetComponent<Animator>().SetBool("isRunning", true);
        } else {
            GetComponent<Animator>().SetBool("isRunning", false);
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            GetComponent<Animator>().SetTrigger("jump");
        }
        Shoot();
	}

    private void Shoot() {
        if (Input.GetMouseButton(0) && !isShooting) {   //Bisa shoot jika tidak sedang menembak
            if (ammo > 0) {
                isShooting = true;
                ammo--;
                ammoText.text = ammo + "/" + maxAmmo;
                StartCoroutine(ShootingCoroutine());
                if (shootParticle != null) {
                    shootParticle.Play();
                }
            }
        }
    }

    private IEnumerator ShootingCoroutine() {
        if (!Network.isServer) {
            CmdRaycast();
        }
        else {
            RpcRaycast();
        }

        AudioSource.PlayClipAtPoint(gunshotSound, arms.transform.position);
        //Menganimasikan gerakan tangan
        while (arms.transform.localPosition.z > -0.15f) {
            arms.transform.localPosition -= new Vector3(0, 0, 2*Time.deltaTime);
            yield return null;
        }
        while (arms.transform.localPosition.z < 0f) {
            arms.transform.localPosition += new Vector3(0, 0, Time.deltaTime);
            yield return null;
        }

        if (ammo == 0) {
            StartCoroutine(ReloadCoroutine());
        } else {
            isShooting = false;
        }
    }

    [Command]
    private void CmdRaycast() {
        RpcRaycast();
    }

    [ClientRpc]
    private void RpcRaycast() {
        Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity)) {
            Debug.DrawLine(aimOrigin.position, hit.point, Color.red);
            if (hit.collider.tag == "Player") {
                CharacterCombat enemy = hit.collider.gameObject.GetComponent<CharacterCombat>();
                if (!enemy.isServer) {
                    enemy.CmdHurt(50);
                }
                else {
                    enemy.RpcHurt(50);
                }
            }
        }
    }

    private IEnumerator ReloadCoroutine() {
        reloadImage.transform.parent.gameObject.SetActive(true);
        reloadImage.fillAmount = 0;
        while (reloadImage.fillAmount < 1) {
            reloadImage.fillAmount += 0.5f * Time.deltaTime;
            yield return null;
        }
        AudioSource.PlayClipAtPoint(reloadSound, arms.transform.position);
        ammo = maxAmmo;
        ammoText.text = ammo + "/" + maxAmmo;
        isShooting = false;
        reloadImage.transform.parent.gameObject.SetActive(false);
    }

    public void Hurt(int damage) {
        health -= damage;
        healthImage.transform.localScale = new Vector3((float) health / maxHealth, 1, 1);
    }

    [Command]
    public void CmdHurt(int damage) {
        RpcHurt(damage);
    }

    [ClientRpc]
    public void RpcHurt(int damage) {
        if (!isLocalPlayer) {
            return;
        }
        health -= damage;
        if (health <= 0) {
            transform.position = spawnPoint.position;
            health = maxHealth;
        }
        healthImage.transform.localScale = new Vector3((float) health / maxHealth, 1, 1);
    }

    void OnTriggerStay(Collider col) {
        if (!isLocalPlayer) {
            return;
        }

        if (col.gameObject.tag == "CapturePoint") {
            if (!gameOver) {
                capturedTime += Time.deltaTime;
                capturedTimeImage.transform.localScale = new Vector3((float)capturedTime / 30f, 1, 1);
                if (capturedTime >= 30f) {
                    gameOver = true;
                    if (!Network.isServer) {
                        CmdGameOver();
                    }
                    else {
                        RpcGameOver();
                    }
                }
            }
        }
    }

    [Command]
    void CmdGameOver() {
        RpcGameOver();
    }

    [ClientRpc]
    void RpcGameOver() {
        GameObject.Find("Epic Music").GetComponent<AudioSource>().Stop();
        if (gameOver) {
            gameOverText.text = "Victory!";
            GameObject.Find("Victory").GetComponent<AudioSource>().Play();
            Time.timeScale = 0.5f;
        } else {
            gameOverText.text = "Defeat";
            GameObject.Find("Defeat").GetComponent<AudioSource>().Play();
            Time.timeScale = 0.5f;
        }
        gameOverText.gameObject.SetActive(true);
        gameOver = true;
        StartCoroutine(MainMenuDelay());
    }

    IEnumerator MainMenuDelay() {
        yield return new WaitForSeconds(3f);
        Time.timeScale = 1;
        if (!Network.isServer) {
            NetworkManager.singleton.StopClient();
        }
        else {
            NetworkManager.singleton.StopHost();
        }
        NetworkServer.Reset();
        SceneManager.LoadScene("Main Menu");
    }
}
