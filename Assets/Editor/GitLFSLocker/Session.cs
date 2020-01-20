﻿using NiceIO;
using UnityEditor;
using UnityEngine;

namespace GitLFSLocker
{
    class Session
    {
        private const string _repositoryPathKey = "GitLFSLockerRepositoryPath";
        private const string _userKey = "GitLFSLockersUser";

        private string _user;
        public string RepositoryPath;

        public LocksTracker LocksTracker { get; private set; }

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
            get { return _user; }
            set
            {
                _user = value;
                EditorPrefs.SetString(_userKey, _user);
            }
        }

        public Session()
        {
            RepositoryPath = EditorPrefs.GetString(_repositoryPathKey);
            _user = EditorPrefs.GetString(_userKey);
            Start();
        }

        public void Start()
        {
            if (string.IsNullOrEmpty(RepositoryPath))
            {
                Debug.LogWarning("Not starting LFS session: repository path not set");
            }
            else
            {
                LocksTracker = new LocksTracker(RepositoryPath.ToNPath(), new UnityEditorThreadMarshaller());
                LocksTracker.Start(HandleStartupComplete);
            }
        }

        private void HandleStartupComplete(bool success)
        {

            if (success)
            {
                Ready = true;
                EditorApplication.delayCall += () => EditorPrefs.SetString(_repositoryPathKey, RepositoryPath);

				EditorApplication.update += Poll;
            }
        }

		private const float _updateFrequencyInSeconds = 30.0f;
		private double _nextUpdateTime = 0.0;
		private void Poll()
		{
			void HandleLocksUpdated(bool locksUpdatesSuccess, string message)
			{
				EditorApplication.delayCall += () =>
				{
					_nextUpdateTime = EditorApplication.timeSinceStartup + _updateFrequencyInSeconds;
					EditorApplication.delayCall += () => EditorApplication.update += Poll;
				};
			}

			if (EditorApplication.timeSinceStartup > _nextUpdateTime)
			{
				EditorApplication.update -= Poll;
				LocksTracker.Update(HandleLocksUpdated);
			}
		}
    }
}