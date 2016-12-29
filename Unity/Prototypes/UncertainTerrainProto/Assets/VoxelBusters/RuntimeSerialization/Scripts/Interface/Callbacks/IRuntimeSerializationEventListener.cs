using UnityEngine;
using System.Collections;

namespace VoxelBusters.RuntimeSerialization
{
	/// <summary>
	/// Implement this interface to observe serialization process of <see cref="RuntimeSerializableAttribute"/> object.
	/// </summary>
	public interface IRuntimeSerializationEventListener
	{
		/// <summary>
		/// Event triggered after <see cref="RuntimeSerializableAttribute"/> object is serialized.
		/// </summary>
		/// <param name="_serializationID">Identifier associated with <see cref="RuntimeSerializableAttribute"/> object.</param>
		void OnAfterRuntimeSerialize (string _serializationID);

		/// <summary>
		/// Event triggered after <see cref="RuntimeSerializableAttribute"/> object is deserialized.
		/// </summary>
		/// <param name="_serializationID">Identifier associated with <see cref="RuntimeSerializableAttribute"/> object.</param>
		/// <param name="_deserializedObject">Deserialized <see cref="RuntimeSerializableAttribute"/> object.</param>
		void OnAfterRuntimeDeserialize (string _serializationID, object _deserializedObject);
	}
}