﻿using UnityEngine;
using System.Collections;

[RequireComponent (typeof (PlayerController))]
[RequireComponent (typeof (GunController))]
public class Player : LivingEntity {

	public float moveSpeed = 5;

	public Crosshairs crosshairs;

	Camera viewCamera;
	PlayerController controller;
	GunController gunController;
	public Animator animator;
	
	protected override void Start () {
		base.Start ();
		animator = GetComponentInChildren<Animator>();
		controller = GetComponent<PlayerController> ();
		gunController = GetComponent<GunController> ();
		viewCamera = Camera.main;
	}

	void Update () {
		// Movement input
		Vector3 moveInput = new Vector3 (Input.GetAxisRaw ("Horizontal"), 0, Input.GetAxisRaw ("Vertical"));
		Vector3 moveVelocity = moveInput.normalized * moveSpeed;
		controller.Move (moveVelocity);

		Vector3 localMove = transform.InverseTransformDirection(moveVelocity);
		animator.SetFloat("Horizontal", localMove.x / moveSpeed);
		animator.SetFloat("Vertical", localMove.z / moveSpeed);

		// Block firing animation while reloading
		bool isReloading = gunController.IsReloading;
		animator.SetBool("isFiring", Input.GetMouseButton(0) && !isReloading);

		// Look input
		Ray ray = viewCamera.ScreenPointToRay (Input.mousePosition);
		Plane groundPlane = new Plane (Vector3.up, Vector3.up * gunController.GunHeight);
		float rayDistance;

		if (groundPlane.Raycast(ray, out rayDistance)) {
			Vector3 point = ray.GetPoint(rayDistance);
			controller.LookAt(point);
			crosshairs.transform.position = point;
			crosshairs.DetectTargets(ray);
			if ((new Vector2(point.x, point.z) - new Vector2(transform.position.x, transform.position.z)).sqrMagnitude > 1) {
				gunController.Aim(point);
			}
		}

		// Weapon input
		if (Input.GetMouseButton(0)) {
			gunController.OnTriggerHold();
		}
		if (Input.GetMouseButtonUp(0)) {
			gunController.OnTriggerRelease();
		}
		if (Input.GetKeyDown (KeyCode.R)) {
			animator.SetTrigger("Reload");
			gunController.Reload();
		}
		if (transform.position.y < -10) {
			TakeDamage(health);
		}
	}

	public override void Die() {
		animator.SetTrigger("Die");
		AudioManager.instance.PlaySound("Player Death", transform.position);
		base.Die();
	}
}
