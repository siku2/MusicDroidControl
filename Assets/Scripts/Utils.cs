using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public static class Utils
{
	static Dictionary<string, string[]> alphabet_ruler = new Dictionary<string, string[]> { { "a", new string[] { "b", "c", "d", "e", "f", "g", "h", "i", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "v", "w", "x", "z" } }, { "b", new string[] { "a", "e", "i", "o", "u" } }, { "c", new string[] { "a", "e", "h", "i", "k", "o", "u" } }, { "d", new string[] { "a", "e", "i", "o", "u" } }, { "e", new string[] { "a", "b", "d", "f", "g", "h", "i", "k", "l", "m", "n", "p", "r", "s", "t", "u", "v", "x", "z" } }, { "f", new string[] { "a", "e", "i", "o", "u" } }, { "g", new string[] { "a", "e", "i", "o", "u" } }, { "h", new string[] { "a", "e", "i", "o", "u" } }, { "i", new string[] { "b", "d", "f", "g", "h", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "w", "z" } }, { "k", new string[] { "a", "e", "i", "l", "o", "u" } }, { "l", new string[] { "a", "e", "i", "o", "u" } }, { "m", new string[] { "a", "e", "i", "o", "u" } }, { "n", new string[] { "a", "e", "i", "o", "u" } }, { "o", new string[] { "b", "c", "d", "f", "g", "h", "i", "k", "l", "m", "n", "p", "r", "s", "t", "v", "w", "x", "z" } }, { "p", new string[] { "a", "e", "i", "l", "n", "o", "r", "s", "u" } }, { "r", new string[] { "a", "e", "i", "o", "u" } }, { "s", new string[] { "a", "c", "e", "h", "i", "l", "n", "o", "r", "t", "u", "w" } }, { "t", new string[] { "a", "e", "i", "o", "r", "u", "w" } }, { "u", new string[] { "b", "c", "d", "f", "g", "h", "i", "k", "l", "m", "n", "p", "r", "s", "t", "v", "w", "x", "z" } }, { "v", new string[] { "a", "e", "i", "o", "u" } }, { "w", new string[] { "a", "e", "i", "o", "u" } }, { "x", new string[] { "a", "e", "i", "o", "u" } }, { "z", new string[] { "a", "e", "i", "o", "u" } } };
	static System.Random rnd;


	public static string GenerateToken(int length)
	{
		rnd = new System.Random();

		string token = alphabet_ruler.ElementAt(rnd.Next(alphabet_ruler.Count)).Key;
		string returnString = "";
		for(int i = 0; i < length; i++)
		{
			returnString += token;
			token = alphabet_ruler[token][rnd.Next(alphabet_ruler[token].Length)];
		}

		return returnString;
	}
}
