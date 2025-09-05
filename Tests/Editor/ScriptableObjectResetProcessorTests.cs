using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkSail.Resettables.Editor.Tests
{
	public class ScriptableObjectProcessorTests
	{
		static string SessionStateKey => typeof(ScriptableObjectProcessorTests).FullName;

		static void Setup()
		{
			var testDir = AssetDatabase.GenerateUniqueAssetPath("Assets/Temp");
			SessionState.SetString(SessionStateKey, testDir);
			AssetDatabase.CreateFolder("Assets", Path.GetFileName(testDir));
		}

		static string AssetPath
		{
			get
			{
				var testDir = SessionState.GetString(SessionStateKey, string.Empty);
				Assert.That(testDir, Is.Not.EqualTo(string.Empty));
				return Path.Combine(testDir, nameof(Mock) + ".asset");
			}
		}

		static void Teardown()
		{
			var testDir = SessionState.GetString(SessionStateKey, string.Empty);
			SessionState.EraseString(SessionStateKey);
			Assert.That(testDir, Is.Not.EqualTo(string.Empty));
			AssetDatabase.DeleteAsset(testDir);
		}

		static readonly (Type type, bool isResettable)[] MockMap =
		{
			(typeof(NonResettableMock), false),
			(typeof(InheritedResettableMock), true),
			(typeof(DerivedInheritedResettableMock), true),
			(typeof(NonInheritedResettableMock), true),
			(typeof(DerivedNonInheritedResettableMock), false),
		};

		[UnityTest]
		public IEnumerator ResetsModifiedAssets(
			[ValueSource(nameof(MockMap))] (Type type, bool isResettable) mockData
		)
		{
			Setup();

			var mock = (Mock)ScriptableObject.CreateInstance(mockData.type);
			mock.Value = 100;
			AssetDatabase.CreateAsset(mock, AssetPath);

			yield return new EnterPlayMode();

			mock = AssetDatabase.LoadAssetAtPath<Mock>(AssetPath);
			mock.Value = 200;
			EditorUtility.SetDirty(mock);

			yield return new ExitPlayMode();

			mock = AssetDatabase.LoadAssetAtPath<Mock>(AssetPath);

			try
			{
				Assert.That(mock.Value, Is.EqualTo(mockData.isResettable ? 100 : 200));
			}
			finally
			{
				Teardown();
			}
		}

		[UnityTest]
		public IEnumerator DeletesCreatedAssets(
			[ValueSource(nameof(MockMap))] (Type type, bool isResettable) mockData
		)
		{
			Setup();

			yield return new EnterPlayMode();

			var mock = (Mock)ScriptableObject.CreateInstance(mockData.type);
			AssetDatabase.CreateAsset(mock, AssetPath);

			yield return new ExitPlayMode();

			mock = AssetDatabase.LoadAssetAtPath<Mock>(AssetPath);

			try
			{
				Assert.That(mock, mockData.isResettable ? Is.Null : Is.Not.Null);
			}
			finally
			{
				Teardown();
			}
		}

		[UnityTest]
		public IEnumerator RestoresDeletedAssets(
			[ValueSource(nameof(MockMap))] (Type type, bool isResettable) mockData
		)
		{
			Setup();

			var mock = (Mock)ScriptableObject.CreateInstance(mockData.type);
			AssetDatabase.CreateAsset(mock, AssetPath);

			yield return new EnterPlayMode();

			AssetDatabase.DeleteAsset(AssetPath);

			yield return new ExitPlayMode();

			mock = AssetDatabase.LoadAssetAtPath<Mock>(AssetPath);

			try
			{
				Assert.That(mock, mockData.isResettable ? Is.Not.Null : Is.Null);
			}
			finally
			{
				Teardown();
			}
		}

		[UnityTest]
		public IEnumerator ResetsRestoredAssets(
			[Values(
				typeof(InheritedResettableMock),
				typeof(DerivedInheritedResettableMock),
				typeof(NonInheritedResettableMock)
			)]
				Type mockType
		)
		{
			Setup();

			var mock = (Mock)ScriptableObject.CreateInstance(mockType);
			mock.Value = 100;
			AssetDatabase.CreateAsset(mock, AssetPath);

			yield return new EnterPlayMode();

			mock = AssetDatabase.LoadAssetAtPath<Mock>(AssetPath);
			mock.Value = 200;
			EditorUtility.SetDirty(mock);
			AssetDatabase.DeleteAsset(AssetPath);

			yield return new ExitPlayMode();

			mock = AssetDatabase.LoadAssetAtPath<Mock>(AssetPath);

			try
			{
				Assert.That(mock.Value, Is.EqualTo(100));
			}
			finally
			{
				Teardown();
			}
		}
	}
}
