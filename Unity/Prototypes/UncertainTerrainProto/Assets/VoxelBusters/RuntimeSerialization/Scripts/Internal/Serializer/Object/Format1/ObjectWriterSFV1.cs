using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using VoxelBusters.Utility;

namespace VoxelBusters.RuntimeSerialization.Internal
{
	internal class ObjectWriterSFV1 : ObjectWriter 
	{
		#region Methods

		internal override void WriteObjectValue (RSBinaryWriter _binaryWriter, object _object)
		{
			if (_object == null || _object.Equals(null))
			{
				WriteUnsupportedTypeValue(_binaryWriter, eTypeTag.NULL);
				return;
			}
			
			// Handle serialization based on object type tag
			Type 		_objectType		= _object.GetType();
			eTypeTag 	_typeTag		= TypeMetadata.GetTypeTag(_objectType);

			switch (_typeTag)
			{
			case eTypeTag.UNSUPPORTED:
				Debug.LogWarning(string.Format("[RS] Serialization isnt supported for type={0}.", _objectType));
				WriteUnsupportedTypeValue(_binaryWriter, _typeTag);
				break;

			case eTypeTag.PRIMITIVE:
				WritePrimitiveTypeValue(_binaryWriter, _object, _objectType);
				break;

			case eTypeTag.STRUCT:
				WriteStructTypeValue(_binaryWriter, _object, _objectType);
				break;

			case  eTypeTag.STRING:
				WriteStringTypeValue(_binaryWriter, _object, _objectType);
				break;

			case eTypeTag.CLASS:
				WriteObjectReferenceTypeValue(_binaryWriter, _object, _objectType, _typeTag);
				break;

			default:
				Debug.LogWarning(string.Format("[RS] Unknown type={0}.", _typeTag));
				break;
			}
		}
		
		#endregion
		
		#region Unsupported / Null Type Methods
		
		private void WriteUnsupportedTypeValue (RSBinaryWriter _binaryWriter, eTypeTag _typeTag)
		{
			_binaryWriter.WriteBinaryElement(BinaryElement.OBJECT_DATA);
			_binaryWriter.WriteTypeTag(_typeTag);
		}
		
		#endregion
		
		#region Primitive Methods
		
		private void WritePrimitiveTypeValue (RSBinaryWriter _binaryWriter, object _object, Type _objectType)
		{
			int _typeID			= (int)Type.GetTypeCode(_objectType);
			
			// Write primitive object data
			_binaryWriter.WriteBinaryElement(BinaryElement.OBJECT_DATA);
			_binaryWriter.WriteTypeTag(eTypeTag.PRIMITIVE);
			_binaryWriter.Write(_typeID);
			_binaryWriter.WritePrimitiveValue(_object, (TypeCode)_typeID);
		}
		
		#endregion

		#region Struct Methods

		private void WriteStructTypeValue (RSBinaryWriter _binaryWriter, object _object, Type _objectType)
		{
			// Register object graph type
			UInt32 	_objectTypeID;
			bool	_newType;
			
			TypeMetadata.RegisterType(_objectType, out _objectTypeID, out _newType);
			
			if (_newType)
				TypeMetadata.WriteTypeMetadata(_binaryWriter, _objectType, _objectTypeID);

			// Write object properties
			_binaryWriter.WriteBinaryElement(BinaryElement.OBJECT_DATA);
			_binaryWriter.WriteTypeTag(eTypeTag.STRUCT);
			_binaryWriter.Write(_objectTypeID);
			
			// Write object graph
			WriteObjectGraph(_binaryWriter, _object, _objectType);
		}

		#endregion
		
		#region String Methods
		
		private void WriteStringTypeValue (RSBinaryWriter _binaryWriter, object _object, Type _objectType)
		{
			// Write string object data
			_binaryWriter.WriteBinaryElement(BinaryElement.OBJECT_DATA);
			_binaryWriter.WriteTypeTag(eTypeTag.STRING);
			_binaryWriter.Write(_object as string);
		}
		
		#endregion
		
		#region Object Graph Methods
		
		private void WriteClassTypeValue (RSBinaryWriter _binaryWriter, object _object, Type _objectType, UInt32 _objectReferenceID)
		{
			// Register object graph type
			UInt32 	_objectTypeID;
			bool	_newType;
			
			TypeMetadata.RegisterType(_objectType, out _objectTypeID, out _newType);
			
			if (_newType)
				TypeMetadata.WriteTypeMetadata(_binaryWriter, _objectType, _objectTypeID);
			
			// Write object properties
			_binaryWriter.WriteBinaryElement(BinaryElement.OBJECT_DATA);
			_binaryWriter.WriteTypeTag(eTypeTag.CLASS);
			_binaryWriter.Write(_objectTypeID);
			_binaryWriter.Write(_objectReferenceID);

			// Write object graph
			WriteObjectGraph(_binaryWriter, _object, _objectType);
		}

		private void WriteObjectGraph (RSBinaryWriter _binaryWriter, object _object, Type _objectType)
		{
			// Fetch properties that needs to be serialized
			RuntimeSerializationInfo 			_serializationInfo		= new RuntimeSerializationInfo(_objectType);
			
			// Get serialization entries for this object
			GetObjectData(_object, ref _serializationInfo);
			
			// Write initializers, properties
			WriteSerializationContainer(_binaryWriter, _serializationInfo.InitializerValuesContainer);
			WriteSerializationContainer(_binaryWriter, _serializationInfo.SerializedValuesContainer);
			
			// Trigger serialization finished callback
			if (typeof(IRuntimeSerializationCallback).IsAssignableFrom(_objectType))
				((IRuntimeSerializationCallback)_object).OnAfterRuntimeSerialize();
		}
		
		#endregion
		
		#region Object Reference Methods

		private void WriteObjectReferenceTypeValue (RSBinaryWriter _binaryWriter, object _object, Type _objectType, eTypeTag _typeTag)
		{
			// Check if this object exists in object reference cache
			bool 	_firstTime;
			UInt32 	_objectReferenceID;
			
			RegisterObject(_object, out _objectReferenceID, out _firstTime);
			
			if (_firstTime)
			{
				if (_typeTag == eTypeTag.CLASS)
				{
					WriteClassTypeValue(_binaryWriter, _object, _objectType, _objectReferenceID);
					return;
				}
			}
			else
			{
				_binaryWriter.WriteBinaryElement(BinaryElement.OBJECT_DATA);
				_binaryWriter.WriteTypeTag(eTypeTag.OBJECT_REFERENCE);
				_binaryWriter.Write(_objectReferenceID);
				return;
			}
		}
		
		private void RegisterObject (object _object, out UInt32 _referenceID, out bool _firstTime)
		{
			// Check if this object already exists
			if (ObjectReferenceCache.TryGetValue(_object, out _referenceID))
			{
				_firstTime			= false;
				return;
			}
			
			// Cache this new object
			_referenceID			= ObjectReferenceCounter++;
			_firstTime				= true;
			ObjectReferenceCache.Add(_object, _referenceID);
		}
		
		#endregion

		#region Serialization Methods

		private void GetObjectData (object _object, ref RuntimeSerializationInfo _serializationInfo)
		{
			Type							_objectType			= _serializationInfo.ObjectType;
			RuntimeSerializableAttribute 	_serializableAttr	= SerializationTypeUtil.GetRuntimeSerializableAttribute(_objectType);

			if (_serializableAttr != null)
			{
				// Serialization is controlled by user
				if (typeof(IRuntimeSerializable).IsAssignableFrom(_objectType))
				{
					((IRuntimeSerializable)_object).WriteSerializationData(_serializationInfo);
				}
				// Serialization using Reflection
				else
				{
					Type					_curObjectType		= _objectType;

					while (true)
					{
						// Gather information about all the fields to be deserialized
						if (_serializableAttr != null)
							GetObjectDataUsingReflection(_object, _curObjectType, ref _serializationInfo, _serializableAttr);

						// Tranverse to object's base and check for termiation condition
						_curObjectType 							= _curObjectType.BaseType;
						
						if (_curObjectType == null)
							break;
						
						// Get base type's attribute
						_serializableAttr						= SerializationTypeUtil.GetRuntimeSerializableAttribute(_curObjectType);
					}
				}

				return;
			}
		}

		private void GetObjectDataUsingReflection (object _object, Type _objectType, ref RuntimeSerializationInfo _serializationInfo, RuntimeSerializableAttribute _serializableAttr)
		{
			List<Field>		_serializableFields			= SerializationTypeUtil.GetRuntimeSerializableFields(_objectType, _serializableAttr);
			int 			_serializableFieldCount 	= _serializableFields.Count;
			
			// Iterate through all serialisable fields
			for (int _iter = 0; _iter < _serializableFieldCount; _iter++)
			{
				Field 		_curField					= _serializableFields[_iter];
				FieldInfo	_curFieldInfo				= _curField.Info;
				object 		_curFieldValue;
				
				if (_curFieldInfo.IsStatic)
					_curFieldValue						= _curFieldInfo.GetValue(null);
				else
					_curFieldValue						= _curFieldInfo.GetValue(_object);
				
				// Add this field info
				_serializationInfo.AddValue(_curFieldInfo.Name, _curFieldValue, _curFieldInfo.FieldType, _curField.IsObjectInitializer);
			}
		}
		
		private void WriteSerializationContainer (RSBinaryWriter _binaryWriter, Dictionary<string, RuntimeSerializationEntry> _container)
		{
			Dictionary<string, RuntimeSerializationEntry>.Enumerator _enumerator	 = _container.GetEnumerator();

			// Write count
			_binaryWriter.Write(_container.Count);

			// Write entries
			while (_enumerator.MoveNext())
			{
				RuntimeSerializationEntry 	_curEntry	= _enumerator.Current.Value;
				
				// Write serialization entry
				_binaryWriter.Write(_curEntry.Name);
				
				// Write child properties
				WriteObjectValue(_binaryWriter, _curEntry.Value);
			}
		}

		#endregion
	}
}