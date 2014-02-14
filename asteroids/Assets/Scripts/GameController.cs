﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

enum GAME_STATE
{
    MAIN_MENU,
    LEVEL_STARTING,
    PLAYING,
    LEVEL_FINISHED,
    PLAYER_DESTROYED,
    GAME_OVER,
    HIGH_SCORES
}
;

public class GameController : MonoBehaviour
{
    public GameObject player_ship_prefab_;
    public GameObject hud_player_life_prefab_;
    public Asteroid asteroid_prefab_;
    public GameObject music_player_prefab_;

    public float max_asteroid_speed_;
    public float min_asteroid_speed_;
    public float max_asteroid_torque_;
    public float min_asteroid_torque_;
    public float score_to_life_rate_;
    public float seconds_to_respawn_;
    public float hud_life_distance_;
    public float blinking_delay_;

    private int current_level_;
    private int max_score_;
    private int max_asteroids_;
    private GUIText current_score_text_;
    private GUIText info_text_;
    private GUIText max_score_text_;
    private GAME_STATE current_state_;
    private int current_score_;
    private int num_lives_;
    private bool num_lives_changed_;
    private List<GameObject> hud_player_lives_;
    private GameObject player_ship_intance_;
    private float respawn_timer_;
    private GameObject music_player_instance_;

    // Use this for initialization
    void Start()
    {
        current_level_ = 1;
        max_score_ = 0;
        max_asteroids_ = 5;

        GameObject score_game_object = new GameObject("ScoreText");
        current_score_text_ = score_game_object.AddComponent<GUIText>();
        current_score_text_.gameObject.transform.position = new Vector3(0.1f, 0.95f, 0.0f);
        current_score_text_.text = "0";

        GameObject info_text_object = new GameObject("InfoText");
        info_text_ = info_text_object.AddComponent<GUIText>();
        info_text_.gameObject.transform.position = new Vector3(0.5f, 0.9f, 0.0f);
        info_text_.guiText.alignment = TextAlignment.Center;
        info_text_.guiText.anchor = TextAnchor.MiddleCenter;

        GameObject max_score_object = new GameObject("MaxScore");
        max_score_text_ = max_score_object.AddComponent<GUIText>();
        max_score_text_.gameObject.transform.position = new Vector3(0.5f, 0.95f, 0.0f);
        max_score_text_.guiText.alignment = TextAlignment.Center;
        max_score_text_.guiText.anchor = TextAnchor.MiddleCenter;
        max_score_text_.text = "0";

        current_state_ = GAME_STATE.MAIN_MENU;
        hud_player_lives_ = new List<GameObject>();       
    }

    public void AddScore(int score)
    {
        current_score_ += score;
        current_score_text_.text = current_score_.ToString();
        if (current_score_ % score_to_life_rate_ == 0)
        {
            num_lives_++;
            AddLifeHUD();
        }
    }

    void Init()
    {
        num_lives_ = 3;
        for (int i = 0; i < num_lives_; i++)
        {
            AddLifeHUD();
        }
        SpawnPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        switch (current_state_)
        {
            case GAME_STATE.MAIN_MENU:                
                info_text_.text = "PUSH START";
                if (!IsInvoking("BlinkInfoText"))
                {
                    InvokeRepeating("BlinkInfoText", 0.0f, blinking_delay_);
                }
                if (GameObject.FindGameObjectsWithTag("Asteroid").Length < max_asteroids_)
                {
                    GenerateAsteroid();
                }
                                
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Init();
                    current_state_ = GAME_STATE.LEVEL_STARTING;
                    CancelInvoke("BlinkInfoText");
                }
                break;
            case GAME_STATE.LEVEL_STARTING:
                CleanAsteroids();
                InitLevel();
                current_state_ = GAME_STATE.PLAYING;
                music_player_instance_ = (GameObject)Instantiate(music_player_prefab_);
                break;
            case GAME_STATE.PLAYING:                
                info_text_.enabled = false;
                if (player_ship_intance_ != null && !player_ship_intance_.GetComponent<PlayerShip>().IsPlayerAlive())
                {
                    current_state_ = GAME_STATE.PLAYER_DESTROYED;
                }
                break;
            case GAME_STATE.LEVEL_FINISHED:
                current_level_++;
                current_state_ = GAME_STATE.LEVEL_STARTING;
                break;
            case GAME_STATE.PLAYER_DESTROYED:
                OnPlayerDestruction();
                respawn_timer_ += Time.deltaTime;
                if (respawn_timer_ > seconds_to_respawn_)
                {
                    SpawnPlayer();
                    current_state_ = GAME_STATE.PLAYING;
                    music_player_instance_ = (GameObject)Instantiate(music_player_prefab_);
                }
                break;
            case GAME_STATE.HIGH_SCORES:
                break;
            case GAME_STATE.GAME_OVER:
                if (current_score_ > max_score_)
                {
                    max_score_ = current_score_;
                    current_score_ = 0;
                    max_score_text_.text = max_score_.ToString();
                }
                info_text_.enabled = true;
                info_text_.text = "GAME OVER";
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    CleanAsteroids();
                    current_score_text_.text = "0";
                    current_state_ = GAME_STATE.MAIN_MENU;
                }
                break;
        }
    }

    void AddLifeHUD()
    {
        float shift = hud_player_lives_.Count * hud_life_distance_;
        GameObject hud_life = (GameObject)GameObject.Instantiate(hud_player_life_prefab_);
        Vector3 position = Camera.main.ViewportToWorldPoint(new Vector3(0.08f + shift, 0.9f, 0.0f));
        position.z = 0.0f;
        hud_life.transform.position = position;
        hud_player_lives_.Add(hud_life);
    }

    void OnPlayerDestruction()
    {
        if (player_ship_intance_ != null)
        {
            Destroy(player_ship_intance_);
            if (hud_player_lives_.Count > 0)
            {
                Destroy(hud_player_lives_[num_lives_ - 1]);
                hud_player_lives_.RemoveAt(num_lives_ - 1);
                num_lives_ -= 1;
                if (num_lives_ > 0)
                {
                    current_state_ = GAME_STATE.PLAYER_DESTROYED;
                }
                else
                {
                    current_state_ = GAME_STATE.GAME_OVER;
                }
            }
            respawn_timer_ = 0.0f;
        }
        Destroy(music_player_instance_);
    }

    void GenerateAsteroid()
    {
        int corner = Random.Range(1, 5); //1 = Left, 2 = Top, 3 = Right, 4 = Bottom        
        float x = 0.0f;
        float y = 0.0f;
        float x_dir = 0.0f;
        float y_dir = 0.0f;
        float min_dist_threshold = 0.1f; // This value is to prevent asteroids from having a straight line trajectory in the screen corners; 
        //they should go near the center of the screen        
        switch (corner)
        {
            case 1:
                y = Random.Range(0.0f, 1.0f);
                x_dir = 1.0f;
                y_dir = Random.Range(0.0f, 1.0f);
                while (Mathf.Abs(y - y_dir) < min_dist_threshold)
                {
                    y_dir = Random.Range(0.0f, 1.0f);
                }
                break;
            case 2:
                x = Random.Range(0.0f, 1.0f);
                y = 1.0f;

                x_dir = Random.Range(0.0f, 1.0f);
                while (Mathf.Abs(x - x_dir) < min_dist_threshold)
                {
                    x_dir = Random.Range(0.0f, 1.0f);
                }

                y_dir = 0.0f;
                break;
            case 3:
                x = 1.0f;
                y = Random.Range(0.0f, 1.0f);

                x_dir = 0.0f;
                y_dir = Random.Range(0.0f, 1.0f);
                while (Mathf.Abs(y - y_dir) < min_dist_threshold)
                {
                    y_dir = Random.Range(0.0f, 1.0f);
                }
                break;
            case 4:
                x = Random.Range(0.0f, 1.0f);

                x_dir = Random.Range(0.0f, 1.0f);
                while (Mathf.Abs(x - x_dir) < min_dist_threshold)
                {
                    x_dir = Random.Range(0.0f, 1.0f);
                }
                y_dir = 1.0f;
                break;

        }
        Vector3 position = Camera.main.ViewportToWorldPoint(new Vector3(x, y, 0.0f));
        position.z = 0.0f;

        Vector3 direction = Camera.main.ViewportToWorldPoint(new Vector3(x_dir, y_dir, 0.0f)) - position;
        direction.z = 0.0f;
        direction.Normalize();

        Asteroid asteroid = (Asteroid)GameObject.Instantiate(asteroid_prefab_, position, Quaternion.identity);
        float asteroid_speed = Random.Range(1, 11);
        asteroid.rigidbody2D.velocity = direction * asteroid_speed * Time.deltaTime;

        if (asteroid.rigidbody2D.velocity.magnitude > max_asteroid_speed_)
        {
            asteroid.rigidbody2D.velocity = asteroid.rigidbody2D.velocity.normalized * max_asteroid_speed_;
        }
        if (asteroid.rigidbody2D.velocity.magnitude < min_asteroid_speed_)
        {
            asteroid.rigidbody2D.velocity = asteroid.rigidbody2D.velocity.normalized * ((max_asteroid_speed_ + min_asteroid_speed_) / 2.0f);
        }

        float torque = Random.Range(min_asteroid_torque_, max_asteroid_torque_);
        asteroid.rigidbody2D.AddTorque(torque);
    }


    void SpawnPlayer()
    {
        player_ship_intance_ = (GameObject)Instantiate(player_ship_prefab_, Vector3.zero, Quaternion.identity);
    }

    void BlinkInfoText()
    {
        info_text_.enabled = !info_text_.enabled;
    }

    void CleanAsteroids()
    {
        GameObject[] asteroids = GameObject.FindGameObjectsWithTag("Asteroid");
        for (int i = 0; i < asteroids.Length; i++)
        {
            Destroy(asteroids[i]);
        }
    }

    void InitLevel()
    {
        switch (current_level_)
        {
            case 1:
                break;
            case 2:
                max_asteroids_++;
                break;
        }
        for(int i = 0; i < max_asteroids_; i++)
        {
            GenerateAsteroid();
        }

    }    
}