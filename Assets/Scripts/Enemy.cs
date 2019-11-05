﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    //Bullet
    public float e_BulletSpeed;
    public float e_BulletDist;
    private bool e_HasShot;
    public float e_BulletDamage;
    private float e_ShotTimer = 0f;
    public float e_ShotCooldown = 0.25f;
    public GameObject bullet;
    public Transform e_GunHolder;

    //Patrolling
    private int currentPoint;
    public Transform[] patrolPoints;
    bool isMovingForward = true;

    //Enemy
    public float e_MoveSpeed;
    public float e_ChaseSpeed;
    public float e_MaxHealth = 100;
    public float e_CurHealth;
    public float e_HealthDeath = 0;
    public float e_ViewDis;
    private Image e_HealthBar;
    private GameObject player;
    private bool targetInViewRange;
    private bool targetInShootRange;


    // Use this for initialization
    void Start()
    {
        e_HealthBar = GameObject.Find("EnemyHealth").GetComponent<Image>();
        player = GameObject.Find("Player");
        Reset();
    }

    void Reset()
    {
        e_CurHealth = e_MaxHealth;
        e_HealthBar.fillAmount = 1f;
        currentPoint = 0;
    }
    void Update()
    {
        Patrol();
        Shoot();
    }

    public void DecreaseHealth(float bulletDamage)
    {
        if (e_CurHealth > e_HealthDeath)
        {
            e_CurHealth -= bulletDamage;
            e_HealthBar.fillAmount -= bulletDamage / 100;
        }
        
        if (e_CurHealth <= e_HealthDeath)
        {
            Destroy(gameObject);
        }
    }

    void Patrol()
    {
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            //Is the player further away then e_ViewDis?
            if (distance >= e_ViewDis)
            {
                targetInViewRange = false;
            }
            else
            {
                targetInViewRange = true;
            }

            if (!targetInViewRange)
            {
                transform.LookAt(patrolPoints[currentPoint].position);
                Vector3 destination = patrolPoints[currentPoint].position;

                transform.position = Vector3.MoveTowards(transform.position, destination, e_MoveSpeed * Time.deltaTime);

                // Compare how far we are to the destination.
                float distanceToDestination = Vector3.Distance(transform.position, destination);
                if (distanceToDestination < 0.2f) // 0.2 is tolerance value.
                {
                    // So, we have reached the destination.

                    // Set the next waypoint.

                    if (isMovingForward)
                        currentPoint++;
                    else // we are moving backward
                        currentPoint--;

                    if (currentPoint >= patrolPoints.Length)
                    {// We have reached the last waypoint, now go backward.
                        isMovingForward = false;
                        currentPoint = patrolPoints.Length - 2;
                    }

                    if (currentPoint < 0)
                    {// We have reached the first waypoint, now go forward.
                        isMovingForward = true;
                        currentPoint = 1;
                    }
                }
            }
        }
        
    }

    void Shoot()
    {
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        // If CAN'T shoot (waiting for shot cooldown)
        if (e_HasShot)
        {
            ShotCooldown();
        }

        // if CAN shoot
        if (!e_HasShot && targetInViewRange)
        {
            e_HasShot = true;

            //If in shooting range, shoot
            if (targetInShootRange)
            {
                Instantiate(bullet, e_GunHolder.position, Quaternion.identity);
            }
        }

        if (targetInViewRange)
        {
            //If not in shooting range, chase
            if (distance > e_BulletDist)
            {
                targetInShootRange = false;

                transform.position = Vector3.MoveTowards(transform.position, player.transform.position, e_ChaseSpeed * Time.deltaTime);
                transform.LookAt(player.transform.position);
            }

            //If in shooting range, stop chasing
            if (distance <= e_BulletDist)
            {
                targetInShootRange = true;
                transform.LookAt(player.transform.position);
            }
        }
    }

    void ShotCooldown()
    {
        if (e_ShotTimer <= e_ShotCooldown)
        {
            e_ShotTimer += Time.deltaTime;
        }

        if (e_ShotTimer >= e_ShotCooldown)
        {
            e_HasShot = false;
            e_ShotTimer = 0;
        }
    }
}
