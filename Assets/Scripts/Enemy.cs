﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Pathfinding;
using EZCameraShake;

public class Enemy : MonoBehaviour
{
    public GameObject obj;
    //AI
    private Rigidbody2D rb;
    [HideInInspector]
    public Seeker seeker;
    public bool e_CanSeeTarget;
    public GameObject nearestEnemy;
    public bool tooCloseEnemy;
    public float dist2;
    public float nexWaypointDistance = 3f;
    public float navUpdateTimer = 0.25f;
    [HideInInspector]
    public Path path;
    [HideInInspector]
    public int currentWaypoint = 0;

    [HideInInspector]
    public bool reachedEndOfPath = false;


    //Element bg
    public bool gettingBigger = true;

    //Bullet
    private bool e_HasShot;
    private float e_ShotTimer = 0f;
    public GameObject bullet;
    public Transform e_GunHolder;

    //Evading
    private float random;

    public Image e_HealthBar;
    public Image e_HealthBarBG;

    public float e_CurHealth;
    private GameObject player;
    private Player playerScript;
    private bool targetInViewRange;
    private bool targetInShootRange;
    private bool alreadyChosen;
    private float timer;

    private SpriteRenderer sr;

    //Room
    public GameObject room;

    //Boss
    public bool isBoss;
    public bool bossDialogueReady;
    private bool changedTag;
    public GameObject memoryRange;

    //Animation
    public Animator animator;
    public Animator elementBGAnimator;
    public GameObject BGElement;

    private Gamemode gm;
    private ScreenShake ss;

    //element bg
    private float x = .1f;
    private float y = .1f;

    public bool isDead;
    private float x1;
    private float y1;

    //Boss health bar
    private Image bossHealthBar;
    private Image bossHealthBarDep;
    private float t;

    public GameObject EGOBossHealth;
    private bool bossLostHealth;
    private float tempHealth;
    private RoomManager rm;
    private float time;
    public bool enraged;

    void Start()
    {
        gm = FindObjectOfType<Gamemode>();
        rb = GetComponent<Rigidbody2D>();
        ss = FindObjectOfType<ScreenShake>();
        rm = FindObjectOfType<RoomManager>();

        //Increase enemy count
        gm.enemyCount++;

        //Change name of enemy, including the enemy count
        name = "Enemy " + gm.enemyCount;

        sr = GetComponentInChildren<SpriteRenderer>();
        player = GameObject.Find("Player");
        playerScript = player.GetComponent<Player>();


        seeker = GetComponent<Seeker>();

        InvokeRepeating("UpdatePath", 0f, navUpdateTimer);

        // This must be placed after all the values are set
        // If the enemy is a boss, ensure their statistics are not boss amounts
        if (!GetComponent<Enemy>().isBoss)
        {
            //gm.bossBulletScaleIncCur = 1;
            gm.bossBulletIncScaleRateCur = 1;
            gm.bossBulletDamageCur = 1;
            gm.bossBulletSpeedCur = 1;
            gm.bossBulletDistCur = 1;
            gm.bossScaleCur = 1;
            gm.bossSpeedCur = 1;
            gm.bossMaxHealthCur = 1;
            gm.bossShotCooldownCur = 1;
            gm.bossBulletSizeInfCur = 1;
            gm.BossAimBot = 1;
        }

        else
        {
            gm.bossBulletIncScaleRateCur = gm.bossBulletIncScaleRateDef;
            gm.bossBulletDamageCur = gm.bossBulletDamageDef;
            gm.bossBulletSpeedCur = gm.bossbulletSpeedDef;
            gm.bossBulletDistCur = gm.bossBulletDistDef;
            gm.bossScaleCur = gm.bossScaleDef;
            gm.bossSpeedCur = gm.bossSpeedDef;
            gm.bossMaxHealthCur = gm.bossMaxHealthDef;
            gm.bossShotCooldownCur = gm.bossShotCooldownDef;
            gm.bossBulletSizeInfCur = gm.bossBulletSizeInfDef;
        }
        gm.bossEnragedBulletSizeInfCur = 1;

        // Set health of enemy
        gm.e_MaxHealth *= gm.bossMaxHealthCur;

        gm.GetSwitchTimer();

        Reset();
        SetSize();
    }
    void Update()
    {
        LookAt();
        StartCoroutine("Shoot");
        ElementManager();
        AllowBossDialogue();
        UpdateBossHealthBarDep();
        Enraged();

        Vector3 z;
        z = transform.position;
        z.z = 0f;
    }

    IEnumerator UpdateHealthBar()
    {
        if (isBoss && rm.bossHealth.activeSelf)
        {
            bossHealthBar = GameObject.Find("Boss Health").GetComponent<Image>();
            bossHealthBarDep = GameObject.Find("Boss Health Dep").GetComponent<Image>();

            tempHealth = bossHealthBar.fillAmount;

            bossHealthBar.fillAmount = (e_CurHealth / gm.e_MaxHealth);
        }

        yield return new WaitForSeconds(0);

        bossLostHealth = true;
    }

    void UpdateBossHealthBarDep()
    {
        if (!bossLostHealth && rm.bossHealth.activeSelf)
        {
            t = 0;
        }
        if (bossLostHealth && rm.bossHealth.activeSelf)
        {
            t += gm.depSpeedCur * Time.deltaTime;
            bossHealthBarDep.fillAmount = Mathf.Lerp(tempHealth, (e_CurHealth / gm.e_MaxHealth), t);

            if (t >= 1)
            {
                bossLostHealth = false;
                t = 0;
            }
        }
    }

    void SetSize()
    {
        x1 = (1 * gm.bossScaleCur);
        y1 = (1 * gm.bossScaleCur);

        transform.localScale = new Vector2(x1, y1);
    }

    void Enraged()
    {      
        if (!isBoss)
        {
            return;
        }
        // Increase stats at 50% health
        if (e_CurHealth <= gm.e_MaxHealth / 2 && !enraged)
        {
            enraged = true;
            gm.switchTimeMin /= 2;
            gm.switchTimeMax /= 2;
            gm.bossEnragedBulletSizeInfCur *= 1.75f;
            gm.bossBulletIncScaleRateCur *= 2;
            gm.bossBulletSpeedCur *= 1.1f;
            gm.bossShotCooldownCur *= 2f;
            gm.BossAimBot = 2;

            // Increase size at 50% health
            transform.localScale = new Vector2(x1 * gm.bossEnragedSizeCur, y1 * gm.bossEnragedSizeCur);

            // Play Audio
            FindObjectOfType<AudioManager>().Play("BossEnrage");
        }
    }

    void UpdatePath()
    {
        if (seeker.IsDone())
        {
            seeker.StartPath(rb.position, player.transform.position, OnPathComplete);
        }
    }

    void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    void Reset()
    {
        // Set evade speed to default value
        gm.e_EvadeSpeed = gm.e_EvadeSpeedDef;

        e_CurHealth = gm.e_MaxHealth;
    }
    private void FixedUpdate()
    {
        Move();
        Evade();
    }

    void Move()
    {
        if (path == null)
        {
            return;
        }

        if (currentWaypoint >= path.vectorPath.Count)
        {
            reachedEndOfPath = true;
            return;
        }
        else
        {
            reachedEndOfPath = false;
        }

        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;

        Vector2 force = direction * (gm.e_MoveSpeed * gm.bossSpeedCur) * Time.deltaTime;

        // Move towards player
        if (!targetInShootRange || !e_CanSeeTarget)
        {
            rb.AddForce(force);
        }

        float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

        if (distance < nexWaypointDistance)
        {
            currentWaypoint++;
        }
    }

    void AllowBossDialogue()
    {
        //Check if this object is a boss, and if it's tag hasn't already been changed
        if (bossDialogueReady)
        {
            memoryRange.SetActive(true);
        }
    }

    void ElementManager()
    {
        if (gm.e_CurElement == 1)
        {
            gm.e_IsEarth = false;
            gm.e_IsWater = false;
            gm.e_IsFire = true;
            elementBGAnimator.SetInteger("curElement", gm.e_CurElement);
        }

        if (gm.e_CurElement == 0)
        {
            gm.e_IsEarth = false;
            gm.e_IsFire = false;
            gm.e_IsWater = true;
            elementBGAnimator.SetInteger("curElement", gm.e_CurElement);
        }

        if (gm.e_CurElement == 2)
        {
            gm.e_IsFire = false;
            gm.e_IsWater = false;
            gm.e_IsEarth = true;
            elementBGAnimator.SetInteger("curElement", gm.e_CurElement);
        }

        // Ensure the curent element stays between the only possible element numbers (0, 1, 2)
        if (gm.e_CurElement >= 3)
        {
            gm.e_CurElement = 0;
        }

        if (gm.e_CurElement <= -1)
        {
            gm.e_CurElement = 2;
        }

        BGElement.transform.localScale = new Vector2(x, y);

        if (gettingBigger)
        {
            // Increase the scale of the background
            x += .1f * Time.deltaTime * gm.incScaleRate;
            y += .1f * Time.deltaTime * gm.incScaleRate;

            // If projectile size has reached it's max scale, stop increasing size.
            if (x >= gm.maxScaleX || y >= gm.maxScaleY)
            {
                gettingBigger = false;
            }
        }

        if (!gettingBigger)
        {
            // Decrease the scale of the background
            x -= .1f * Time.deltaTime * gm.incScaleRate;
            y -= .1f * Time.deltaTime * gm.incScaleRate;

            // If projectile size has reached it's lowest scale, stop increasing size.
            if (x <= gm.minScaleX || y <= gm.minScaleY)
            {
                gettingBigger = true;
            }
        }

    }

    public void DecreaseHealth(float bulletDamage, string playersCurElement)
    {
        // Decrease enemy health if it wont die from damage
        if (e_CurHealth > gm.e_HealthDeath)
        {
            //Screen shake
            ScreenShakeInfo Info = new ScreenShakeInfo();
            Info.shakeMag = gm.shakeMagHit;
            Info.shakeRou = gm.shakeRouHit;
            Info.shakeFadeIDur = gm.shakeFadeIDurHit;
            Info.shakeFadeODur = gm.shakeFadeODurHit;
            Info.shakePosInfluence = gm.shakePosInfluenceHit;
            Info.shakeRotInfluence = gm.shakeRotInfluenceHit;

            ss.StartShaking(Info, .1f, 2);

            //Freeze the game for a split second
            gm.Freeze();

            // If player countered the enemy with their hit, take bonus damage
            if (playersCurElement == "Fire" && gm.e_IsEarth)
            {
                e_CurHealth -= bulletDamage + gm.fireDamage;

                // Play crit projectile audio
                FindObjectOfType<AudioManager>().Play("ProjectileHitCrit");

                // Play audio
                FindObjectOfType<AudioManager>().Play("ProjectileHit");
            }

            // If player countered the enemy with their hit, take bonus damage
            if (playersCurElement == "Water" && gm.e_IsFire)
            {
                e_CurHealth -= bulletDamage + gm.waterDamage;

                // Play crit projectile audio
                FindObjectOfType<AudioManager>().Play("ProjectileHitCrit");

                // Play audio
                FindObjectOfType<AudioManager>().Play("ProjectileHit");
            }

            // If player countered the enemy with their hit, take bonus damage
            if (playersCurElement == "Earth" && gm.e_IsWater)
            {
                e_CurHealth -= bulletDamage + gm.earthDamage;

                // Play crit projectile audio
                FindObjectOfType<AudioManager>().Play("ProjectileHitCrit");

                // Play audio
                FindObjectOfType<AudioManager>().Play("ProjectileHit");
            }

            // If there is no element counter, do regular damage
            if (playersCurElement == "Fire" && gm.e_IsWater)
            {
                e_CurHealth -= bulletDamage;
            }

            // If there is no element counter, do regular damage
            if (playersCurElement == "Fire" && gm.e_IsFire)
            {
                e_CurHealth -= bulletDamage;
            }

            // If there is no element counter, do regular damage
            if (playersCurElement == "Water" && gm.e_IsEarth)
            {
                e_CurHealth -= bulletDamage;
            }

            // If there is no element counter, do regular damage
            if (playersCurElement == "Water" && gm.e_IsWater)
            {
                e_CurHealth -= bulletDamage;
            }

            // If there is no element counter, do regular damage
            if (playersCurElement == "Earth" && gm.e_IsFire)
            {
                e_CurHealth -= bulletDamage;
            }

            // If there is no element counter, do regular damage
            if (playersCurElement == "Earth" && gm.e_IsEarth)
            {
                e_CurHealth -= bulletDamage;
            }

            // Play audio
            FindObjectOfType<AudioManager>().Play("ProjectileHit");
        }

        // If this object dies, and is not a boss
        if (e_CurHealth <= gm.e_HealthDeath && !isBoss)
        {
            //Play Death Audio
            FindObjectOfType<AudioManager>().Play("EnemyDeath");

            //Decrease enemy count
            gm.enemyCount--;

            //Decrease room enemy count
            room.GetComponent<Room>().roomEnemyCount--;

            //Kill enemy
            Destroy(gameObject);
        }

        // if the boss dies
        if (e_CurHealth <= gm.e_HealthDeath && isBoss && !isDead)
        {
            //Play Death Audio
            FindObjectOfType<AudioManager>().Play("BossDeath");

            //Trigger dialogue system
            bossDialogueReady = true;

            isDead = true;

            // set the animation to idle
            animator.SetInteger("EnemyBrain", 0);

            // set the rb of the enemy to kinematic. prevents it 
            // being able to be pushed around by player
            rb.bodyType = RigidbodyType2D.Static;

            // turn off the element background
            elementBGAnimator.enabled = false;
            BGElement.GetComponent<SpriteRenderer>().sprite = null;

            rm.bossHealth.SetActive(false);

        }

        StartCoroutine("UpdateHealthBar");
    }

    void LookAt()
    {
        if (player != null && !bossDialogueReady)
        {
            // Set rotation of gun holder to aim at player position
            // Rotate gun holder
            gm.e_ShootDir = player.transform.position - transform.position;
            gm.e_ShootDir.Normalize();
            e_GunHolder.transform.right = gm.shootDir;
        }

        if (player.transform.position.x > transform.position.x)
        {
            GetComponentInChildren<SpriteRenderer>().flipX = false;
        }

        else
        {
            GetComponentInChildren<SpriteRenderer>().flipX = true;
        }

        RaycastHit2D hitinfo = Physics2D.Linecast(transform.position, player.transform.position, (1 << 11) | (1 << 14));

        // See if can see target
        if (hitinfo.transform.tag != "Player")
        {
            //Debug.DrawLine(transform.position, gm.player.transform.position, Color.red, .1f);
            //Debug.Log("NOT hitting player, hitting: " + hitinfo.transform.tag);
            e_CanSeeTarget = false;
        }

        else
        {
            //Debug.DrawLine(transform.position, gm.player.transform.position, Color.green, .1f);
            //Debug.Log("Hitting player, hitting: " + hitinfo.transform.tag);
            e_CanSeeTarget = true;
        }
    }

    IEnumerator Shoot()
    {
        if (player == null)
        {
            yield return 0;
        }

        float distance = Vector2.Distance(transform.position, player.transform.position);

        //Is the player further away then e_ViewDis?
        if (distance >= gm.e_ViewDis)
        {
            targetInViewRange = false;

            // Play idle animation
            animator.SetInteger("EnemyBrain", 0);
        }
        else
        {
            targetInViewRange = true;
        }

        // If CAN'T shoot (waiting for shot cooldown)
        if (e_HasShot)
        {
            ShotCooldown();
        }

        // If the enemy can shoot, is alive, check if enemy has had their tag changed
        // to memory (for boss), if so, don't allow it to shoot
        if (!e_HasShot && targetInViewRange && !bossDialogueReady && !isDead)
        {
            e_HasShot = true;

            // If in shooting range and can see target, shoot
            if (targetInShootRange && e_CanSeeTarget)
            {
                // If is boss, but not enraged, play correct projectile throw sfx
                if (isBoss && !enraged)
                {
                    // Play audio
                    FindObjectOfType<AudioManager>().Play("BossProjectileThrow1");
                    
                }

                // If is boss, and enraged, play correct projectile throw sfx
                else if (isBoss && enraged)
                {
                    // Play audio
                    FindObjectOfType<AudioManager>().Play("BossProjectileThrow2");
                }

                // If NOT boss
                else if (!isBoss)
                {
                    // Play audio
                    FindObjectOfType<AudioManager>().Play("ProjectileThrow");
                }

                // Instantiate(obj, gm.player.transform.position, Quaternion.identity);
                GameObject go = Instantiate(bullet, e_GunHolder.position, Quaternion.identity);
                go.GetComponent<E_Bullet>().enemy = gameObject;

                // Play shooting animation
                animator.SetInteger("EnemyBrain", 2);

                // Wait x seconds
                yield return new WaitForSeconds(0.25f);

                // Play idle animation
                animator.SetInteger("EnemyBrain", 0);
            }
        }
    }
    void Evade()
    {
        if (isDead)
        {
            return;
        }

        if (timer <= gm.evadeTimerCur)
        {
            alreadyChosen = true;
            timer += Time.deltaTime;
        }

        if (timer >= gm.evadeTimerCur)
        {
            timer = 0;
            alreadyChosen = false;
        }

        if (!alreadyChosen)
        {
            alreadyChosen = true;
            random = Random.Range(1, 7);
        }

        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);

            // If in shooting range, stop chasing and begin evading
            // and if it can see target
            if (distance <= (gm.e_BulletDist * gm.bossBulletDistCur) - gm.e_rangeOffset && e_CanSeeTarget || tooCloseEnemy)
            {
                if (!tooCloseEnemy)
                {
                    gm.evadeTimerCur = gm.evadeTimerDef;
                }

                targetInShootRange = true;

                // Play run animation
                animator.SetInteger("EnemyBrain", 1);

                // Evade Left
                if (random == 1 || random == 6 && !tooCloseEnemy)
                {
                    rb.AddRelativeForce(e_GunHolder.transform.up * (gm.e_EvadeSpeed * gm.bossSpeedCur) * Time.deltaTime);
                }

                // go left, the opposite of what the close enemy is now doing
                if (random == 1 || random == 6 && tooCloseEnemy)
                {
                    gm.evadeTimerCur = gm.enemyOverlapEvadeTimer;

                    // Send close enemy right
                    if (nearestEnemy)
                    {
                        nearestEnemy.GetComponent<Enemy>().random = 2;
                        rb.AddRelativeForce(e_GunHolder.transform.up * gm.e_EvadeSpeed * Time.deltaTime);
                    }
                }

                // Evade Right
                if (random == 2 || random == 5 && !tooCloseEnemy)
                {
                    rb.AddRelativeForce(-e_GunHolder.transform.up * (gm.e_EvadeSpeed * gm.bossSpeedCur) * Time.deltaTime);
                }

                // go right, the opposite of what the close enemy is now doing
                if (random == 2 || random == 5 && tooCloseEnemy)
                {
                    gm.evadeTimerCur = gm.enemyOverlapEvadeTimer;

                    if (nearestEnemy)
                    {
                        // Send close enemy left
                        nearestEnemy.GetComponent<Enemy>().random = 1;
                        rb.AddRelativeForce(-e_GunHolder.transform.up * gm.e_EvadeSpeed * Time.deltaTime);
                    }
                }

                // Evade forwards
                if (random == 3 && !tooCloseEnemy)
                {
                    rb.AddRelativeForce(-e_GunHolder.transform.right * (gm.e_EvadeSpeed * gm.bossSpeedCur) * Time.deltaTime);
                }

                // go forwards, the opposite of what the close enemy is now doing
                if (random == 3 && tooCloseEnemy)
                {
                    gm.evadeTimerCur = gm.enemyOverlapEvadeTimer;

                    if (nearestEnemy)
                    {
                        // Send close enemy backwards
                        nearestEnemy.GetComponent<Enemy>().random = 4;
                        rb.AddRelativeForce(-e_GunHolder.transform.right * gm.e_EvadeSpeed * Time.deltaTime);
                    }
                }

                // Evade Backwards
                if (random == 4 && !tooCloseEnemy)
                {
                    rb.AddRelativeForce(e_GunHolder.transform.right * (gm.e_EvadeSpeed * gm.bossSpeedCur) * Time.deltaTime);
                }

                // go backwards, the opposite of what the close enemy is now doing
                if (random == 4 && tooCloseEnemy)
                {
                    gm.evadeTimerCur = gm.enemyOverlapEvadeTimer;

                    if (nearestEnemy)
                    {
                        // Send close enemy forwards
                        nearestEnemy.GetComponent<Enemy>().random = 3;
                        rb.AddRelativeForce(e_GunHolder.transform.right * gm.e_EvadeSpeed * Time.deltaTime);
                    }
                }
            }
            else
            {
                targetInShootRange = false;
            }
        }
    }

    void ShotCooldown()
    {
        if (e_ShotTimer <= (gm.e_ShotCooldown * gm.bossShotCooldownCur))
        {
            e_ShotTimer += Time.deltaTime;
        }

        if (e_ShotTimer >= (gm.e_ShotCooldown * gm.bossShotCooldownCur))
        {
            e_HasShot = false;
            e_ShotTimer = 0;
        }
    }

}
//void Move()
//{
//    if enemy is not in shoot range and enemy cannot see player
//    if (!targetInShootRange || !e_CanSeeTarget)
//    {
//        float minDist = Mathf.Infinity;

//        Check if there is an enemy very close to this enemy
//        foreach (Enemy e in FindObjectsOfType<Enemy>())
//        {
//            Don't include this enemy in the foreach
//            if (e != this.gameObject.GetComponent<Enemy>())
//            {
//                float dist = Vector2.Distance(e.transform.position, transform.position);

//                if (minDist > dist)
//                {
//                    nearestEnemy = e.transform.gameObject;
//                    minDist = dist;
//                }
//            }
//        }

//        If there is a close enemy, check...
//        if (nearestEnemy)
//        {
//            float dist2 = Vector2.Distance(nearestEnemy.transform.position, transform.position);

//            If the other enemy is too close
//            if (dist2 < gm.enemyTooCloseDis)
//            {
//                tooCloseEnemy = true;
//            }
//            else
//            {
//                tooCloseEnemy = false;
//            }

//            If there is an enemy, but it's not too close
//            if (!tooCloseEnemy)
//            {
//                Set collider to normal size, so enemies cannot walk ontop of eachother
//                GetComponent<CircleCollider2D>().radius = 7;
//                gm.e_EvadeSpeed = gm.e_EvadeSpeedDef;

//                if (path == null)
//                {
//                    return;
//                }

//                if (currentWaypoint >= path.vectorPath.Count)
//                {
//                    reachedEndOfPath = true;
//                    return;
//                }
//                else
//                {
//                    reachedEndOfPath = false;
//                }

//                Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;
//                Vector2 force = direction * gm.e_MoveSpeed * Time.deltaTime;

//                Move the enemy towards player
//                rb.AddForce(force);

//                Play run animation
//                animator.SetInteger("EnemyBrain", 1);

//                float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

//                if (distance < nexWaypointDistance)
//                {
//                    currentWaypoint++;
//                }
//            }

//            if (tooCloseEnemy)
//            {
//                Set colldier radius to very small, so enemies CAN walk ontop of eachother
//                GetComponent<CircleCollider2D>().radius = 7;
//                gm.e_EvadeSpeed = gm.enemyOverlapSpeed / 2;
//            }
//        }

//        if there is NO close enemy(only one enemy)
//        else
//        {
//            if (path == null)
//            {
//                return;
//            }

//            if (currentWaypoint >= path.vectorPath.Count)
//            {
//                reachedEndOfPath = true;
//                return;
//            }
//            else
//            {
//                reachedEndOfPath = false;
//            }

//            Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;
//            Vector2 force = direction * gm.e_MoveSpeed * Time.deltaTime;

//            Move the enemy towards player
//            rb.AddForce(force);

//            Play run animation
//            animator.SetInteger("EnemyBrain", 1);

//            float distance2 = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

//            if (distance2 < nexWaypointDistance)
//            {
//                currentWaypoint++;
//            }
//        }
//    }
//}