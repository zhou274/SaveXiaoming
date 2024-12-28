using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent (typeof (PlayerController))]
[RequireComponent(typeof(GunController))]
public class Player : LivingEntity
{
    public float moveSpeed = 5f;
    PlayerController controller;
    GunController gunController;

    Camera viewCamera;

    public Crosshairs crosshair;


    private JoyStickMove joyStickMove;
    private Vector3 detailMove;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        gunController = GetComponent<GunController>();
        viewCamera = Camera.main;
        FindObjectOfType<Spawner>().OnNewWave += OnNewWave;


        this.joyStickMove = FindObjectOfType<JoyStickMove>();
        this.joyStickMove.onMoveStart += this.onMoveStart;
        this.joyStickMove.onMoving += this.onMoving;
        this.joyStickMove.onMoveEnd += this.onMoveEnd;

    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start(); // run Start() in `LivingEntity` first, then this Start()
    }


    void OnNewWave(int waveNumber) {
        health = startingHealth;
        gunController.EquipGun(waveNumber - 1);
    }

    // Update is called once per frame
    void Update()
    {
        // Movement
        //Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        //Vector3 velocity = direction.normalized * moveSpeed;
        //controller.Move(velocity);
        this.transform.Translate(this.detailMove * Time.deltaTime * moveSpeed, Space.World);


        // Look at mouse position
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.forward * gunController.GunHeight);
        float rayDistance;

        if(groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            //Debug.DrawLine(ray.origin, point, Color.red);

            controller.LookAt(point);

            crosshair.transform.position = point;
            crosshair.DetectTarget(ray);

            if ((new Vector2(point.x, point.z) - new Vector2(transform.position.x, transform.position.z)).sqrMagnitude > 1)
            {
                gunController.Aim(point);
            }
            
        }


        // Weapon shoots
        if (Input.GetMouseButtonDown(0))
        {
            
            Vector3 worldMousePosition = Input.mousePosition;
            //Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            if(worldMousePosition.x<Screen.width/4 && worldMousePosition.y<Screen.height/4)
            {
                return;
            }
            else
            {
                gunController.OnTriggerHold();
            }

        }

        // Stop shooting
        if (Input.GetMouseButtonUp(0))
        {
            gunController.OnTriggerReleased();
        }


        //Reload the gun
        if (Input.GetKeyDown(KeyCode.R))
        {
            gunController.Reload();
        }

        // GOD mode, auto shoot, no need for holding mouse button
        if (Input.GetKeyDown(KeyCode.G))
        {
            StartCoroutine(AutoShoot());
        }

        if(transform.position.y < -5)
        {
            TakeDamage(health);
        }
    }

    IEnumerator AutoShoot() {
        while (true)
        {
            gunController.OnAutoShoot();
            yield return null;
        }
    }

    public override void Die()
    {
        AudioManager.instance.PlaySound("Player Death", transform.position);
        base.Die();
    }












    public void onMoveStart()
    {
        
    }

    public void onMoving(Vector2 vector2)
    {
        this.detailMove = new Vector3(vector2.x, 0, vector2.y);
        
    }

    public void onMoveEnd()
    {
        this.detailMove = Vector2.zero;
        
    }
}
