using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using VoxelBusters.Utility;

namespace VoxelBusters.RuntimeSerialization.Internal
{
	internal class SerializationTypeUtil
	{
		#region Properties

		private 	static		Dictionary<Type, RuntimeSerializableAttribute>		serializableAttributeCache;
		private 	static 		Dictionary<Type, List<Field>>						typeMemberInfoCache;

		#endregion

		#region Constructor

		static SerializationTypeUtil ()
		{
			serializableAttributeCache		= new Dictionary<Type, RuntimeSerializableAttribute>();
			typeMemberInfoCache				= new Dictionary<Type, List<Field>>();
		}

		#endregion

		#region RS Methods

		internal static void Initialise ()
		{}
	
		internal static void Purge (Type _type)
		{
			if (_type == null)
				throw new Exception("[RS] Type cant be null.");

			// Clear information cached for this type
			serializableAttributeCache.Remove(_type);
			typeMemberInfoCache.Remove(_type);
		}

		#endregion

		#region Type Check Methods
	
		internal static bool IsPrimitive (Type _type)
		{
			return (_type.IsPrimitive || _type == typeof(DateTime));
		}
		
		internal static bool IsRuntimeSerializableObject (Type _objectType)
		{
			// Check if type supports member based serialization
			RuntimeSerializableAttribute 	_serializableAttr	= GetRuntimeSerializableAttribute(_objectType);
			
			return (_serializableAttr != null);
		}
		
		private static bool IsRuntimeSerializable (FieldInfo _field)
		{
			return (GetRSFieldAttribute(_field) != null);
		}

		#endregion

		#region Serializable Attribute Methods

		internal static RuntimeSerializableAttribute GetRuntimeSerializableAttribute (Type _objectType)
		{	
			RuntimeSerializableAttribute _serializableAttr	= null;

			lock (serializableAttributeCache)
			{
				// If cached value doesnt exist, then check if object implements RuntimeSerializableAttribute
				if (!serializableAttributeCache.TryGetValue(_objectType, out _serializableAttr))
				{
					_serializableAttr						= _objectType.GetAttribute<RuntimeSerializableAttribute>(false);
				
					// Add it to cache collection
					serializableAttributeCache[_objectType]	= _serializableAttr;
				}
			}

			return _serializableAttr;
		}

		internal static RuntimeSerializeFieldAttribute GetRSFieldAttribute (FieldInfo _field)
		{
			return _field.GetAttribute<RuntimeSerializeFieldAttribute>(false);
		}

		#endregion

		#region Fields Methods
		
		internal static List<Field> GetRuntimeSerializableFields (Type _objectType, RuntimeSerializableAttribute _runtimeSerializableAttr)
		{	
			List<Field> 			_serializableFields				= null;

			lock (typeMemberInfoCache)
			{
				// If cached value doesnt exist, then use Reflection to get list of RuntimeSerializable fields
				if (!typeMemberInfoCache.TryGetValue(_objectType, out _serializableFields))
				{
					bool 			_serializeAllPublicFields 		= false;
					bool 			_serializeAllNonPublicFields	= false;

					if (_runtimeSerializableAttr != null)
					{
						_serializeAllPublicFields					= _runtimeSerializableAttr.SerializeAllPublicVariables;
						_serializeAllNonPublicFields				= _runtimeSerializableAttr.SerializeAllNonPublicVariables;
					} 

					// Using reflection fetch all the fields 
					FieldInfo[] 	_publicFields					= _objectType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static);
					FieldInfo[] 	_nonPublicFields				= _objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static);
					
					// List holds both public and non-public fields which needs to be serialised
					_serializableFields								= new List<Field>(_publicFields.Length + _nonPublicFields.Length);
					
					FilterOutNonSerializableFields(_publicFields, 		_serializeAllPublicFields, 		ref _serializableFields);
					FilterOutNonSerializableFields(_nonPublicFields, 	_serializeAllNonPublicFields, 	ref _serializableFields);

					// Cache member 
					typeMemberInfoCache[_objectType]				= _serializableFields;
				}
			}

			return _serializableFields;
		}
		
		private static void FilterOutNonSerializableFields (FieldInfo[] _fieldList, bool _overrideFlag, ref List<Field> _filteredList)
		{
			// Iterating through the fields which are either public, private, protected or instance
 			for (int _fIter = 0;  _fIter < _fieldList.Length; _fIter++)
			{
				FieldInfo						_currentField	= _fieldList[_fIter];

				// Constants fields should be ignored
				if (_currentField.IsLiteral)
					continue;

				RuntimeSerializeFieldAttribute 	_attribute		= GetRSFieldAttribute(_currentField);

				// Check if override flag is enabled,
				if (_overrideFlag)
				{
					_filteredList.Add(new Field(_currentField, (_attribute == null) ? false : _attribute.IsObjectInitializer));
					continue;
				}

				// Check if field type can be serialised
				if (_attribute != null)
				{
					_filteredList.Add(new Field(_currentField, _attribute.IsObjectInitializer));
					continue;
				}
			}
		}

		#endregion
	}
}