﻿using System.Diagnostics;
using System.Threading;

namespace GitLFSLocker
{
	public class CommandRunner
	{
		public delegate void Callback(int exitCode, string output, string error);

		private string _repositoryPath;

		public bool IsRunning { get; private set; } = false;

		public CommandRunner(string repositoryPath)
		{
			_repositoryPath = repositoryPath;
		}

		public void Run(string command, Callback callback)
		{
			IsRunning = true;
			ThreadPool.QueueUserWorkItem(stateInfo => ThreadFunc(command, callback));
		}

		private void ThreadFunc(string command, Callback callback)
		{
			Process gitProcess = new Process();
			gitProcess.StartInfo.Arguments = command;
			gitProcess.StartInfo.CreateNoWindow = true;
			gitProcess.StartInfo.FileName = "git";
			gitProcess.StartInfo.RedirectStandardError = true;
			gitProcess.StartInfo.RedirectStandardOutput = true;
			gitProcess.StartInfo.UseShellExecute = false;
			gitProcess.StartInfo.WorkingDirectory = _repositoryPath;

			string output = "";
			string error = "";
			gitProcess.OutputDataReceived += (sender, e) => output += e.Data + '\n';
			gitProcess.ErrorDataReceived += (sender, e) => error += e.Data + '\n';

			gitProcess.Start();
			gitProcess.BeginOutputReadLine();
			gitProcess.BeginErrorReadLine();

			gitProcess.WaitForExit();

			IsRunning = false;

			callback(gitProcess.ExitCode, output, error);
		}
	}
}