using NiceIO;
using UnityEditor;
using UnityEngine;

namespace GitLFSLocker
{
    class Session
    {
        private const string _repositoryPathKey = "GitLFSLockerRepositoryPath";

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
            Start();
        }

        private void Start()
        {
            string repositoryPath = EditorPrefs.GetString(_repositoryPathKey);
            Start(repositoryPath);
        }

        public void Start(string repositoryPath)
        {
            if (string.IsNullOrEmpty(repositoryPath))
            {
                Debug.LogWarning("Not starting LFS session: repository path not set");
            }
            else
            {
                LocksTracker = new LocksTracker(repositoryPath.ToNPath(), new UnityEditorThreadMarshaller());
                Ready = true;
            }
        }
    }
}