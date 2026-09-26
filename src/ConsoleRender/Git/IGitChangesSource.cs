namespace ConsoleRender;

/// <summary>Reads the uncommitted changes of the repository containing a folder; replaceable in tests.</summary>
public interface IGitChangesSource
{
    GitChanges Read(string folder);
}
