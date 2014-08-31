using UnityEngine;
using System.Collections;

public static class Helpers
{
	public static bool FireButtonDown ()
	{
		// Fire1 is A key on XBox Controller
		return (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space));
	}

	public static bool HyperspaceButtonDown ()
	{
		// Fire2 is B key on XBox Controller
		return (Input.GetButtonDown("Fire2") || Input.GetKeyDown(KeyCode.H));
	}

}

