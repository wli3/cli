namespace Microsoft.DotNet.ShellShim
{
    internal interface IFilePermissionSetter
    {
        void SetUserExecutionPermission(string path);
    }
}
