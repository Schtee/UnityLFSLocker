using UnityEditor;
using NiceIOEditor;
using UnityEngine;

namespace GitLFSLocker
{
	[InitializeOnLoad]
	public class ProjectWindow
	{
		private const string _lockMenuItem = "Assets/LFS Lock";
		private const string _unlockMenuItem = "Assets/LFS Unlock";
		private static Texture _lockIconTexture;

		public static Texture LockIconTexture
		{
			get
			{
				if (_lockIconTexture == null)
				{
					_lockIconTexture = Resources.Load<Texture>("lockicon");
				}
				return _lockIconTexture;
			}
		}

		static ProjectWindow()
		{
			EditorApplication.projectWindowItemOnGUI += ProjectWindowOnGUI;
		}

		private static void ProjectWindowOnGUI(string guid, Rect selectionRect)
		{
			if (!Session.Instance.Ready || string.IsNullOrEmpty(guid))
			{
				return;
			}

			NPath fullPath = GetFullAssetPath(AssetDatabase.GUIDToAssetPath(guid));
			if (!Session.Instance.LocksTracker.IsPathInsideRepository(fullPath))
			{
				return;
			}

			LockInfo lockInfo;
			if (Session.Instance.LocksTracker.TryGetLockInfo(fullPath, out lockInfo))
			{
				Rect pos = selectionRect;
				const float iconWidth = 16.0f;
				pos.x = pos.width - iconWidth;
				pos.width = iconWidth;
				Color oldColor = GUI.color;
				GUI.color = lockInfo.owner.name == Session.Instance.User ? Color.green : Color.red;
				var content = new GUIContent(LockIconTexture);
				content.tooltip = "Locked by " + lockInfo.owner.name;
				GUI.Label(pos, content);
				GUI.color = oldColor;
			}
		}

		private static NPath GetFullAssetPath(Object asset)
		{
			string assetPath = AssetDatabase.GetAssetPath(asset);
			return GetFullAssetPath(assetPath);
		}

		private static NPath GetFullAssetPath(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return null;
			}

			string fullPath = System.IO.Path.GetFullPath(assetPath);
			return fullPath.ToNPath();
		}

		[MenuItem(_lockMenuItem, false, 999)]
		private static void Lock()
		{
			var selected = Selection.objects;
			foreach (var obj in selected)
			{
				Session.Instance.LocksTracker.Lock(GetFullAssetPath(obj));
			}
		}

		[MenuItem(_lockMenuItem, true)]
		private static bool LockValidate()
		{
			if (!Session.Instance.Ready)
			{
				return false;
			}

			var selected = Selection.objects;
			foreach (var obj in selected)
			{
				NPath fullPath = GetFullAssetPath(obj);
				if (string.IsNullOrEmpty(fullPath))
				{
					return false;
				}

				if (!Session.Instance.LocksTracker.IsPathInsideRepository(fullPath))
				{
					return false;
				}

				if (Session.Instance.LocksTracker.IsLocked(fullPath))
				{
					return false;
				}
			}

			return true;
		}

		[MenuItem(_unlockMenuItem, false, 999)]
		private static void Unlock()
		{
			var selected = Selection.objects;
			foreach (var obj in selected)
			{
				Session.Instance.LocksTracker.UnlockAbsolutePath(GetFullAssetPath(obj), Session.Instance.ForceUnlock);
			}
		}

		[MenuItem(_unlockMenuItem, true)]
		private static bool UnlockValidate()
		{
			if (!Session.Instance.Ready)
			{
				return false;
			}

			var selected = Selection.objects;
			foreach (var obj in selected)
			{
				NPath fullPath = GetFullAssetPath(obj);
				if (string.IsNullOrEmpty(fullPath))
				{
					return false;
				}

				if (!Session.Instance.LocksTracker.IsPathInsideRepository(fullPath))
				{
					return false;
				}

				if (!Session.Instance.LocksTracker.IsLocked(fullPath))
				{
					return false;
				}
			}

			return true;
		}
	}
}