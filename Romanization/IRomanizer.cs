namespace Subtitlr.Romanization;

/// <summary>
/// Converts native-script text for one language into Latin-script text.
/// Implement this for additional languages (Japanese, Chinese, ...) and
/// register them in RomanizerRegistry.
/// </summary>
public interface IRomanizer
{
    string Romanize(string text);
}
