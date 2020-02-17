using NiceIO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GitLFSLocker
{
    class LocksTracker
    {
        public delegate void LocksUpdatedHandler(IEnumerable<LockInfo> locks);
        public delegate void StartupCompleteHandler(bool success);
        public delegate void CommandCompleteHandler(bool success, string message);

        // Always called when locks need to be refreshed
        public event LocksUpdatedHandler OnLocksUpdated;

        private NPath _repositoryPath;
        private CommandRunner _commandRunner;
        private IThreadMarshaller _threadMarshaller;
        private object _lock = new object();
        private Dictionary<NPath, LockInfo> _locks = new Dictionary<NPath, LockInfo>();

        public IEnumerable<LockInfo> Locks
        {
            get
            {
                lock (_lock)
                {
                    return LocksWithoutLock;
                }
            }
        }

        // Can be used in contexts where we already have a lock, e.g. when locks are updated
        private IEnumerable<LockInfo> LocksWithoutLock
        {
            get
            {
                foreach (var kvp in _locks)
                {
                    yield return kvp.Value;
                }
            }
        }

        public bool IsBusy => _commandRunner.IsRunning;

        public LocksTracker(NPath repositoryPath, IThreadMarshaller threadMarshaller, LocksUpdatedHandler onLocksUpdated = null)
        {
            _repositoryPath = repositoryPath;
            _threadMarshaller = threadMarshaller;
            _commandRunner = new CommandRunner(_repositoryPath);

            if (onLocksUpdated != null)
            {
                OnLocksUpdated += onLocksUpdated;
            }
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

        public bool TryGetLockInfo(NPath absolutePath, out LockInfo lockInfo)
        {
            NPath relativePath = GetRepositoryRelativePath(absolutePath);

            lock (_lock)
            {
                return _locks.TryGetValue(relativePath, out lockInfo);
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

        public NPath RepositoryPathToProjectPath(NPath path)
        {
            NPath fullPath = _repositoryPath.Combine(path);
            return fullPath.RelativeTo(Application.dataPath.ToNPath().Parent);
        }

        public NPath GetRepositoryRelativePath(NPath absolutePath)
        {
            if (!IsPathInsideRepository(absolutePath))
            {
                throw new Exception("Tried to get relative path of file outside repository " + absolutePath);
            }

            NPath relativePath = absolutePath.RelativeTo(_repositoryPath);
            return relativePath;
        }

        public bool IsPathInsideRepository(NPath absolutePath)
        {
            return absolutePath.IsChildOf(_repositoryPath);
        }

        public void Update(CommandCompleteHandler handler = null)
        {
			RunCommand("lfs locks --json", HandleLocksCommandComplete, handler);
        }

        private void RunCommand(string command, CommandCompleteHandler continuation, CommandCompleteHandler externalCallback = null)
        {
            Debug.Log("Running git " + command);
            _commandRunner.Run(command, (code, o, e) => HandleCommandComplete(code, o, e, continuation, externalCallback));
        }

        private void HandleCommandComplete(int exitCode, string output, string error,
			CommandCompleteHandler continuation, CommandCompleteHandler externalCallback)
        {
            try
            {
				bool success = exitCode == 0;
				string message = success ? output : error;
                continuation(success, message);
				externalCallback?.Invoke(success, message);
            }
            catch (Exception e)
            {
                // marshal exceptions back to main thread for Unity to handle them
                _threadMarshaller.Marshal(() => Throw(e));
            }
        }

        private void Throw(Exception e)
        {
			Debug.LogException(e);
        }

        private void HandleLocksCommandComplete(bool success, string output)
        {
			if (!success)
			{
				Debug.LogWarning("Failed to get locks: " + output);
				return;
			}

            HandleLocksUpdated(output);
        }

        private void HandleLocksUpdated(string output)
        {
            var allLocks = LocksParser.Parse(output);
            var locks = allLocks.ToDictionary(x => x.path, x => x);
            lock (_lock)
            {
                _locks = locks;
                OnLocksUpdated(LocksWithoutLock);
            }
        }

        public void UnlockAbsolutePath(NPath path)
        {
            Unlock(GetRepositoryRelativePath(path));
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

			RunCommand("lfs unlock " + path, (success, message) => HandleUnlocked(success, message, path));
        }

        private void HandleUnlocked(bool success, string message, NPath path)
        {
			if (!success)
			{
				Debug.LogError("Failed to unlock " + path + ": " + message);
				return;
			}

            lock (_locks)
            {
                _locks.Remove(path);
                OnLocksUpdated(LocksWithoutLock);
            }
        }

        public void Lock(NPath path)
        {
            RunCommand("lfs lock " + GetRepositoryRelativePath(path), (success, message) => HandleLocked(success, message, path));
        }

        private void HandleLocked(bool success, string message, NPath path)
        {
			if (!success)
			{
				Debug.LogError("Failed to unlock " + path + ": " + message);
				return;
			}

            Update();
        }
    }
}
