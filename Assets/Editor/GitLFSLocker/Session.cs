using NiceIOEditor;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace GitLFSLocker
{
	class Session
	{
		public LocksTracker LocksTracker { get; private set; }

		private LFSLockerConfig _config;
		private string _user;

		private static Session _instance;
		public static Session Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Session();
				}
				return _instance;
			}
		}

		public bool Ready { get; private set; }


		public string User
		{
			get => _user;
			set
			{
				_user = value;
				if (_config != null)
				{
					_config.User = value;
					EditorUtility.SetDirty(_config);
				}
			}
		}

		public string RepositoryPath;
		public bool ForceUnlock;

		public Session()
		{
			_config = FindConfig();
			ReadConfig();
			Start();
		}

		private LFSLockerConfig FindConfig()
		{
			var guids = AssetDatabase.FindAssets("t:" + typeof(LFSLockerConfig).Name);
			if (guids.Length == 0)
				return null;
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var config = AssetDatabase.LoadAssetAtPath<LFSLockerConfig>(path);
			return config;
		}

		private void ReadConfig()
		{
			if (_config == null)
				return;
			User = _config.User;
			RepositoryPath = _config.RepositoryPath;
		}

		public void Start()
		{
			if (string.IsNullOrEmpty(RepositoryPath))
			{
				Debug.LogWarning("Not starting LFS session: repository path not set");
			}
			else
			{
				if (_config == null)
				{
					_config = ScriptableObject.CreateInstance<LFSLockerConfig>();
					AssetDatabase.CreateAsset(_config, "Assets/Editor/LFSLockerConfig.asset");
				}
				_config.User = User;
				_config.RepositoryPath = RepositoryPath;
				EditorUtility.SetDirty(_config);

				LocksTracker = new LocksTracker(_config?.RepositoryPath.ToNPath(), new UnityEditorThreadMarshaller(), HandleLocksUpdated);
				LocksTracker.Start(HandleStartupComplete);
			}
		}

		private void HandleLocksUpdated(IEnumerable<LockInfo> locks)
		{
			EditorApplication.delayCall += () =>
			{
				UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
			};
		}

		private void HandleStartupComplete(bool success)
		{
			if (success)
			{
				Ready = true;
				EditorApplication.delayCall += () => AssetDatabase.SaveAssets();

				EditorApplication.update += Poll;
			}
		}

		private const float _updateFrequencyInSeconds = 30.0f;
		private double _nextUpdateTime = 0.0;
		private void Poll()
		{
			if (EditorApplication.timeSinceStartup > _nextUpdateTime)
			{
				EditorApplication.update -= Poll;
				LocksTracker.Update(HandleLocksUpdatedPoll);
			}
		}

		private void HandleLocksUpdatedPoll(bool locksUpdatesSuccess, string message)
		{
			if (!locksUpdatesSuccess)
			{
				return;
			}

			EditorApplication.delayCall += () =>
			{
				_nextUpdateTime = EditorApplication.timeSinceStartup + _updateFrequencyInSeconds;
				EditorApplication.delayCall += () => EditorApplication.update += Poll;
			};
		}

		public bool IsLockedBySomeoneElse(string assetPath, out string lockUser)
		{
			if (!Ready)
			{
				lockUser = null;
				return false;
			}

			LockInfo lockInfo;
			if (!Instance.LocksTracker.TryGetLockInfo(assetPath.ToNPath().MakeAbsolute(), out lockInfo))
			{
				lockUser = null;
				return false;
			}

			lockUser = lockInfo.owner.name;
			return lockUser != _config?.User;
		}

		[MenuItem("Git/Clear settings")]
		private static void ClearSettings()
		{
			if (Instance._config != null)
			{
				var path = AssetDatabase.GetAssetPath(Instance._config);
				Object.DestroyImmediate(Instance._config, true);
				AssetDatabase.DeleteAsset(path);
			}

			Instance._config = null;
			Instance.User = null;
			Instance.RepositoryPath = null;
			Instance.ForceUnlock = false;
			Instance.Ready = false;
		}
	}
}
