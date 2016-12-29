using UnityEngine;
using System.Collections;
using System.Reflection;

namespace VoxelBusters.RuntimeSerialization.Internal
{
	internal struct Field
	{
		#region Properties
		
		public 			FieldInfo		Info
		{
			get;
			private set;
		}
		
		public			bool			IsObjectInitializer
		{
			get;
			private set;
		}
		
		#endregion
		
		#region Constructor
		
		internal Field (FieldInfo _fieldInfo, bool _isObjectInitializer) : this ()
		{
			Info				= _fieldInfo;
			IsObjectInitializer	= _isObjectInitializer;
		}
		
		#endregion
	}
}
