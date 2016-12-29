using UnityEngine;
using System.Collections;

namespace VoxelBusters.RuntimeSerialization.Internal
{
	internal enum eTypeTag : byte
	{
		UNSUPPORTED,
		NULL,
		PRIMITIVE,
		STRING,
		STRUCT,
		CLASS,
		OBJECT_REFERENCE
	}

	internal enum BinaryElement : byte
	{
		VERSION,
		ASSEMBLY,
		TYPE,
		OBJECT_DATA,
		VALUE
	}
}