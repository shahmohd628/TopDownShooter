using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : LivingEntity {

    public enum State { Idle, Chasing, Attacking };
    State currentState;

    public ParticleSystem deathEffect;
    public static event System.Action OnDeathStatic;

    NavMeshAgent pathfinder;
    Transform target;
    LivingEntity targetEntity;

    float attackDistanceThreshold = .5f;
    float timeBetweenAttacks = 2.2f;
    float damage = 1f;

    float nextAttackTime;
    float myCollisionRadius;
    float targetCollisionRadius;
    bool hasTarget;
    Animator animator;

    void Awake() {
        // Animator is now directly on this GameObject — no GetComponentInChildren needed
        animator = GetComponent<Animator>();
        pathfinder = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) {
            hasTarget = true;
            target = playerObj.transform;
            targetEntity = target.GetComponent<LivingEntity>();
            myCollisionRadius = GetComponent<CapsuleCollider>().radius;
            targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;
        }
    }

    protected override void Start() {
        base.Start();

        if (hasTarget) {
            currentState = State.Chasing;
            targetEntity.OnDeath += OnTargetDeath;
            StartCoroutine(UpdatePath());
        }
    }

    public void SetCharacteristics(float moveSpeed, int hitsToKillPlayer, float enemyHealth, Color skinColor) {
        pathfinder.speed = moveSpeed;
        if (hasTarget) {
            damage = Mathf.Ceil(targetEntity.startingHealth / hitsToKillPlayer);
        }
        startingHealth = enemyHealth;

        // Death particle is always red to look like blood — skinColor ignored
        if (deathEffect != null) {
            var main = deathEffect.main;
            main.startColor = Color.red;
        }
    }

    public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection) 
	{
		AudioManager.instance.PlaySound("Impact", transform.position);

		if (damage >= health && !dead) {
			animator.speed = 1f;
			animator.SetTrigger("Die");

			pathfinder.enabled = false;
			pathfinder.velocity = Vector3.zero;

			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb != null) {
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				rb.isKinematic = true;
			}

			if (OnDeathStatic != null) OnDeathStatic();
			AudioManager.instance.PlaySound("Enemy Death", transform.position);

			if (deathEffect != null) {
				Destroy(Instantiate(deathEffect.gameObject, hitPoint,
					Quaternion.FromToRotation(Vector3.forward, hitDirection)) as GameObject,
					deathEffect.main.startLifetime.constantMax);
			}
		} else {
			// Only play hit reaction if not already attacking or reacting
			if (currentState != State.Attacking) {
				StartCoroutine(HitReaction());
			}
		}

		base.TakeHit(damage, hitPoint, hitDirection);
	}

    void OnTargetDeath() {
        hasTarget = false;
        currentState = State.Idle;
        animator.SetFloat("Speed", 0f);
    }

    void Update() {
        if (hasTarget) {
            float speed = pathfinder.velocity.magnitude;
            animator.SetFloat("Speed", speed);

            // Only scale animation speed during chasing so walk feet match movement
            // Tune the 0.5f in Play mode until feet don't slide
            if (currentState == State.Chasing) {
                animator.speed = (speed > 0.1f) ? speed * 0.3f : 1f;
            }

            if (currentState != State.Attacking && Time.time > nextAttackTime) {
                float sqrDstToTarget = (target.position - transform.position).sqrMagnitude;
                float attackRange = attackDistanceThreshold + myCollisionRadius + targetCollisionRadius;
                if (sqrDstToTarget < Mathf.Pow(attackRange, 2)) {
                    nextAttackTime = Time.time + timeBetweenAttacks;
                    StartCoroutine(Attack());
                }
            }
        }
    }

    IEnumerator Attack() {
        currentState = State.Attacking;

        pathfinder.enabled = false;
        pathfinder.velocity = Vector3.zero;

        // Face the player squarely before attacking
        if (hasTarget) {
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        // Reset to normal speed so attack animation plays correctly
        animator.speed = 1f;

        // Brief pause so zombie fully stops before animation triggers
        yield return new WaitForSeconds(0.1f);

        // Trigger animation and sound on the same frame so they are in sync
        animator.SetTrigger("Attack");

        // Wait for animation midpoint (~1.1s) then check and apply damage
        yield return new WaitForSeconds(1f);
        if (hasTarget && !dead) {
        float sqrDst = (target.position - transform.position).sqrMagnitude;
        float range = attackDistanceThreshold + myCollisionRadius + targetCollisionRadius;
        if (sqrDst < range * range) {
            AudioManager.instance.PlaySound("Enemy Attack", transform.position); // moved inside
            targetEntity.TakeDamage(damage);
            }
        }

        if (hasTarget && !dead) {
            float sqrDst = (target.position - transform.position).sqrMagnitude;
            float range = attackDistanceThreshold + myCollisionRadius + targetCollisionRadius;
            if (sqrDst < range * range) {
                targetEntity.TakeDamage(damage);
            }
        }

        // Wait for second half of animation before resuming movement
        yield return new WaitForSeconds(1.0f);

        currentState = State.Chasing;
        pathfinder.enabled = true;
    }

    IEnumerator UpdatePath() {
        float refreshRate = .25f;

        while (hasTarget) {
            if (currentState == State.Chasing && !dead) {
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                Vector3 targetPosition = target.position - dirToTarget *
                    (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold / 2);
                pathfinder.SetDestination(targetPosition);
            }
            yield return new WaitForSeconds(refreshRate);
        }
    }
	IEnumerator HitReaction() 
	{
		currentState = State.Attacking; // borrow Attacking state to block movement

		// Stop zombie in place
		pathfinder.enabled = false;
		pathfinder.velocity = Vector3.zero;

		animator.speed = 1.5f;
		animator.SetTrigger("Hit");

		// Wait for hit animation duration (yours is 1:29 = 1.48s)
		yield return new WaitForSeconds(1f);

		// Resume chasing if still alive
		if (!dead) {
			currentState = State.Chasing;
			pathfinder.enabled = true;
		}
	}
}