using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GitLFSLocker
{
    class LocksTracker
    {
        public delegate void LocksUpdatedHandler(Dictionary<string, LockInfo> locks);
        private delegate void CommandCompleteHandler(string output);

        private string _repositoryPath;
        private CommandRunner _commandRunner;
        private IThreadMarshaller _threadMarshaller;
        private object _lock = new object();
        private Dictionary<string, LockInfo> _locks = new Dictionary<string, LockInfo>();

        public IEnumerable<KeyValuePair<string, LockInfo>> Locks
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

        public LocksTracker(string repositoryPath, IThreadMarshaller threadMarshaller)
        {
            _repositoryPath = repositoryPath;
            _threadMarshaller = threadMarshaller;
            _commandRunner = new CommandRunner(_repositoryPath);
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
                    throw new Exception("LFS command failed: " + output);
                }

                continuation(output);
            }
            catch (Exception e)
            {
                // marshal exception back to main thread for Unity to handle them
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
            var locks = allLocks.ToDictionary(x => x.id, x => x);
            lock (_lock)
            {
                _locks = locks;
            }
        }

        public void Unlock(string id)
        {
            lock (_locks)
            {
                if (!_locks.ContainsKey(id))
                {
                    throw new Exception("Tried to remove lock that didn't exist: " + id);
                }
            }

            RunCommand("lfs unlock --id=" + id, o => HandleUnlocked(id));
        }

        private void HandleUnlocked(string id)
        {
            lock (_locks)
            {
                _locks.Remove(id);
            }
        }
    }
}