namespace GitLFSLocker
{
	interface IThreadMarshaller
	{
		void Marshal(System.Action action);
	}
}