namespace BashGPT.Tools.Fetch;

public interface IFetchPolicy
{
    bool Allow(FetchInput input);
}
