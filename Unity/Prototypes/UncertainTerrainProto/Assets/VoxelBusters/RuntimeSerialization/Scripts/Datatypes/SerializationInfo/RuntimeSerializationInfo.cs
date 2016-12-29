using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using VoxelBusters.Utility;

namespace VoxelBusters.RuntimeSerialization
{
	using Internal;

	/// <summary>
	/// Stores all the data required to serialize or deserialize an object.
	/// </summary>
	public class RuntimeSerializationInfo
	{
		#region Properties

		public			Type											ObjectType
		{
			get;
			private set;
		}

		internal		Dictionary<string, RuntimeSerializationEntry>	InitializerValuesContainer
		{
			get;
			private set;
		}

		internal		Dictionary<string, RuntimeSerializationEntry>	SerializedValuesContainer	
		{
			get;
			private set;
		}

		public			int												MemberCount
		{
			get
			{
				return SerializedValuesContainer.Count + InitializerValuesContainer.Count;
			}
		}

		#endregion

		#region Constructors

		private RuntimeSerializationInfo ()
		{}

		internal RuntimeSerializationInfo (Type _targetType)
		{
			if (_targetType == null)
				throw new NullReferenceException("Target object type cant be null.");
			
			// Set properties
			ObjectType						= _targetType;
			InitializerValuesContainer		= new Dictionary<string, RuntimeSerializationEntry>();
			SerializedValuesContainer		= new Dictionary<string, RuntimeSerializationEntry>();
		}

		internal RuntimeSerializationInfo (Type _targetType, Dictionary<string, RuntimeSerializationEntry> _initializersContainer, Dictionary<string, RuntimeSerializationEntry> _propertiesContainer)
		{
			if (_targetType == null)
				throw new NullReferenceException("Target object type cant be null.");

			// Set properties
			ObjectType						= _targetType;
			InitializerValuesContainer		= _initializersContainer;
			SerializedValuesContainer		= _propertiesContainer;
		}

		#endregion

		#region Add Value Methods

		/// <summary>
		/// Adds the specified object into the <see cref="RuntimeSerializationInfo"/> for serialization, where it is associated with name.
		/// </summary>
		/// <param name="_name">The name to associate with the value, so it can be deserialized later.</param>
		/// <param name="_value">The value to be serialized. Any children of this object will automatically be serialized.</param>
		/// <param name="_isObjectInitializer">The flag indicates whether this value is object intializer type value. Object initializers let you assign values to any accessible fields of an object at creation time.</param>
		/// <typeparam name="T">The Type associated with the current object. This must always be the type of the object itself.</typeparam>
		public void AddValue <T> (string _name, T _value, bool _isObjectInitializer = false)
		{
			AddValue(_name, _value, typeof(T), _isObjectInitializer);
		}

		/// <summary>
		/// Adds the specified object into the <see cref="RuntimeSerializationInfo"/> for serialization, where it is associated with name.
		/// </summary>
		/// <param name="_name">The name to associate with the value, so it can be deserialized later.</param>
		/// <param name="_value">The value to be serialized. Any children of this object will automatically be serialized.</param>
		/// <param name="_valueType">The Type associated with the current object. This must always be the type of the object itself.</param>
		/// <param name="_isObjectInitializer">The flag indicates whether this value is object intializer type value. Object initializers let you assign values to any accessible fields of an object at creation time.</param>
		public void AddValue (string _name, object _value, Type _valueType, bool _isObjectInitializer = false)
		{	
			RuntimeSerializationEntry	_newEntry	= new RuntimeSerializationEntry(_name, _value, _valueType);

			if (_isObjectInitializer)
			{
				if (InitializerValuesContainer == null)
					throw new NullReferenceException("Serialized initializers container is null.");

				InitializerValuesContainer.Add(_name, _newEntry);
			}
			else
			{
				if (SerializedValuesContainer == null)
					throw new NullReferenceException("Serialized values container is null.");

				SerializedValuesContainer.Add(_name, _newEntry);
			}
		}

		#endregion

		#region Get Value Methods

		/// <summary>
		/// Retrieves value from <see cref="RuntimeSerializationInfo"/>.
		/// </summary>
		/// <returns>The object of specified type associated with name.</returns>
		/// <param name="_name">The name associated with the value to retrieve.</param>
		/// <param name="_isObjectInitializer">The flag indicates whether this value is object intializer type value. Object initializers let you assign values to any accessible fields of an object at creation time.</param>
		/// <typeparam name="T">The Type of the value to retrieve.</typeparam>
		public T GetValue <T> (string _name, bool _isObjectInitializer = false)
		{
			return (T)GetValue(_name, typeof(T), _isObjectInitializer);
		}

		/// <summary>
		/// Retrieves value from <see cref="RuntimeSerializationInfo"/>.
		/// </summary>
		/// <returns>The object of specified type associated with name.</returns>
		/// <param name="_name">The name associated with the value to retrieve.</param>
		/// <param name=="_type">The Type of the value to retrieve.</param>
		/// <param name="_isObjectInitializer">The flag indicates whether this value is object intializer type value. Object initializers let you assign values to any accessible fields of an object at creation time.</param>
		public object GetValue (string _name, Type _type, bool _isObjectInitializer = false)
		{
			// Select appropriate serialized values container
			Dictionary<string, RuntimeSerializationEntry> _container	= null;

			if (_isObjectInitializer)
				_container	= InitializerValuesContainer;
			else
				_container	= SerializedValuesContainer;

			// Fetch value associated with given name and check target values validity
			if (_container == null)				
				return _type.DefaultValue();
			
			RuntimeSerializationEntry _entry;
			
			if (!_container.TryGetValue(_name, out _entry))
				return _type.DefaultValue();
			
			if (_entry.Value != null && _type.IsInstanceOfType(_entry.Value))	
				return _entry.Value;
			
			return _type.DefaultValue();
		}

		#endregion
	}
}