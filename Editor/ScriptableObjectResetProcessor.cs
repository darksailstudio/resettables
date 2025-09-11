using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DarkSail.Resettables.Editor
{
	/// <summary>
	/// Utility for resetting instantiable <see cref="ScriptableObject"/> assets
	/// marked with <see cref="ResetOnExitPlayModeAttribute"/>
	/// after changes made in editor play mode.
	/// It preserves a snapshot of serializable state upon entering play mode
	/// and restores modified resettable assets on exit,
	/// deletes resettable assets created during play mode,
	/// and restores resettable assets deleted during play mode.
	/// <example>
	/// Example usage:
	/// <code>
	/// using DarkSail.Resettables;
	/// using UnityEngine;
	///
	/// [ResetOnExitPlayMode]
	/// [CreateAssetMenu(menuName = "My Project/My Resettable")]
	/// public class MyResettable : ScriptableObject
	/// {
	///     public int Value;
	/// }
	/// </code>
	/// </example>
	/// </summary>
	class ScriptableObjectResetProcessor : UnityEditor.AssetModificationProcessor
	{
		readonly struct Metadata
		{
			public readonly string GUID;
			public readonly string Path;
			public readonly Type Type;

			public Metadata(string guid, string path, Type type)
			{
				GUID = guid;
				Path = path;
				Type = type;
			}
		}

		static List<Metadata> restorables = new List<Metadata>();

		static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
		{
			if (EditorApplication.isPlaying)
			{
				var type = AssetDatabase.GetMainAssetTypeAtPath(path);
				if (IsValid(type))
				{
					var guid = AssetDatabase.AssetPathToGUID(path);
					restorables.Add(new Metadata(guid, path, type));
				}
			}
			return AssetDeleteResult.DidNotDelete;
		}

		static bool IsValid(Type type)
		{
			if (
				!typeof(ScriptableObject).IsAssignableFrom(type)
				|| type.IsAbstract
				|| type.IsGenericType
			)
				return false;

			var attributeType = typeof(ResetOnExitPlayModeAttribute);

			if (type.IsDefined(attributeType, inherit: false))
				return true;

			var attribute = (ResetOnExitPlayModeAttribute)
				Attribute.GetCustomAttribute(type, attributeType, inherit: true);

			return attribute != null && attribute.Inherited;
		}

		static IEnumerable<(ScriptableObject instance, Metadata metadata)> Resettables =>
			TypeCache
				.GetTypesDerivedFrom<ScriptableObject>()
				.Where(IsValid)
				.SelectMany(type => AssetDatabase.FindAssets($"t:{type.Name}"))
				.Distinct()
				.Select(guid => (guid, path: AssetDatabase.GUIDToAssetPath(guid)))
				.Where(asset => asset.path != string.Empty)
				.Select(asset =>
				{
					var type = AssetDatabase.GetMainAssetTypeAtPath(asset.path);
					return (
						instance: IsValid(type)
							? AssetDatabase.LoadAssetAtPath<ScriptableObject>(asset.path)
							: null,
						metadata: new Metadata(asset.guid, asset.path, type)
					);
				})
				.Where(asset => asset.instance != null)
				.Concat(
					restorables.Select(metadata => (instance: (ScriptableObject)null, metadata))
				);

		[InitializeOnLoadMethod]
		static void OnLoad()
		{
			EditorApplication.playModeStateChanged += mode =>
			{
				switch (mode)
				{
					case PlayModeStateChange.ExitingEditMode:
						OnExitingEditMode();
						break;

					case PlayModeStateChange.ExitingPlayMode:
						OnExitingPlayMode();
						break;
				}
			};
		}

		static void OnExitingEditMode()
		{
			foreach (var resettable in Resettables)
			{
				Save(resettable.instance, resettable.metadata.GUID);
			}
		}

		static void OnExitingPlayMode()
		{
			try
			{
				foreach (var resettable in Resettables)
				{
					Load(resettable.instance, resettable.metadata);
				}
			}
			finally
			{
				restorables.Clear();
			}
		}

		static void Save(ScriptableObject instance, string guid)
		{
			SessionState.SetString(guid, EditorJsonUtility.ToJson(instance));
		}

		static void Load(ScriptableObject instance, Metadata metadata)
		{
			var serializedInstance = SessionState.GetString(metadata.GUID, string.Empty);
			SessionState.EraseString(metadata.GUID);

			if (serializedInstance == string.Empty)
			{
				Discard(metadata.Path);
			}
			else if (instance == null)
			{
				Recreate(metadata.Path, metadata.Type, serializedInstance);
			}
			else
			{
				Reset(instance, serializedInstance);
			}
		}

		static void Discard(string path)
		{
			AssetDatabase.DeleteAsset(path);
		}

		static void Recreate(string path, Type type, string serializedInstance)
		{
			var instance = ScriptableObject.CreateInstance(type);
			Reset(instance, serializedInstance);
			AssetDatabase.CreateAsset(instance, path);
		}

		static void Reset(ScriptableObject instance, string serializedInstance)
		{
			EditorJsonUtility.FromJsonOverwrite(serializedInstance, instance);
		}
	}
}
