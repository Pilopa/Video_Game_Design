using UnityEngine;
using System.Collections;

namespace VoxelBusters.RuntimeSerialization.Internal
{
	internal class Constants : MonoBehaviour 
	{
		#region Constants

		// Product info
		internal 	const		string				kProductName							= "Runtime Serialization for Unity Lite Version";
		internal 	const		string				kProductVersion							= "1.0";
		internal	const		int					kSerializationFormatVersion				= 1;
		internal	const		int					kSerializationSupportedFormatVersions	= kSerializationFormatVersion;

		#endregion
	}
}