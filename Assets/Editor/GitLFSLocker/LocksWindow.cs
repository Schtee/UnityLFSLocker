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
            string newUser = EditorGUILayout.TextField("User", Session.Instance.User);
            if (newUser != Session.Instance.User)
            {
                Session.Instance.User = newUser;
            }
            Session.Instance.RepositoryPath = EditorGUILayout.TextField("Repo path: ", Session.Instance.RepositoryPath);

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

            GUIStyle box = "box";
            GUIStyle button = "button";
            foreach (var kvp in Session.Instance.LocksTracker.Locks)
            {
                float viewWidth = EditorGUIUtility.currentViewWidth - box.margin.left - box.margin.right;
                GUILayout.BeginHorizontal(box, GUILayout.ExpandWidth(true));

                const float desiredButtonWidth = 24.0f;
                float buttonWidth = desiredButtonWidth + button.border.left + button.border.right;
                float infoWidth = viewWidth - buttonWidth * 2;
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Project"), GUILayout.Width(desiredButtonWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    string assetPath = Session.Instance.LocksTracker.RepositoryPathToProjectPath(kvp.Value.path);
                    Object o = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    if (o == null)
                    {
                        Debug.LogWarning("No asset found at " + assetPath);
                    }
                    else
                    {
                        Selection.activeObject = o;
                    }
                }
                GUILayout.Label(kvp.Value.path, GUILayout.Width(infoWidth / 2.0f));
                GUILayout.Label(kvp.Value.user, GUILayout.Width(infoWidth / 4.0f));
                GUILayout.Label(kvp.Value.id, GUILayout.Width(infoWidth / 4.0f));
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUILayout.Button(EditorGUIUtility.IconContent("AssemblyLock"), GUILayout.Width(desiredButtonWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    Session.Instance.LocksTracker.Unlock(kvp.Value.path);
                }

                GUILayout.EndHorizontal();
            }
        }
    }
}
