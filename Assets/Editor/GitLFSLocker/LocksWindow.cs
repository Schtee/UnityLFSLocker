﻿using UnityEngine;
using UnityEditor;
using NiceIOEditor;
using System.Linq;

namespace GitLFSLocker
{
	public class LocksWindow : EditorWindow
	{
		private Vector2 _scrollPos;
		private bool _onlyShowOwnLocks = false;
		private System.Func<LockInfo, bool> _filter = null;

		[MenuItem("Git/Window")]
		private static void OpenWindow()
		{
			GetWindow<LocksWindow>().Show();
		}

		private void OnGUI()
		{
			string newUser = EditorGUILayout.TextField("User", Session.Instance.User);
			if (newUser != Session.Instance.User)
			{
				Session.Instance.User = newUser;
			}

			bool useForce = EditorGUILayout.Toggle("Use Force Unlock", Session.Instance.ForceUnlock);
			if(useForce != Session.Instance.ForceUnlock)
			{
				Session.Instance.ForceUnlock = useForce;
			}

			InitialiseFilter();

			GUILayout.BeginHorizontal();
			Session.Instance.RepositoryPath = EditorGUILayout.TextField("Repo path: ", Session.Instance.RepositoryPath);
			if (GUILayout.Button("Find", GUILayout.Width(50)))
			{
				NPath found = FindRepository();
				if (found == null)
				{
					Debug.LogWarning("Couldn't find repository path in parents of " + Application.dataPath);
				}
				else
				{
					Session.Instance.RepositoryPath = found;
				}
			}
			GUILayout.EndHorizontal();

			if (!Session.Instance.Ready && GUILayout.Button("Initialize"))
			{
				Session.Instance.Start();
				Session.Instance.LocksTracker.Update();
			}

			if (!Session.Instance.Ready)
			{
				GUILayout.Label("Session not ready");
				return;
			}

			if (Session.Instance.LocksTracker.IsBusy)
			{
				GUILayout.Label("Updating...");
				return;
			}

			DrawLocks();
		}

		private void InitialiseFilter()
		{
			bool newOnlyShowLocks = EditorGUILayout.Toggle("Only show own locks",_onlyShowOwnLocks);
			if (newOnlyShowLocks != _onlyShowOwnLocks)
			{
				if (newOnlyShowLocks)
				{
					_filter = IsOwnLock;
				}
				else
				{
					_filter = null;
				}

				_onlyShowOwnLocks = newOnlyShowLocks;
			}
		}

		private void DrawLocks()
		{
			_scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(EditorGUIUtility.currentViewWidth));

			GUIStyle box = "box";
			GUIStyle button = "button";

			foreach (var l in Session.Instance.LocksTracker.Locks)
			{
				if (_filter != null && !_filter(l))
				{
					continue;
				}

				GUILayout.BeginHorizontal(box, GUILayout.ExpandWidth(true), GUILayout.Width(EditorGUIUtility.currentViewWidth));
				float viewWidth = EditorGUIUtility.currentViewWidth - box.margin.left - box.margin.right - 10;

				const float desiredButtonWidth = 24.0f;
				float buttonWidth = desiredButtonWidth + button.border.left + button.border.right;
				float infoWidth = viewWidth - buttonWidth * 2;
				if (GUILayout.Button(EditorGUIUtility.IconContent("d_Project"), GUILayout.Width(desiredButtonWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
				{
					string assetPath = Session.Instance.LocksTracker.RepositoryPathToProjectPath(l.path);
					Object o = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
					if (o == null)
					{
						Debug.LogWarning("No asset found at " + assetPath);
					}
					else
					{
						EditorGUIUtility.PingObject(o);
					}
				}
				GUILayout.Label(new GUIContent(l.path, l.path.FileName), GUILayout.Width(infoWidth / 2.0f));
				GUILayout.Label(l.owner.name, GUILayout.Width(infoWidth / 6.0f));
				GUILayout.Label(l.locked_at.ToString(), GUILayout.Width(infoWidth * 2.0f / 6.0f));
				if (GUILayout.Button(ProjectWindow.LockIconTexture, GUILayout.Width(desiredButtonWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
				{
					Session.Instance.LocksTracker.Unlock(l.path, Session.Instance.ForceUnlock);
				}

				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
		}

		private NPath FindRepository()
		{
			NPath currentPath = Application.dataPath.ToNPath();
			if (IsGitFolder(currentPath))
			{
				return currentPath;
			}
			foreach (var parent in currentPath.RecursiveParents)
			{
				if (IsGitFolder(parent))
				{
					return parent;
				}
			}

			return null;
		}

		private static bool IsGitFolder(NPath path)
		{
			return path.Directories(".git").Any();
		}

		private static bool IsOwnLock(LockInfo l)
		{
			return l.owner.name == Session.Instance.User;
		}
	}
}
