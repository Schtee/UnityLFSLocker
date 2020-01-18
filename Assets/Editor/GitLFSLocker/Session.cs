using NiceIO;
using UnityEditor;
using UnityEngine;

namespace GitLFSLocker
{
    class Session
    {
        private const string _repositoryPathKey = "GitLFSLockerRepositoryPath";

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

        public Session()
        {
            RepositoryPath = EditorPrefs.GetString(_repositoryPathKey);
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
                LocksTracker.Update();
            }
        }
    }
}