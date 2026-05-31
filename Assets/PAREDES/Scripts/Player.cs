using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{

    public GameObject WallBounceEffectObj;
    public GameObject DeadEffectObj;
    public GameObject JumpEffectObj;
    public CameraShake cameraShake;


    GameManager GameManagerScript;
    Rigidbody2D rb;
    Vector3 _spawnPosition;


    [HideInInspector]
    public bool isDead = false;
    bool isFirstTouch = true;



    float hueValue;

    AudioSource source;
    [Space]
    public AudioClip jumpClip;
    public AudioClip deadClip;

    [Space]
    public int JumpSpeed_X;
    public int JumpSpeed_Y;

    public int Gravity;



    void Start()
    {
        source = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();

        GameManagerScript = GameObject.Find("GameManager").GetComponent<GameManager>();
        _spawnPosition = transform.position;

        hueValue = WallsGameColors.RandomHueStep();

        SetBackgroundColor();
        StopPlayer();
    }


    void LateUpdate()
    {
        if (source == null)
            return;
        var dir = WallsAudioDirector.Instance;
        if (dir != null && dir.HasMixer)
            source.volume = 1f;
        else if (dir != null)
            source.volume = dir.GetSfxLinear();
        else
            source.volume = 1f;
    }

    void Update()
    {
        rb.gravityScale = Gravity;

        if (isDead) return;

        if (WallsPauseMenu.BlockGameplayInput)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isFirstTouch)
            {
                isFirstTouch = false;
                StartPlayer();
            }
            else
            {
                GameObject effectObj = Instantiate(JumpEffectObj, transform.position, Quaternion.identity);
                Destroy(effectObj, 0.5f);
                source.PlayOneShot(jumpClip, 1);

                if (rb.linearVelocity.x > 0)
                {
                    rb.linearVelocity = new Vector2(JumpSpeed_X, JumpSpeed_Y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(-JumpSpeed_X, JumpSpeed_Y);
                }
            }


        }


    }



    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Wall")
        {
            source.PlayOneShot(jumpClip, 1);

            if (other.gameObject.name == "Left" || other.gameObject.name == "Right")
            {
                GameManagerScript.addScore();


                GameObject.Find("GameManager").GetComponent<TriangleManager>().WallTouched(other.gameObject.name);


            }

            GameObject effectObj = Instantiate(WallBounceEffectObj, other.contacts[0].point, Quaternion.identity);
            Destroy(effectObj, 0.5f);

            SetBackgroundColor();
        }

        if (other.gameObject.tag == "Triangle" && isDead == false)
        {
            source.PlayOneShot(deadClip, 1);
            isDead = true;

            GameObject effectObj = Instantiate(DeadEffectObj, other.contacts[0].point, Quaternion.identity);
            Destroy(effectObj, 0.5f);

            GameManagerScript.Gameover();

            if (WallsGamePrefs.ScreenShakeEnabled && cameraShake != null)
                StartCoroutine(cameraShake.Shake(0.2f, 0.3f));

            StopPlayer();

        }

    }

    public void StartPlayer()
    {
        rb.linearVelocity = new Vector2(-1, 0);
        rb.isKinematic = false;
        // isStarted = true;
    }


    void StopPlayer()
    {
        rb.linearVelocity = new Vector2(0, 0);
        rb.isKinematic = true;
    }

    /// <summary>
    /// Após anúncio recompensado: mesma pontuação e espinhos; bola no início à espera do primeiro toque.
    /// </summary>
    public void PrepareReviveFromReward()
    {
        isDead = false;
        isFirstTouch = true;
        transform.position = _spawnPosition;
        StopPlayer();
    }

    void SetBackgroundColor()
    {
        hueValue += 0.1f;
        if (hueValue >= 1)
        {
            hueValue = 0;
        }
        var cam = Camera.main;
        if (cam == null)
            return;
        cam.backgroundColor = WallsGameColors.ColorFromHue(hueValue);
        ThemeManager.ApplyBaseColor(cam.backgroundColor);
        WallsGamePrefs.LastRunBackground.Save(cam.backgroundColor);
    }


}
