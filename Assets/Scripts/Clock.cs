using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Clock : MonoBehaviour
{
	[SerializeField] Transform hoursPointer;
	[SerializeField] Transform minutesPointer;


	void Update()
	{
		TimeSpan now = DateTime.Now.TimeOfDay;
		float hours = (float) now.TotalHours;
		float minutes = (float) now.TotalMinutes;

		hoursPointer.rotation = Quaternion.AngleAxis(hours * 30, Vector3.back);
		minutesPointer.rotation = Quaternion.AngleAxis(minutes * 6, Vector3.back);
	}
}
