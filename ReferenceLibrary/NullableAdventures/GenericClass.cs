using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace ReferenceLibrary.NullableAdventures
{
	public class GenericClass<T1,T2,T3,T4,T5,T6>
		where T2 : struct
		where T3 : class, new()
		where T4 : class?
		where T5 : class?
		where T6 : class?
	{
	}
}
