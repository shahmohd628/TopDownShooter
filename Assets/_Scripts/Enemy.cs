using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent (typeof (NavMeshAgent))]
public class Enemy : LivingEntity {

	public enum State {Idle, Chasing, Attacking};
	State currentState;

	public ParticleSystem deathEffect;
	public static event System.Action OnDeathStatic;

	NavMeshAgent pathfinder;
	Transform target;
	LivingEntity targetEntity;
	Material skinMaterial;

	Color originalColour;

	float attackDistanceThreshold = .5f;
	float timeBetweenAttacks = 2.2f; // matches 2:18 attack animation length
	float damage = 1f;

	float nextAttackTime;
	float myCollisionRadius;
	float targetCollisionRadius;
	bool hasTarget;
	Animator animator;

	void Awake() {
		animator = GetComponentInChildren<Animator>();
		pathfinder = GetComponent<NavMeshAgent>();

		if (GameObject.FindGameObjectWithTag("Player").transform != null) {
			hasTarget = true;

			target = GameObject.FindGameObjectWithTag("Player").transform;
			targetEntity = target.GetComponent<LivingEntity>();

			myCollisionRadius = GetComponent<CapsuleCollider>().radius;
			targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;
		}
	}

	protected override void Start () {
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
		deathEffect.startColor = new Color(skinColor.r, skinColor.g, skinColor.b, 1);
		skinMaterial = GetComponent<Renderer>().material;
		skinMaterial.color = skinColor;
		originalColour = skinMaterial.color;
	}

	public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection) {
		AudioManager.instance.PlaySound("Impact", transform.position);
		if (damage >= health && !dead) {
			// Reset animator speed so death plays at normal speed
			animator.speed = 1f;
			animator.SetTrigger("Die");

			// Stop all movement immediately
			pathfinder.enabled = false;
			pathfinder.velocity = Vector3.zero;

			// Kill Rigidbody momentum so corpse doesn't slide
			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb != null) {
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				rb.isKinematic = true;
			}

			if (OnDeathStatic != null) {
				OnDeathStatic();
			}
			AudioManager.instance.PlaySound("Enemy Death", transform.position);
			Destroy(Instantiate(deathEffect.gameObject, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection)) as GameObject, deathEffect.startLifetime);
		} else {
			// Reset speed before hit reaction so it plays correctly
			animator.speed = 1f;
			animator.SetTrigger("Hit");
		}
		base.TakeHit(damage, hitPoint, hitDirection);
	}

	void OnTargetDeath() {
		hasTarget = false;
		currentState = State.Idle;
		animator.SetFloat("Speed", 0f);
	}

	void Update() 
	{
		if (hasTarget) {
			float speed = pathfinder.velocity.magnitude;
			animator.SetFloat("Speed", speed);

			// Only scale animator speed during chase, not attack
			if (currentState == State.Chasing) {
				animator.speed = (speed > 0.1f) ? speed * 0.5f : 1f;
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

	IEnumerator Attack() 
	{
		currentState = State.Attacking;

		// Fully stop movement before playing animation
		pathfinder.enabled = false;
		pathfinder.velocity = Vector3.zero;

		// Face the player before attacking
		if (hasTarget) {
			Vector3 dir = (target.position - transform.position).normalized;
			dir.y = 0;
			transform.rotation = Quaternion.LookRotation(dir);
		}

		// Reset speed so attack animation plays at correct speed
		animator.speed = 1f;

		// Small delay so zombie fully stops before animation triggers
		yield return new WaitForSeconds(0.1f);

		// NOW trigger the animation and sound together
		animator.SetTrigger("Attack");
		AudioManager.instance.PlaySound("Enemy Attack", transform.position);

		// Wait for animation midpoint then apply damage
		yield return new WaitForSeconds(1.1f);

		if (hasTarget && !dead) {
			float sqrDst = (target.position - transform.position).sqrMagnitude;
			float range = attackDistanceThreshold + myCollisionRadius + targetCollisionRadius;
			if (sqrDst < range * range) {
				targetEntity.TakeDamage(damage);
			}
		}

		// Wait for rest of animation
		yield return new WaitForSeconds(1.0f);

		currentState = State.Chasing;
		pathfinder.enabled = true;
	}

	IEnumerator UpdatePath() {
		float refreshRate = .25f;

		while (hasTarget) {
			if (currentState == State.Chasing) {
				Vector3 dirToTarget = (target.position - transform.position).normalized;
				Vector3 targetPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold / 2);
				if (!dead) {
					pathfinder.SetDestination(targetPosition);
				}
			}
			yield return new WaitForSeconds(refreshRate);
		}
	}
}
