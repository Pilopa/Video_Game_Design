using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using VoxelBusters.Utility;

namespace VoxelBusters.RuntimeSerialization.Internal
{
	internal class ObjectReaderSFV1 : ObjectReader 
	{
		#region Methods

		internal override object ReadObjectValue (RSBinaryReader _binaryReader, out Type _objectType, object _object = null)
		{
			BinaryElement _curBinaryElement;
			
			while ((_curBinaryElement = _binaryReader.ReadBinaryElement()) != BinaryElement.OBJECT_DATA)
			{
				TypeMetadata.ReadTypeMetaData(_binaryReader, _curBinaryElement);
			}
			
			if (_curBinaryElement != BinaryElement.OBJECT_DATA)
				throw new Exception(string.Format("[RS] Parsing error. BinaryElement={0}.", _curBinaryElement));

			// Deserialize based on value
			eTypeTag _typeTag	= _binaryReader.ReadTypeTag();
			object _objectValue	= null;
			
			switch (_typeTag)
			{
			case eTypeTag.NULL:
			case eTypeTag.UNSUPPORTED:
				_objectType		= null;
				_objectValue	= null;
				break;
				
			case eTypeTag.PRIMITIVE:
				_objectValue	= ReadPrimitiveTypeValue(_binaryReader, out _objectType);
				break;
				
			case eTypeTag.STRUCT:
				_objectValue	= ReadStructTypeValue(_binaryReader, out _objectType, _object);
				break;
				
			case eTypeTag.STRING:
				_objectValue	= ReadStringTypeValue(_binaryReader, out _objectType);
				break;
				
			case eTypeTag.CLASS:
				_objectValue	= ReadClassTypeValue(_binaryReader, out _objectType, _object);
				break;
				
			case eTypeTag.OBJECT_REFERENCE:
				_objectValue	= ReadObjectReferenceValue(_binaryReader, out _objectType);
				break;
				
			default:
				throw new Exception(string.Format("[RS] Unsupported type tag{0}. For more supported types, Purchase full version http://u3d.as/g8b", _typeTag));
			}
			
			return _objectValue;
		}
		
		private object ReadObjectValue (RSBinaryReader _binaryReader)
		{
			Type _objectType;
			
			return ReadObjectValue(_binaryReader, out _objectType);
		}
		
		#endregion
		
		#region Primitive Methods
		
		private object ReadPrimitiveTypeValue (RSBinaryReader _binaryReader, out Type _objectType)
		{
			// Get object type
			UInt32	_typeID				= _binaryReader.ReadUInt32();
			_objectType					= TypeMetadata.GetType(_typeID);
			
			// Get object value
			return _binaryReader.ReadPrimitiveValue((TypeCode)_typeID);
		}
		
		#endregion

		#region Struct Methods

		private object ReadStructTypeValue (RSBinaryReader _binaryReader, out Type _objectType, object _object)
		{
			// Get object type
			UInt32		_typeID			= _binaryReader.ReadUInt32();
			_objectType					= TypeMetadata.GetType(_typeID);

			// Read object refernece
			Dictionary<string, RuntimeSerializationEntry>	_initValuesContainer	= new Dictionary<string, RuntimeSerializationEntry>();
			Dictionary<string, RuntimeSerializationEntry>	_serValuesContainer		= new Dictionary<string, RuntimeSerializationEntry>();
			RuntimeSerializationInfo						_serializationInfo		= new RuntimeSerializationInfo(_objectType, _initValuesContainer, _serValuesContainer);
			
			// Read initializers and properties
			ReadSerializationContainer(_binaryReader, ref _initValuesContainer);
			ReadSerializationContainer(_binaryReader, ref _serValuesContainer);

			if (_object == null)
				_object					= CreateInstance(_binaryReader, _serializationInfo);

			// Set object value
			_object						= SetObjectData(_object, _serializationInfo);
			
			// Trigger deserialization callback
			if (typeof(IRuntimeSerializationCallback).IsAssignableFrom(_objectType))
				((IRuntimeSerializationCallback)_object).OnAfterRuntimeDeserialize();
			
			return _object;
		}

		#endregion
		
		#region String Methods
		
		private object ReadStringTypeValue (RSBinaryReader _binaryReader, out Type _objectType)
		{
			// Get object type
			_objectType					= typeof(string);
			
			// Get object value
			return _binaryReader.ReadString();
		}
		
		#endregion
		
		#region Object Graph Methods
		
		private object ReadClassTypeValue (RSBinaryReader _binaryReader, out Type _objectType, object _object)
		{
			// Read properties
			UInt32		_typeID					= _binaryReader.ReadUInt32();
			UInt32		_objectReferenceID		= _binaryReader.ReadUInt32();

			// Get object type
			_objectType							= TypeMetadata.GetType(_typeID); 
			
			// Read object refernece
			Dictionary<string, RuntimeSerializationEntry>	_initValuesContainer	= new Dictionary<string, RuntimeSerializationEntry>();
			Dictionary<string, RuntimeSerializationEntry>	_serValuesContainer		= new Dictionary<string, RuntimeSerializationEntry>();
			RuntimeSerializationInfo						_serializationInfo		= new RuntimeSerializationInfo(_objectType, _initValuesContainer, _serValuesContainer);

			// Read initializers and create instance, if required
			ReadSerializationContainer(_binaryReader, ref _initValuesContainer);

			if (_object == null)
				_object							= CreateInstance(_binaryReader, _serializationInfo);

			ObjectReferenceCache.Add(_object, _objectReferenceID);

			// Read properties
			ReadSerializationContainer(_binaryReader, ref _serValuesContainer);

			// Set object value
			_object								= SetObjectData(_object, _serializationInfo);
			
			// Trigger deserialization callback
			if (typeof(IRuntimeSerializationCallback).IsAssignableFrom(_objectType))
				((IRuntimeSerializationCallback)_object).OnAfterRuntimeDeserialize();
			
			return _object;
		}
		
		#endregion
		
		#region Object Reference Methods
		
		private object ReadObjectReferenceValue (RSBinaryReader _binaryReader, out Type _objectType)
		{
			UInt32 	_objectReferenceID								= _binaryReader.ReadUInt32();
			object 	_object;
			
			// Find object using reference ID
			Dictionary<object, UInt32>.Enumerator _dictEnumerator	= ObjectReferenceCache.GetEnumerator();
			
			while (_dictEnumerator.MoveNext())
			{
				KeyValuePair<object, UInt32> 	_keyValuePair		= _dictEnumerator.Current;
				UInt32 							_currentReferenceID	= _keyValuePair.Value;
				
				if (_currentReferenceID == _objectReferenceID)
				{
					_object											= _keyValuePair.Key;
					_objectType										= _object.GetType();
					
					return _object;
				}
			}
			
			throw new Exception(string.Format("[RS] Object Reference not found for ID={0}.", _objectReferenceID));
		}
		
		#endregion 

		#region Serialization Methods
		
		private void ReadSerializationContainer (RSBinaryReader _binaryReader, ref Dictionary<string, RuntimeSerializationEntry> _container)
		{
			int		_memberCount	= _binaryReader.ReadInt32();
			int		_iter			= 0;
			
			while (_iter < _memberCount)
			{
				// Get property name and type
				string	_pName		= _binaryReader.ReadString();
				Type	_pType;
				object	_pValue		= ReadObjectValue(_binaryReader, out _pType);
				
				// Add new serialization entry
				_container.Add(_pName, new RuntimeSerializationEntry(_pName, _pValue, _pType));
				
				// Increment
				_iter++;
			}
		}

		private object CreateInstance (RSBinaryReader _binaryReader, RuntimeSerializationInfo _serilizationInfo)
		{
			Type							_objectType				= _serilizationInfo.ObjectType;
			RuntimeSerializableAttribute	_serializableAttr		= SerializationTypeUtil.GetRuntimeSerializableAttribute(_objectType);

			if (_serializableAttr != null)
			{
				if (typeof(IRuntimeSerializableActivator).IsAssignableFrom(_objectType))
				{
					MethodInfo				_staticInstanceCreator	= _objectType.GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Static);

					if (_staticInstanceCreator != null)
						return _staticInstanceCreator.Invoke(null, new object[] { _serilizationInfo });
				}
			}

			// Fallback condition
			return Activator.CreateInstance(_objectType);
		}
		
		private object SetObjectData (object _object, RuntimeSerializationInfo _serializationInfo)
		{
			Type							_objectType			= _serializationInfo.ObjectType;
			RuntimeSerializableAttribute 	_serializableAttr	= SerializationTypeUtil.GetRuntimeSerializableAttribute(_objectType);
			
			if (_serializableAttr != null)
			{
				// Serialization is controlled by user
				if (typeof(IRuntimeSerializable).IsAssignableFrom(_objectType))
				{
					_object										= ((IRuntimeSerializable)_object).ReadSerializationData(_serializationInfo);					
				}
				// Serialization using Reflection
				else
				{
					Type					_curObjectType		= _objectType;
					
					while (true)
					{
						// Gather information about all the fields to be serialized
						if (_serializableAttr != null)
							_object								= SetObjectDataUsingReflection(_object, _curObjectType, _serializationInfo, _serializableAttr);

						// Tranverse upwards to object's base and check for termiation condition
						_curObjectType 							= _curObjectType.BaseType;

						if (_curObjectType == null)
							break;

						// Get base type's attribute
						_serializableAttr						= SerializationTypeUtil.GetRuntimeSerializableAttribute(_curObjectType);
					}
				}
			}
			
			return _object;
		}

		private object SetObjectDataUsingReflection (object _object, Type _objectType, RuntimeSerializationInfo _serializationInfo, RuntimeSerializableAttribute _serializableAttr)
		{
			List<Field>		_serializableFields			= SerializationTypeUtil.GetRuntimeSerializableFields(_objectType, _serializableAttr);
			int 			_serializableFieldCount 	= _serializableFields.Count;
			
			// Iterate through all serialisable fields
			for (int _iter = 0; _iter < _serializableFieldCount; _iter++)
			{
				Field 		_curField					= _serializableFields[_iter];
				FieldInfo	_curFieldInfo				= _curField.Info;
				object 		_curFieldValue				= _serializationInfo.GetValue(_curFieldInfo.Name, _curFieldInfo.FieldType, _curField.IsObjectInitializer);
				
				// Set this new value
				if (_curFieldInfo.IsStatic)
					_curFieldInfo.SetValue(null, 	_curFieldValue);
				else
					_curFieldInfo.SetValue(_object, _curFieldValue);
			}

			return _object;
		}

		#endregion
	}
}