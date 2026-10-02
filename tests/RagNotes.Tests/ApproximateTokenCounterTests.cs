using RagNotes.Web.Services.Chunking;

namespace RagNotes.Tests;

public class ApproximateTokenCounterTests
{
    private readonly ApproximateTokenCounter _counter = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public void Whitespace_has_no_tokens(string text)
    {
        Assert.Empty(_counter.Tokenize(text));
    }

    [Fact]
    public void Word_and_punctuation_are_separate_tokens()
    {
        var tokens = _counter.Tokenize("Hello, world!");

        Assert.Equal(4, tokens.Count);
        Assert.Equal("Hello", Slice("Hello, world!", tokens[0]));
        Assert.Equal(",", Slice("Hello, world!", tokens[1]));
        Assert.Equal("world", Slice("Hello, world!", tokens[2]));
        Assert.Equal("!", Slice("Hello, world!", tokens[3]));
    }

    [Fact]
    public void Whitespace_between_tokens_is_not_counted()
    {
        var text = "hello   world";
        var tokens = _counter.Tokenize(text);

        Assert.Equal(2, tokens.Count);
        Assert.Equal("hello   world", text[tokens[0].Start..(tokens[1].Start + tokens[1].Length)]);
    }

    [Fact]
    public void Short_word_is_one_token_and_long_word_splits_every_four_characters()
    {
        var shortWord = new string('a', 16);
        var longWord = new string('b', 17);

        Assert.Single(_counter.Tokenize(shortWord));

        var pieces = _counter.Tokenize(longWord);
        Assert.Equal(5, pieces.Count);
        Assert.Equal(new[] { 4, 4, 4, 4, 1 }, pieces.Select(token => token.Length));
        Assert.Equal(longWord, string.Concat(pieces.Select(token => longWord.Substring(token.Start, token.Length))));
    }

    private static string Slice(string text, TextToken token) => text.Substring(token.Start, token.Length);
}
