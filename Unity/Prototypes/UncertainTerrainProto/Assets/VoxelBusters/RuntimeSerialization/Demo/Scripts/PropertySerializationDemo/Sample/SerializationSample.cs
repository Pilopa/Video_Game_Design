using UnityEngine;
using System.Collections;
using System;
using System.IO;
using VoxelBusters.RuntimeSerialization;

namespace VoxelBusters.RuntimeSerialization.Demo
{
	[Serializable, RuntimeSerializable]
	public class SerializationSample
	{
		public enum eSamepleEnum
		{
			VAL_1,
			VAL_2
		}
		
		#region Properties

		public	 		string			stringField			= "string value";
		public			int				intField			= 1;
		public			float			floatField			= 99.9f;
		public			DateTime		dateTimeField;

		#endregion

		#region Methods

		public void AssignRandomValue ()
		{
			stringField			= Path.GetRandomFileName().Replace(".", "");
			intField			= UnityEngine.Random.Range(0, 1000);
			floatField			= UnityEngine.Random.Range(0f, 100f);

			// Random date
			DateTime _startDate	= new DateTime(1947, 1, 1);
			int _range 			= (DateTime.Today - _startDate).Days;           
			dateTimeField		= _startDate.AddDays(UnityEngine.Random.Range(0, _range));
		}

		public override string ToString ()
		{
			return string.Format ("StringField={0}\n IntField={1}\n FloatField={2}\n DateTimeField={3}", 
			                      stringField, intField, floatField, dateTimeField);
		}

		#endregion
	}
}