using NiceIO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GitLFSLocker
{
    class LocksTracker
    {
        public delegate void LocksUpdatedHandler(Dictionary<string, LockInfo> locks);
        public delegate void StartupCompleteHandler(bool success);
        private delegate void CommandCompleteHandler(string output);

        private NPath _repositoryPath;
        private CommandRunner _commandRunner;
        private IThreadMarshaller _threadMarshaller;
        private object _lock = new object();
        private Dictionary<NPath, LockInfo> _locks = new Dictionary<NPath, LockInfo>();

        public IEnumerable<KeyValuePair<NPath, LockInfo>> Locks
        {
            get
            {
                lock (_lock)
                {
                    foreach (var kvp in _locks)
                    {
                        yield return kvp;
                    }
                }
            }
        }

        public bool IsBusy => _commandRunner.IsRunning;
        public event LocksUpdatedHandler OnLocksUpdated;

        public LocksTracker(NPath repositoryPath, IThreadMarshaller threadMarshaller)
        {
            _repositoryPath = repositoryPath;
            _threadMarshaller = threadMarshaller;
            _commandRunner = new CommandRunner(_repositoryPath);
        }

        public void Start(StartupCompleteHandler callback)
        {
            _commandRunner.Run("lfs status", (code, output, error) => HandleStatusCommandComplete(code, output, error, callback));
        }

        private void HandleStatusCommandComplete(int exitCode, string output, string error, StartupCompleteHandler callback)
        {
            if (exitCode == 0)
            {
                callback(true);
            }
            else
            {
                Debug.LogError("lfs status failed: " + error);
                callback(false);
            }
        }

        public bool IsLocked(NPath absolutePath)
        {
            NPath relativePath = GetRepositoryRelativePath(absolutePath);

            lock (_lock)
            {
                return _locks.ContainsKey(relativePath);
            }
        }

        private NPath GetRepositoryRelativePath(NPath absolutePath)
        {
            if (!absolutePath.IsChildOf(_repositoryPath))
            {
                throw new Exception("Tried to get relative path of file outside repository " + absolutePath);
            }

            NPath relativePath = absolutePath.RelativeTo(_repositoryPath);
            return relativePath;
        }

        public void Update()
        {
            RunCommand("lfs locks", HandleLocksCommandComplete);
        }

        private void RunCommand(string command, CommandCompleteHandler continuation)
        {
            Debug.Log("Running git " + command);
            _commandRunner.Run(command, (code, o, e) => HandleCommandComplete(code, o, e, continuation));
        }

        private void HandleCommandComplete(int exitCode, string output, string error, CommandCompleteHandler continuation)
        {
            try
            {
                if (exitCode != 0)
                {
                    throw new Exception("LFS command failed: " + error);
                }

                continuation(output);
            }
            catch (Exception e)
            {
                // marshal exceptions back to main thread for Unity to handle them
                _threadMarshaller.Marshal(() => Throw(e));
            }
        }

        private void Throw(System.Exception e)
        {
            throw e;
        }

        private void HandleLocksCommandComplete(string output)
        {
            HandleLocksUpdated(output);
        }

        private void HandleLocksUpdated(string output)
        {
            var allLocks = LocksParser.Parse(output);
            var locks = allLocks.ToDictionary(x => x.path, x => x);
            lock (_lock)
            {
                _locks = locks;
            }
        }

        public void Unlock(NPath path)
        {
            lock (_locks)
            {
                if (!_locks.ContainsKey(path))
                {
                    throw new Exception("Tried to remove lock that didn't exist: " + path);
                }
            }

            RunCommand("lfs unlock " + path, o => HandleUnlocked(path));
        }

        private void HandleUnlocked(NPath path)
        {
            lock (_locks)
            {
                _locks.Remove(path);
            }
        }

        public void Lock(NPath path)
        {
            RunCommand("lfs lock " + GetRepositoryRelativePath(path), o => HandleLocked(path));
        }

        private void HandleLocked(NPath path)
        {
            Update();
        }
    }
}