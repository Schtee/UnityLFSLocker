using UnityEngine;
using UnityEditor;
using NiceIO;

namespace GitLFSLocker
{
    public class LocksWindow : EditorWindow
    {
        [MenuItem("Git/Window")]
        private static void OpenWindow()
        {
            GetWindow<LocksWindow>().Show();
        }

        private void OnGUI()
        {
            Session.Instance.RepositoryPath = GUILayout.TextField(Session.Instance.RepositoryPath);

            if (GUILayout.Button("Test"))
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

            foreach (var kvp in Session.Instance.LocksTracker.Locks)
            {
                GUILayout.BeginHorizontal("box");
                float viewWidth = EditorGUIUtility.currentViewWidth;

                GUILayout.Label(kvp.Value.path, GUILayout.Width(viewWidth / 4.0f));
                GUILayout.Label(kvp.Value.user, GUILayout.Width(viewWidth / 4.0f));
                GUILayout.Label(kvp.Value.id, GUILayout.Width(viewWidth / 4.0f));
                if (GUILayout.Button(EditorGUIUtility.IconContent("LockIcon"), GUILayout.Width(viewWidth / 4.0f)))
                {
                    Session.Instance.LocksTracker.Unlock(kvp.Value.path);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
