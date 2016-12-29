using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using VoxelBusters.Utility;
using VoxelBusters.DesignPatterns;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace VoxelBusters.RuntimeSerialization
{
	using Internal;

	/// <summary>
	/// RSManager class is responsible for serializing and deserializing objects at runtime.
	/// </summary>
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	public class RSManager : SingletonPattern <RSManager>
	{
		#region Constants
		
		private 	const		string									kIdentifiersForPrefsKey			= "rs-pref-ids";
		private 	const		string									kIdentifiersForFilesKey			= "rs-files-ids";
		private 	const		string									kRSManagerUID					= "RS-Manager";

		#endregion

		#region Static Properties

		public new 	static 		RSManager 								Instance 
		{
			get
			{
				RSManager 		_instance				= SingletonPattern<RSManager>.Instance;

#if UNITY_EDITOR
				// Special case: To ensure same script works well in edit mode as well.
				if (!EditorApplicationIsPlaying())
				{
					if (_instance != null && !_instance.IsInitialized)
						_instance.Awake();
				}
#endif
				return _instance;
			}

			private set
			{
				SingletonPattern<RSManager>.Instance	= value;
			}
		}

		#endregion

		#region Properties

		private					Dictionary<string, List<IRuntimeSerializationEventListener>>	m_serializationListeners;
		private					Dictionary<string, byte[]>				m_save2PrefsSerializationData;
		private					List<string>							m_save2FileSerializationIdentifiers;
		
		// Sub systems
		private 				BinarySerializer						m_binarySerializer;
		private 				BinaryDeserializer						m_binaryDeserializer;

		#endregion
		
		#region Events
		
		public		static		Action									OnDestroyEvent					= null;
		
		#endregion

		#region Constructors
	
#if UNITY_EDITOR
		static RSManager ()
		{
			EditorApplication.playmodeStateChanged 	-= ApplicationStateChanged;
			EditorApplication.playmodeStateChanged	+= ApplicationStateChanged;
			EditorApplication.update				-= ApplicationUpdate;
			EditorApplication.update				+= ApplicationUpdate;
		}
#endif

		#endregion

		#region Unity Methods

		protected override void Awake ()
		{
			base.Awake ();

			if (instance != this)
				return;

			// Initialise
			m_save2PrefsSerializationData		= new Dictionary<string, byte[]>();
			m_save2FileSerializationIdentifiers	= new List<string>();
			m_serializationListeners			= new Dictionary<string, List<IRuntimeSerializationEventListener>>();
			m_binarySerializer					= new BinarySerializer();
			m_binaryDeserializer				= new BinaryDeserializer();

			// Initialise utility classes
			SerializationTypeUtil.Initialise();

			// Load all the serialization data
			LoadSerializationData();
		}
		
		private void OnApplicationPause (bool _isPaused)
		{
#if !UNITY_EDITOR
			// Save serialization data when application is moved to paused state
			if (_isPaused)
				Obj_Save();
#endif
		}

		protected override void Reset ()
		{
			base.Reset ();

			// Reset properties
			m_save2PrefsSerializationData		= null;
			m_save2FileSerializationIdentifiers	= null;
			m_serializationListeners			= null;
			m_binarySerializer					= null;
			m_binaryDeserializer				= null;
		}
		
		protected override void OnDestroy ()
		{
			if (instance == this)
			{
				// Trigger callback
				if (OnDestroyEvent != null)
					OnDestroyEvent();
				
				// Save serialization data
				Obj_Save();
			}
		
			// Call base
			base.OnDestroy();
		} 
		
		#endregion

		#region Serialize [No Save] Methods

		/// <summary>
		/// Returns serialization data of target object as Base64 string. 
		/// Serialization data associated with this object is not saved by <see cref="RSManager"/>, so use <see cref="DeserializeData"/> method to deserialize and retrieve formerly serialized object.
		/// </summary>
		/// <param name="_object">Object to serialize.</param>
		/// <param name="_serializationID">Identifier associated with object serialization. Used only for sending callbacks, when serialization finishes. If not specified, serialization callbacks are not fired.</param>
		/// <typeparam name="T">Type of the object to serialize.</typeparam>
		public static string Serialize <T> (T _object, string _serializationID = null)
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] Serialization failed. RSManager instance not found.");
				return null;
			}

			return Instance.Obj_Serialize<T>(_object, _serializationID);
		}
		
		private string Obj_Serialize <T> (T _object, string _serializationID = null)
		{
			// Serialize object 
			byte[] 				_serializationData		= m_binarySerializer.Serialize(_object, typeof(T));
			
			// Invoke event
			if (!string.IsNullOrEmpty(_serializationID))
				OnSerializationFinished(_serializationID);
			
			// Return serialization data
			return Convert.ToBase64String(_serializationData);
		}

		/// <summary>
		/// Deserializes the serialization data and recreates the object of specified type.
		/// </summary>
		/// <returns>The deserialized object of specified type.</returns>
		/// <param name="_serializationDataBase64">Serialization data in Base64String format.</param>
		/// <param name="_serializationID">Identifier associated with serialization data.</param>
		/// <param name="_targetObject">Deserialized value is assigned to this instance, if value is not null.</param>
		/// <typeparam name="T">The Type of the value to deserialize.</typeparam>
		public static T DeserializeData <T> (string _serializationDataBase64, string _serializationID = null, T _targetObject = default(T))
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] Deserialization failed. RSManager instance not found.");
				return default(T);
			}
			
			return Instance.Obj_DeserializeData<T>(_serializationDataBase64, _serializationID, _targetObject);
		}
		
		public T Obj_DeserializeData <T> (string _serializationDataBase64, string _serializationID = null, T _targetObject = default(T))
		{
			if (string.IsNullOrEmpty(_serializationDataBase64))
			{
				Debug.LogError("[RS] Deserialization failed. Serialization data cant null/empty.");
				return default(T);
			}
			
			// Deserialise serialization data
			byte[] 				_serializationData		= Convert.FromBase64String(_serializationDataBase64);
			T 					_deserializedObject		= m_binaryDeserializer.Deserialize<T>(_serializationData, _targetObject);
			
			// Invoke event
			if (!string.IsNullOrEmpty(_serializationID))
				OnDeserializationFinished(_serializationID, _deserializedObject);
			
			// Return object
			return _deserializedObject;
		}

		#endregion

		#region Serialize [With Save] Methods

		/// <summary>
		/// Serialize the specified object and save serialization data to specified target where it is associated with an identifier.
		/// </summary>
		/// <param name="_object">Object to serialize.</param>
		/// <param name="_serializationID">Identifier associated with serialization data of the target object.</param>
		/// <param name="_saveTarget">Save target for serialization data.</param>
		/// <typeparam name="T">Type of the object to serialize.</typeparam>
		public static void Serialize <T> (T _object, string _serializationID, eSaveTarget _saveTarget)
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] Serialization failed. RSManager instance not found.");
				return;
			}

			Instance.Obj_Serialize<T>(_object, _serializationID, _saveTarget);
		}
		
		private void Obj_Serialize <T> (T _object, string _serializationID, eSaveTarget _saveTarget)
		{
			// Check if serialization identifier is non-null
			if (string.IsNullOrEmpty(_serializationID))
			{
				Debug.LogError("[RS] Serialization identifier cant be null/empty.");
				return;
			}
			
			// Serialize object
			byte[] 				_serializationData		= m_binarySerializer.Serialize(_object, typeof(T));
			
			// Cache serialization data
			AddSerializationData(_serializationID, _serializationData, _saveTarget);
			
			// Invoke event
			OnSerializationFinished(_serializationID);
		}
	
		/// <summary>
		/// Deserializes the data serialization associated with identifier and recreates the object of specified type.
		/// </summary>
		/// <param name="_serializationID">Identifier associated with serialization data.</param>
		/// <param name="_targetObject">Deserialized value is assigned to this instance, if value is not null.</param>
		/// <typeparam name="T">The Type of the value to deserialize.</typeparam>
		public static T Deserialize <T> (string _serializationID, T _targetObject = default(T))
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] Deserialization failed. RSManager instance not found.");
				return default(T);
			}

			return Instance.Obj_Deserialize<T>(_serializationID, _targetObject);
		}

		private T Obj_Deserialize <T> (string _serializationID, T _targetObject = default(T))
		{
			byte[] 					_serializationData	= null;
			
			// Get serialization data and deserialize it
			if (Obj_TryGetSerializationData(_serializationID, out _serializationData))
			{
				T 					_deserializedObject	= m_binaryDeserializer.Deserialize<T>(_serializationData, _targetObject);
				
				// Invoke event
				OnDeserializationFinished(_serializationID, _deserializedObject);

				// Return deserialized object
				return _deserializedObject;
			}

			Debug.LogError(string.Format("[RS] Deserialization failed. Couldnt find serialization data for ID={0}.", _serializationID));
			return default(T);
		}

		#endregion

		#region Serialization Data Methods

		/// <summary>
		/// Returns serialization data associated with given identifier.
		/// Ideal for supporting multi device login, wherein you can remotely save serialization data and restore it on other devices using <see cref="RSManager.RestoreSerializationData"/>.
		/// </summary>
		/// <returns>Serialization data as Base64String.</returns>
		/// <param name="_serializationID">Identifier associated with serialization data.</param>
		public static string GetSerializationData (string _serializationID)
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] RSManager instance not found.");
				return null;
			}

			return Instance.Obj_GetSerializationData(_serializationID);
		}

		private string Obj_GetSerializationData (string _serializationID)
		{
			byte[] 				_serializationData		= null;

			if (Obj_TryGetSerializationData(_serializationID, out _serializationData))
				return Convert.ToBase64String(_serializationData);

			throw new Exception(string.Format("[RS] Couldnt find serialization data for ID={0}.", _serializationID));
		}

		public static bool TryGetSerializationData (string _serializationID, out byte[] _serializationData)
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] RSManager instance not found.");
				_serializationData	= null;

				return false;
			}
			
			return Instance.Obj_TryGetSerializationData(_serializationID, out _serializationData);
		}

		private bool Obj_TryGetSerializationData (string _serializationID, out byte[] _serializationData)
		{
			// Try to get serialization data from player prefs
			if (m_save2PrefsSerializationData.TryGetValue(_serializationID, out _serializationData))
				return true;
			
			// Try to get serialization data from files
			string	_filePath		= GetFilePathForIdentifier(_serializationID);
			bool	_fileExists		= FileOperations.Exists(_filePath);
			
			if (_fileExists)
				_serializationData	= FileOperations.ReadAllBytes(_filePath);
			
			return _fileExists;
		}

		/// <summary>
		/// Saves Base64String format serialization data to specified target location where it is associated with an identifier.
		/// </summary>
		/// <param name="_serializationDataBase64">Serialization data in Base64String format.</param>
		/// <param name="_serializationID">Identifier associated with serialization data.</param>
		/// <param name="_saveTarget">Serialization data save target.</param>
		public static bool RestoreSerializationData (string _serializationDataBase64, string _serializationID, eSaveTarget _saveTarget)
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] RSManager instance not found.");
				return false;
			}

			return Instance.Obj_RestoreSerializationData(_serializationDataBase64, _serializationID, _saveTarget);
		}

		private bool Obj_RestoreSerializationData (string _serializationDataBase64, string _serializationID, eSaveTarget _saveTarget)
		{
			if (string.IsNullOrEmpty(_serializationDataBase64))
			{
				Debug.LogError("[RS] Restoring data failed. Serialization data cant null/empty.");
				return false;
			}

			// Deserialise
			byte[]				_serializationData			= Convert.FromBase64String(_serializationDataBase64);
			int 				_serializedFormatVersion	= m_binaryDeserializer.GetVersion(_serializationData);

			// Check if serialization data is compatible with code
			if (_serializedFormatVersion <= Constants.kSerializationSupportedFormatVersions)
			{
				AddSerializationData(_serializationID, _serializationData, _saveTarget);
				return true;
			}

			Debug.LogWarning("[RS] Failed to restore. Serialized data format not supported please update SDK to the most recent version.");
			return false;
		}

		private void AddSerializationData (string _serializationID, byte[] _serializationData, eSaveTarget _saveTarget)
		{
			if (_saveTarget == eSaveTarget.PLAYER_PREFS)
			{
				m_save2PrefsSerializationData[_serializationID]	= _serializationData;
			}
			else
			{
				string	_filePath	= GetFilePathForIdentifier(_serializationID);
				
				// Cache identifier's that were used for serializing to file	
				if (!m_save2FileSerializationIdentifiers.Contains(_serializationID))
					m_save2FileSerializationIdentifiers.Add(_serializationID);
				
				// Save to file				
				FileOperations.WriteAllBytes(_filePath, _serializationData);
			}
		}

		#endregion

		#region Event Listener Methods

		/// <summary>
		/// Register object to receive serialization events.
		/// </summary>
		/// <param name="_serializationID">Identifier associated with serialization.</param>
		/// <param name="_newListener">Instance to be registered as listener.</param>
		public static void RegisterEventListener (string _serializationID, IRuntimeSerializationEventListener _newListener)
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] RSManager instance not found.");
				return;
			}

			Instance.Obj_RegisterEventListener(_serializationID, _newListener);
		}

		private void Obj_RegisterEventListener (string _serializationID, IRuntimeSerializationEventListener _newListener)
		{
			if (string.IsNullOrEmpty(_serializationID))
			{
				Debug.LogError("[RS] Failed to register. Serialization identifier cant be null / empty.");
				return;
			}

			if (_newListener == null)
			{
				Debug.LogError("[RS] Failed to register. Listener instance cant be null.");
				return;
			}

			// Get list of callbacks registered for this identifier
			List<IRuntimeSerializationEventListener> _eventListeners	= GetEventListeners(_serializationID);

			if (_eventListeners == null)
			{
				_eventListeners											= new List<IRuntimeSerializationEventListener>();
				m_serializationListeners[_serializationID]				= _eventListeners;
			}

			// Make sure we dont have duplicates
			if (!_eventListeners.Contains(_newListener))
				_eventListeners.Add(_newListener);
		}

		/// <summary>
		/// Unregister object from receiving serialization events.
		/// </summary>
		/// <param name="_serializationID">Identifier associated with serialization data.</param>
		/// <param name="_callback">Target object to be unregistered from serialization callbacks.</param>
		public static void UnRegisterEventListener (string _serializationID, IRuntimeSerializationEventListener _listener)
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] RSManager instance not found.");
				return;
			}

			Instance.Obj_UnRegisterEventListener(_serializationID, _listener);
		}

		private void Obj_UnRegisterEventListener (string _serializationID, IRuntimeSerializationEventListener _listener)
		{
			if (string.IsNullOrEmpty(_serializationID))
			{
				Debug.LogError("[RS] Failed to unregister. Serialization identifier cant be null / empty.");
				return;
			}

			if (_listener == null)
			{
				Debug.LogError("[RS] Failed to unregister. Listener instance cant be null.");
				return;
			}

			// Get list of callbacks registered for this identifier
			List<IRuntimeSerializationEventListener> _eventListeners	= GetEventListeners(_serializationID);

			// Remove target callback from the callback list
			if (_eventListeners != null)
				_eventListeners.Remove(_listener);
		}

		private List<IRuntimeSerializationEventListener> GetEventListeners (string _serializationID)
		{
			List<IRuntimeSerializationEventListener> _eventListeners	= null;
			
			// Fetch callback list associated with this identifier
			m_serializationListeners.TryGetValue(_serializationID, out _eventListeners);
			
			return _eventListeners;
		}
		
		private void OnSerializationFinished (string _serializationID)
		{
			// Get callback list associated with this identifier
			List<IRuntimeSerializationEventListener> 	_eventListeners	= GetEventListeners(_serializationID);
			
			if (_eventListeners == null)
				return;
			
			for (int _iter = 0; _iter < _eventListeners.Count; _iter++)
			{
				IRuntimeSerializationEventListener 		_listener		= _eventListeners[_iter];
				
				if (_listener != null)
					_listener.OnAfterRuntimeSerialize(_serializationID);
			}
		}
		
		private void OnDeserializationFinished (string _serializationID, object _deserializedObject)
		{
			// Get callback list associated with this identifier
			List<IRuntimeSerializationEventListener> 	_eventListeners	= GetEventListeners(_serializationID);
			
			if (_eventListeners == null)
				return;
			
			for (int _iter = 0; _iter < _eventListeners.Count; _iter++)
			{
				IRuntimeSerializationEventListener 		_listener		= _eventListeners[_iter];
				
				if (_listener != null)
					_listener.OnAfterRuntimeDeserialize(_serializationID, _deserializedObject);
			}
		}

		#endregion

		#region Load Methods

		private void LoadSerializationData ()
		{
			LoadDataRelatedToSaveTargetPrefs();

#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
			LoadDataRelatedToSaveTargetFile();
#endif
		}
		
		private void LoadDataRelatedToSaveTargetPrefs ()
		{
			List<string>	_identifiers				= LoadSerializationIdentifiers(kIdentifiersForPrefsKey);
			
			if (_identifiers == null)
				return;
			
			for (int _iter = 0; _iter < _identifiers.Count; _iter++)
			{
				string		_identifier					= _identifiers[_iter];
				string		_serializationDataBase64	= PlayerPrefs.GetString(_identifier, null);

				if (string.IsNullOrEmpty(_serializationDataBase64))
					continue;

				// Add it to dictionary
				m_save2PrefsSerializationData.Add(_identifier, Convert.FromBase64String(_serializationDataBase64));
			}
		}

#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
		private void LoadDataRelatedToSaveTargetFile ()
		{
			List<string> 	_identifiers		= LoadSerializationIdentifiers(kIdentifiersForFilesKey);
			
			if (_identifiers == null)
				return;

			// Assign value
			m_save2FileSerializationIdentifiers	= _identifiers;
		}
#endif

		private List<string> LoadSerializationIdentifiers (string _key)
		{
			string 			_identifiersJSONStr		= PlayerPrefs.GetString(_key);
			
			if (string.IsNullOrEmpty(_identifiersJSONStr))
				return null;
			
			// Read saved identifiers
			using (MemoryStream _memoryStream = new MemoryStream(Convert.FromBase64String(_identifiersJSONStr)))
			{
				using (RSBinaryReader _binaryReader	= new RSBinaryReader(_memoryStream))
				{
					int 			_count			= _binaryReader.ReadInt32();
					List<string> 	_identifiers	= new List<string>(_count);
					
					for (int _iter = 0; _iter < _count; _iter++)
					{
						_identifiers.Add(_binaryReader.ReadString());
					}
					
					return _identifiers;
				}
			}
		}

		#endregion

		#region Save Methods

		/// <summary>
		/// Writes all serialization data to disk.
		/// By default <see cref="RSManager"/> writes serialization data to PlayerPrefs/File on Application Pause and on Application Quit.
		/// </summary>
		public static void Save ()
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] RSManager instance not found.");
				return;
			}
			
			Instance.Obj_Save();
		}

		private void Obj_Save ()
		{
			SaveDataRelatedToSaveTargetPrefs();

#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
			SaveDataRelatedToSaveTargetFile();
#endif

			// Save player prefs
			PlayerPrefs.Save();
		}

		private void SaveDataRelatedToSaveTargetPrefs ()
		{
			using (MemoryStream _memoryStream = new MemoryStream(256))
			{
				using (RSBinaryWriter _binaryWriter = new RSBinaryWriter(_memoryStream))
				{
					Dictionary<string, byte[]>.Enumerator	_dictEnumerator		= m_save2PrefsSerializationData.GetEnumerator();

					// Write count
					_binaryWriter.Write(m_save2PrefsSerializationData.Count);
					
					// Write identifier info and add serialization data to the player prefs
					while (_dictEnumerator.MoveNext())
					{
						KeyValuePair<string, byte[]> 		_keyValuePair		= _dictEnumerator.Current;
						string 								_identifier			= _keyValuePair.Key;
						byte[]								_serializationData	= _keyValuePair.Value;
						
						if (_serializationData == null)
							continue;

						// Save serialization data to the player prefs
						PlayerPrefs.SetString(_identifier, Convert.ToBase64String(_serializationData));
						
						// Write identifier
						_binaryWriter.Write(_identifier);
					}

					// Save identifier data to player prefs
					PlayerPrefs.SetString(kIdentifiersForPrefsKey, _binaryWriter.ToBase64String());
				}
			}
		}

#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
		private void SaveDataRelatedToSaveTargetFile ()
		{
			using (MemoryStream _memoryStream = new MemoryStream(256))
			{
				using (RSBinaryWriter _binaryWriter = new RSBinaryWriter(_memoryStream))
				{
					int 	_identifierCount		= m_save2FileSerializationIdentifiers.Count;

					// Write count
					_binaryWriter.Write(_identifierCount);
					
					// Write identifiers
					for (int _iter = 0; _iter < _identifierCount; _iter++)
						_binaryWriter.Write(m_save2FileSerializationIdentifiers[_iter]);
					
					// Save identifier data to player prefs
					PlayerPrefs.SetString(kIdentifiersForFilesKey, _binaryWriter.ToBase64String());
				}
			}
		}
#endif

		#endregion

		#region Remove Methods
		
		/// <summary>
		/// Clears existing serialization data associated with serialization identifier.
		/// </summary>
		/// <param name="_serializationID">Identifier associated with serialization data.</param>
		public static void Remove (string _serializationID)
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] RSManager instance not found.");
				return;
			}

			Instance.Obj_Remove(_serializationID);
		}

		private void Obj_Remove (string _serializationID)
		{
			// If identifier exists within serialized data saved to prefs then remove it
			if (m_save2PrefsSerializationData.Remove(_serializationID))
				return;

			// Remove identifier from serialized to files list
			if (m_save2FileSerializationIdentifiers.Remove(_serializationID))
				RemoveSerializationDataFile(_serializationID);
		}
		
		/// <summary>
		/// Clears all existing serialization data.
		/// </summary>
		public static void RemoveAll ()
		{
			if (Instance == null)
			{
				Debug.LogError("[RS] RSManager instance not found.");
				return;
			}

			Instance.Obj_RemoveAll();
		}

		private void Obj_RemoveAll ()
		{
			// Remove all serialization data which are saved to prefs
			m_save2PrefsSerializationData.Clear();

			// Remove all serialization data which were saved as files
			for (int _iter = 0; _iter < m_save2FileSerializationIdentifiers.Count; _iter++)
				RemoveSerializationDataFile(m_save2FileSerializationIdentifiers[_iter]);

			m_save2FileSerializationIdentifiers.Clear();
		}

		#endregion

		#region Purge Methods
	
		/// <summary>
		/// Clears all the cached information of specified object type.
		/// </summary>
		public static void Purge (Type _objectType)
		{
			SerializationTypeUtil.Purge(_objectType);
		}

		#endregion

		#region Misc. Methods

		/// <summary>
		/// Gets the serialization format version.
		/// </summary>
		/// <returns>The serialization format version.</returns>
		public int GetSerializationFormatVersion ()
		{
			return Constants.kSerializationFormatVersion;
		}
	
		private string GetFilePathForIdentifier (string _serializationID)
		{
			string	_fileName	= "RS" + _serializationID + ".txt";
			string 	_filePath	= Path.Combine(Application.persistentDataPath, _fileName);

			return _filePath;
		}

		private bool RemoveSerializationDataFile (string _serializationID)
		{
			string	_filePath	= GetFilePathForIdentifier(_serializationID);
			bool	_fileExists	= FileOperations.Exists(_filePath);

			if (_fileExists)
				FileOperations.Delete(_filePath);

			return _fileExists;
		}

		#endregion

		#region Editor Methods

#if UNITY_EDITOR
		private static void ApplicationUpdate ()
		{
			EditorApplication.update	-= ApplicationUpdate;

			// Assembly reload might have occured
			ApplicationStateChanged();
		}

		private static void ApplicationStateChanged ()
		{
			if (EditorApplicationIsPlaying())
				return;

			// Reset properties to ensure RSManager works fine in Edit mode as well
			ResetStaticProperties();
			
			RSManager _rsManager	= FindObjectOfType<RSManager>();
			
			if (_rsManager != null)
				Instance.Reset();
		}

		private static bool EditorApplicationIsPlaying ()
		{
			return (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused);
		}
#endif

		#endregion
	}
}