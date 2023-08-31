using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferenceLibrary;

public class EnumerableMethods
{
	int a = 5;
	int b = 4;
	int[] c = new int[5];
	int[] d = new int[6];
	int[][] e = new int[7][];

	public IEnumerable<int> GetNothing()
	{
		yield break;
	}

	public IEnumerable<int> GetA()
	{
		yield return a;
	}

	public IEnumerable<int> GetAB()
	{
		yield return a;
		yield return b;
	}

	public IEnumerable<int> GetABA()
	{
		yield return a;
		yield return b;
		yield return a;
	}

	public IEnumerable<int> GetC()
	{
		for (int i = 0; i < c.Length; i++)
		{
			yield return c[i];
		}
	}

	public IEnumerable<int> GetACB()
	{
		yield return a;
		for (int i = 0; i < c.Length; i++)
		{
			yield return c[i];
		}
		yield return b;
	}

	public IEnumerable<int> GetCD()
	{
		for (int i = 0; i < c.Length; i++)
		{
			yield return c[i];
		}
		for (int j = 0; j < d.Length; j++)
		{
			yield return d[j];
		}
	}

	public IEnumerable<int> GetCE()
	{
		for (int i = 0; i < c.Length; i++)
		{
			yield return c[i];
		}
		for (int j = 0; j < e.Length; j++)
		{
			for (int k = 0; k < e[j].Length; k++)
			{
				yield return e[j][k];
			}
		}
	}

	public IEnumerable<int> GetAinC()
	{
		for (int i = 0; i < c.Length; i++)
		{
			yield return a;
			yield return c[i];
		}
	}
}
